namespace Elsasa.Sis.Students.Features.Admission.RegisterApplication;

using System.Globalization;
using System.Text.RegularExpressions;
using Dapper;
using Elsasa.Sis.Contracts.Operations;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Elsasa.Sis.Students.Features.Admission.Common;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler executing prospective student application registration, deduplication matching,
/// sequential application numbering, and atomic multi-table persistence (ADR-004, ADR-005).
/// </summary>
public sealed class RegisterApplicationHandler(
    ISqlConnectionFactory connectionFactory,
    IOperationsCalendarQueryService calendarService,
    ICurrentUser currentUser)
{
    private const string _findApplicantByIdSql = """
        SELECT id
        FROM students.applicants
        WHERE id = @ApplicantId;
        """;

    private const string _findApplicantByGovIdSql = """
        SELECT TOP 1 id
        FROM students.applicants
        WHERE government_id_number = @GovernmentIdNumber;
        """;

    private const string _findApplicantByNameAndDobSql = """
        SELECT TOP 1 id
        FROM students.applicants
        WHERE UPPER(LTRIM(RTRIM(full_name))) = UPPER(LTRIM(RTRIM(@FullName)))
          AND date_of_birth = @DateOfBirth;
        """;

    private const string _updateApplicantContactSql = """
        UPDATE students.applicants
        SET email = COALESCE(@Email, email),
            phone = COALESCE(@Phone, phone),
            address = COALESCE(@Address, address),
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @ApplicantId;
        """;

    private const string _insertApplicantSql = """
        INSERT INTO students.applicants (
            id,
            first_name,
            last_name,
            full_name,
            date_of_birth,
            gender,
            government_id_type,
            government_id_number,
            email,
            phone,
            address,
            previous_school_name,
            previous_school_address,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @FirstName,
            @LastName,
            @FullName,
            @DateOfBirth,
            @Gender,
            @GovernmentIdType,
            @GovernmentIdNumber,
            @Email,
            @Phone,
            @Address,
            @PreviousSchoolName,
            @PreviousSchoolAddress,
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private const string _checkActiveApplicationSql = """
        SELECT COUNT(1)
        FROM students.admission_applications
        WHERE applicant_id = @ApplicantId
          AND admission_period_id = @AdmissionPeriodId
          AND status NOT IN ('withdrawn', 'decided');
        """;

    private const string _nextSequenceSql = """
        SELECT NEXT VALUE FOR students.admission_application_number_seq;
        """;

    private const string _insertApplicationSql = """
        INSERT INTO students.admission_applications (
            id,
            applicant_id,
            academic_year_id,
            admission_period_id,
            application_number,
            application_type,
            requested_entry_level,
            requested_campus,
            requested_program,
            registration_date,
            status,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @ApplicantId,
            @AcademicYearId,
            @AdmissionPeriodId,
            @ApplicationNumber,
            @ApplicationType,
            @RequestedEntryLevel,
            @RequestedCampus,
            @RequestedProgram,
            @RegistrationDate,
            @Status,
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private const string _insertContactSql = """
        INSERT INTO students.admission_application_contacts (
            id,
            application_id,
            full_name,
            relationship,
            phone,
            email,
            address,
            is_primary,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @ApplicationId,
            @FullName,
            @Relationship,
            @Phone,
            @Email,
            @Address,
            @IsPrimary,
            @CreatedAt,
            @CreatedBy
        );
        """;

    /// <summary>
    /// Processes application registration with cross-domain operational window verification,
    /// applicant identity deduplication, sequence numbering, and atomic multi-table insertion.
    /// </summary>
    public async Task<Result<RegisterApplicationResponse>> HandleAsync(
        RegisterApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Cross-domain operational period validation via IOperationsCalendarQueryService (ADR-001, ADR-003)
        var periodResult = await calendarService.GetOperationalPeriodByIdAsync(request.AdmissionPeriodId, cancellationToken);
        if (!periodResult.IsSuccess)
        {
            return periodResult.Error;
        }

        var period = periodResult.Value;
        if (period is null)
        {
            return DomainError.NotFound(
                "Admission.PeriodNotFound",
                $"Operational period '{request.AdmissionPeriodId}' was not found.");
        }

        var isPeriodUsable = string.Equals(period.Status, "active", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(period.Status, "approved", StringComparison.OrdinalIgnoreCase);

        if (!isPeriodUsable)
        {
            return DomainError.Conflict(
                "Admission.PeriodNotActive",
                $"Admission period '{period.Name}' is not active or approved (current status: '{period.Status}').");
        }

        var registrationDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var isBypassRequested = request.BypassWindowCheck == true;

        if (isBypassRequested)
        {
            var hasBypassPermission = currentUser.IsAuthenticated && (
                currentUser.Permissions.Contains(Permissions.Students.Admission.Register)
                || currentUser.Permissions.Contains("*")
                || currentUser.Roles.Contains("Admin")
                || currentUser.Roles.Contains("AdmissionOfficer")
                || currentUser.Roles.Contains("Admission Officer")
                || currentUser.Roles.Contains("StudentAdminOfficer")
                || currentUser.Roles.Contains("Student Administration Officer"));

            if (!hasBypassPermission)
            {
                return DomainError.Forbidden(
                    "Admission.BypassForbidden",
                    "Only authorized staff may bypass admission window checks.");
            }
        }
        else
        {
            if (registrationDate < period.StartDate || registrationDate > period.EndDate)
            {
                return DomainError.Conflict(
                    "Admission.WindowClosed",
                    $"Admission period '{period.Name}' is closed for registration (window: {period.StartDate:yyyy-MM-dd} to {period.EndDate:yyyy-MM-dd}).");
            }
        }

        // 2. Open connection and begin atomic multi-table transaction
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var auditUser = currentUser.Username ?? (currentUser.IsAuthenticated ? "AuthenticatedStaff" : "PortalUser");
            var now = DateTimeOffset.UtcNow;

            // 3. Applicant identity deduplication matching (TD-03)
            Guid? matchedApplicantId = null;

            if (request.ApplicantId.HasValue)
            {
                var existingId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                    _findApplicantByIdSql,
                    new { ApplicantId = request.ApplicantId.Value },
                    transaction: tx);

                if (!existingId.HasValue)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return DomainError.NotFound(
                        "Admission.ApplicantNotFound",
                        $"Applicant with ID '{request.ApplicantId.Value}' was not found.");
                }

                matchedApplicantId = existingId.Value;
            }

            if (!matchedApplicantId.HasValue && !string.IsNullOrWhiteSpace(request.GovernmentIdNumber))
            {
                var existingIdByGovId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                    _findApplicantByGovIdSql,
                    new { GovernmentIdNumber = request.GovernmentIdNumber.Trim() },
                    transaction: tx);

                if (existingIdByGovId.HasValue)
                {
                    matchedApplicantId = existingIdByGovId.Value;
                }
            }

            if (!matchedApplicantId.HasValue && request.DateOfBirth.HasValue)
            {
                var existingIdByNameDob = await connection.QuerySingleOrDefaultAsync<Guid?>(
                    _findApplicantByNameAndDobSql,
                    new
                    {
                        FullName = request.FullName.Trim(),
                        DateOfBirth = request.DateOfBirth.Value
                    },
                    transaction: tx);

                if (existingIdByNameDob.HasValue)
                {
                    matchedApplicantId = existingIdByNameDob.Value;
                }
            }

            Guid applicantId;
            if (matchedApplicantId.HasValue)
            {
                applicantId = matchedApplicantId.Value;
                await connection.ExecuteAsync(
                    _updateApplicantContactSql,
                    new
                    {
                        ApplicantId = applicantId,
                        Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                        Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                        Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
                        UpdatedAt = now,
                        UpdatedBy = auditUser
                    },
                    transaction: tx);
            }
            else
            {
                applicantId = Guid.NewGuid();
                var trimmedFullName = request.FullName.Trim();
                var nameParts = trimmedFullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var firstName = nameParts.Length > 0 ? nameParts[0] : trimmedFullName;
                var lastName = nameParts.Length > 1 ? nameParts[1] : firstName;

                if (firstName.Length > 100) firstName = firstName[..100];
                if (lastName.Length > 100) lastName = lastName[..100];

                await connection.ExecuteAsync(
                    _insertApplicantSql,
                    new
                    {
                        Id = applicantId,
                        FirstName = firstName,
                        LastName = lastName,
                        FullName = trimmedFullName.Length > 200 ? trimmedFullName[..200] : trimmedFullName,
                        DateOfBirth = request.DateOfBirth,
                        Gender = request.Gender,
                        GovernmentIdType = request.GovernmentIdType,
                        GovernmentIdNumber = string.IsNullOrWhiteSpace(request.GovernmentIdNumber) ? null : request.GovernmentIdNumber.Trim(),
                        Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                        Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                        Address = request.Address,
                        PreviousSchoolName = request.PreviousSchoolName,
                        PreviousSchoolAddress = request.PreviousSchoolAddress,
                        Notes = request.Notes,
                        CreatedAt = now,
                        CreatedBy = auditUser
                    },
                    transaction: tx);
            }

            // 4. Enforce single active application uniqueness per period (TD-03)
            var activeAppCount = await connection.ExecuteScalarAsync<int>(
                _checkActiveApplicationSql,
                new
                {
                    ApplicantId = applicantId,
                    AdmissionPeriodId = period.Id
                },
                transaction: tx);

            if (activeAppCount > 0)
            {
                await tx.RollbackAsync(cancellationToken);
                return DomainError.Conflict(
                    "Admission.DuplicateActiveApplication",
                    "An active application already exists for this applicant in the specified admission period.");
            }

            // 5. Sequence generation and application number formatting (TD-02)
            var seq = await connection.ExecuteScalarAsync<int>(_nextSequenceSql, transaction: tx);
            var yearMatch = Regex.Match(period.PeriodCode ?? string.Empty, @"\b(20\d\d)\b");
            var yearCode = yearMatch.Success ? yearMatch.Value : period.StartDate.Year.ToString("D4", CultureInfo.InvariantCulture);
            var applicationNumber = ApplicationNumberFormatter.Format(yearCode, seq);

            // 6. Insert admission application record
            var applicationId = Guid.NewGuid();
            var applicationType = string.IsNullOrWhiteSpace(request.ApplicationType) ? "new" : request.ApplicationType.Trim();

            await connection.ExecuteAsync(
                _insertApplicationSql,
                new
                {
                    Id = applicationId,
                    ApplicantId = applicantId,
                    AcademicYearId = period.AcademicYearId,
                    AdmissionPeriodId = period.Id,
                    ApplicationNumber = applicationNumber,
                    ApplicationType = applicationType,
                    RequestedEntryLevel = request.RequestedEntryLevel.Trim(),
                    RequestedCampus = request.RequestedCampus,
                    RequestedProgram = request.RequestedProgram,
                    RegistrationDate = registrationDate,
                    Status = "registered",
                    Notes = request.Notes,
                    CreatedAt = now,
                    CreatedBy = auditUser
                },
                transaction: tx);

            // 7. Insert guardian contacts atomically
            if (request.Contacts is { Count: > 0 })
            {
                foreach (var contact in request.Contacts)
                {
                    await connection.ExecuteAsync(
                        _insertContactSql,
                        new
                        {
                            Id = Guid.NewGuid(),
                            ApplicationId = applicationId,
                            FullName = contact.FullName.Trim(),
                            Relationship = contact.Relationship.Trim(),
                            Phone = string.IsNullOrWhiteSpace(contact.Phone) ? null : contact.Phone.Trim(),
                            Email = string.IsNullOrWhiteSpace(contact.Email) ? null : contact.Email.Trim(),
                            Address = contact.Address,
                            IsPrimary = contact.IsPrimary,
                            CreatedAt = now,
                            CreatedBy = auditUser
                        },
                        transaction: tx);
                }
            }

            await tx.CommitAsync(cancellationToken);

            return Result.Success(new RegisterApplicationResponse(
                applicationId,
                applicationNumber,
                applicantId,
                "registered",
                registrationDate));
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            await tx.RollbackAsync(cancellationToken);

            if (ex.Message.Contains("ux_students_admission_applications_active", StringComparison.OrdinalIgnoreCase))
            {
                return DomainError.Conflict(
                    "Admission.DuplicateActiveApplication",
                    "An active application already exists for this applicant in the specified admission period.");
            }

            if (ex.Message.Contains("ux_students_admission_applications_application_number", StringComparison.OrdinalIgnoreCase))
            {
                return DomainError.Conflict(
                    "Admission.DuplicateApplicationNumber",
                    "The generated application number conflicts with an existing application.");
            }

            return DomainError.Conflict(
                "Admission.PersistenceConflict",
                "A concurrency or uniqueness conflict occurred during registration.");
        }
        catch (Exception)
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
