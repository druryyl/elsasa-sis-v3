namespace Elsasa.Sis.Students.Features.Enrollment.EnrollAcceptedApplicant;

using Dapper;
using Elsasa.Sis.Contracts.Finance;
using Elsasa.Sis.Contracts.Students.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Elsasa.Sis.Students.Features.Enrollment.Services;
using Elsasa.Sis.Students.Features.Records.Guardians;
using MediatR;

/// <summary>
/// Feature handler executing the official enrollment of an accepted applicant (ADR-003, ADR-004, ADR-005, UC-STD-04).
/// Enforces admission prerequisite checks, non-enrollment verification, payment/waiver gates,
/// atomic multi-table insertion (student identity, administrative placement, credential document promotion, guardian matrix),
/// and strictly post-commit domain event notification.
/// </summary>
public sealed class EnrollAcceptedApplicantHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IStudentNumberGenerator studentNumberGenerator,
    IFinanceModule financeModule,
    IPublisher publisher)
{
    private const string _selectApplicationPrerequisiteSql = """
        SELECT TOP 1
            a.id AS ApplicationId,
            a.applicant_id AS ApplicantId,
            a.academic_year_id AS AcademicYearId,
            a.status AS ApplicationStatus,
            d.decision_type AS DecisionType
        FROM students.admission_applications a
        LEFT JOIN students.admission_decisions d ON d.application_id = a.id
        WHERE (
            (@ApplicationId IS NOT NULL AND a.id = @ApplicationId)
            OR (@ApplicationId IS NULL AND a.applicant_id = @ApplicantId AND (a.academic_year_id = @AcademicYearId OR @AcademicYearId = '00000000-0000-0000-0000-000000000000'))
        )
        ORDER BY a.created_at DESC;
        """;

    private const string _fallbackSelectApplicationSql = """
        SELECT TOP 1
            a.id AS ApplicationId,
            a.applicant_id AS ApplicantId,
            a.academic_year_id AS AcademicYearId,
            a.status AS ApplicationStatus,
            d.decision_type AS DecisionType
        FROM students.admission_applications a
        LEFT JOIN students.admission_decisions d ON d.application_id = a.id
        WHERE a.applicant_id = @ApplicantId
        ORDER BY a.created_at DESC;
        """;

    private const string _checkStudentExistsSql = """
        SELECT id
        FROM students.students
        WHERE applicant_id = @ApplicantId;
        """;

    private const string _selectApplicantSql = """
        SELECT
            id AS Id,
            full_name AS FullName,
            preferred_name AS PreferredName,
            date_of_birth AS DateOfBirth,
            place_of_birth AS PlaceOfBirth,
            gender AS Gender,
            nationality AS Nationality,
            government_id_type AS GovernmentIdType,
            government_id_number AS GovernmentIdNumber,
            address AS Address,
            phone AS Phone,
            email AS Email
        FROM students.applicants
        WHERE id = @ApplicantId;
        """;

    private const string _insertStudentSql = """
        INSERT INTO students.students
        (
            id,
            student_number,
            applicant_id,
            full_name,
            preferred_name,
            date_of_birth,
            place_of_birth,
            gender,
            nationality,
            government_id_type,
            government_id_number,
            address,
            phone,
            email,
            is_independent,
            current_status,
            enrollment_date,
            notes,
            created_at,
            created_by
        )
        VALUES
        (
            @Id,
            @StudentNumber,
            @ApplicantId,
            @FullName,
            @PreferredName,
            @DateOfBirth,
            @PlaceOfBirth,
            @Gender,
            @Nationality,
            @GovernmentIdType,
            @GovernmentIdNumber,
            @Address,
            @Phone,
            @Email,
            0,
            'enrolled',
            @EnrollmentDate,
            @Notes,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    private const string _insertEnrollmentSql = """
        INSERT INTO students.enrollments
        (
            id,
            student_id,
            application_id,
            enrollment_type,
            enrollment_date,
            initial_campus,
            initial_level,
            initial_academic_year,
            initial_program,
            initial_cohort,
            approved_by,
            enrollment_payment_confirmed,
            payment_reference,
            payment_waived,
            waiver_reference,
            notes,
            created_at,
            created_by
        )
        VALUES
        (
            @Id,
            @StudentId,
            @ApplicationId,
            'initial',
            @EnrollmentDate,
            @InitialCampus,
            @InitialLevel,
            @InitialAcademicYear,
            @InitialProgram,
            @InitialCohort,
            @ApprovedBy,
            @EnrollmentPaymentConfirmed,
            @PaymentReference,
            @PaymentWaived,
            @WaiverReference,
            @Notes,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    private const string _promoteVerifiedDocumentsSql = """
        INSERT INTO students.student_documents
        (
            id,
            student_id,
            document_type,
            submission_status,
            submitted_date,
            verified_date,
            verified_by,
            file_reference,
            file_name,
            file_size_bytes,
            content_type,
            valid_until,
            notes,
            created_at,
            created_by
        )
        SELECT
            NEWID(),
            @StudentId,
            document_type,
            submission_status,
            submitted_date,
            verified_date,
            verified_by,
            file_reference,
            file_name,
            file_size_bytes,
            content_type,
            NULL,
            notes,
            SYSUTCDATETIME(),
            @CreatedBy
        FROM students.applicant_documents
        WHERE application_id = @ApplicationId
          AND submission_status = 'verified';
        """;

    private const string _selectApplicationContactsSql = """
        SELECT
            full_name AS FullName,
            relationship AS RelationshipType,
            phone AS Phone,
            email AS Email,
            address AS Address,
            is_primary AS IsPrimaryContact
        FROM students.admission_application_contacts
        WHERE application_id = @ApplicationId;
        """;

    private const string _selectGuardianByIdSql = """
        SELECT id
        FROM students.guardians
        WHERE id = @GuardianId;
        """;

    private const string _selectGuardianByGovIdSql = """
        SELECT TOP 1 id
        FROM students.guardians
        WHERE government_id_number = @GovernmentIdNumber;
        """;

    private const string _selectGuardianByNameAndPhoneSql = """
        SELECT TOP 1 id
        FROM students.guardians
        WHERE full_name = @FullName AND phone = @Phone;
        """;

    private const string _insertGuardianSql = """
        INSERT INTO students.guardians
        (
            id,
            full_name,
            government_id_type,
            government_id_number,
            phone,
            email,
            address,
            occupation,
            notes,
            created_at,
            created_by
        )
        VALUES
        (
            @Id,
            @FullName,
            @GovernmentIdType,
            @GovernmentIdNumber,
            @Phone,
            @Email,
            @Address,
            @Occupation,
            @Notes,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    private const string _demotePrimaryContactsSql = """
        UPDATE students.student_guardian_relationships
        SET is_primary_contact = 0,
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE student_id = @StudentId AND is_primary_contact = 1 AND is_active = 1;
        """;

    private const string _insertRelationshipSql = """
        INSERT INTO students.student_guardian_relationships
        (
            id,
            student_id,
            guardian_id,
            relationship_type,
            is_primary_contact,
            is_emergency_contact,
            is_financial_contact,
            is_authorized_pickup,
            is_active,
            created_at,
            created_by
        )
        VALUES
        (
            @Id,
            @StudentId,
            @GuardianId,
            @RelationshipType,
            @IsPrimaryContact,
            @IsEmergencyContact,
            @IsFinancialContact,
            @IsAuthorizedPickup,
            1,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    /// <summary>
    /// Executes the enrollment workflow for an accepted applicant.
    /// </summary>
    /// <param name="request">The enrollment request parameters.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A result containing the enrolled student identifiers.</returns>
    public async Task<Result<EnrollAcceptedApplicantResponse>> HandleAsync(
        EnrollAcceptedApplicantRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Admission prerequisite verification (status == 'decided' and decision_type == 'accepted')
        var application = await connection.QuerySingleOrDefaultAsync<ApplicationPrerequisiteRow>(
            new CommandDefinition(
                _selectApplicationPrerequisiteSql,
                new
                {
                    ApplicationId = request.ApplicationId,
                    ApplicantId = request.ApplicantId,
                    AcademicYearId = request.AcademicYearId
                },
                cancellationToken: cancellationToken));

        if (application is null)
        {
            // Fallback query by ApplicantId directly
            application = await connection.QuerySingleOrDefaultAsync<ApplicationPrerequisiteRow>(
                new CommandDefinition(
                    _fallbackSelectApplicationSql,
                    new { ApplicantId = request.ApplicantId },
                    cancellationToken: cancellationToken));
        }

        if (application is null
            || !string.Equals(application.ApplicationStatus, "decided", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(application.DecisionType, "accepted", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<EnrollAcceptedApplicantResponse>(
                DomainError.Conflict(
                    "Students.Enrollment.ApplicantNotAccepted",
                    "The applicant does not have an accepted admission decision."));
        }

        // 2. Verify applicant is not already enrolled in students.students
        var existingStudentId = await connection.ExecuteScalarAsync<Guid?>(
            new CommandDefinition(
                _checkStudentExistsSql,
                new { ApplicantId = request.ApplicantId },
                cancellationToken: cancellationToken));

        if (existingStudentId.HasValue)
        {
            return Result.Failure<EnrollAcceptedApplicantResponse>(
                DomainError.Conflict(
                    "Students.Enrollment.AlreadyEnrolled",
                    $"Applicant with ID '{request.ApplicantId}' has already been enrolled as an official student."));
        }

        // 3. Payment gate evaluation (GAP-01, OQ-003)
        bool paymentWaived;
        string? waiverReference = null;
        bool? paymentConfirmed = null;
        string? paymentReference = null;

        if (request.PaymentWaived)
        {
            if (string.IsNullOrWhiteSpace(request.WaiverReference))
            {
                return Result.Failure<EnrollAcceptedApplicantResponse>(
                    DomainError.Validation(
                        "Students.Enrollment.WaiverReferenceRequired",
                        "A waiver reference or authorized executive decree is required when payment is waived."));
            }

            paymentWaived = true;
            waiverReference = request.WaiverReference.Trim();
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(request.PaymentReference))
            {
                paymentWaived = false;
                paymentConfirmed = true;
                paymentReference = request.PaymentReference.Trim();
            }
            else if (request.CheckFinanceModule == true)
            {
                var financeResult = await financeModule.IsEnrollmentFeePaidAsync(
                    request.ApplicantId,
                    request.AcademicYearId,
                    cancellationToken);

                if (!financeResult.IsSuccess || !financeResult.Value)
                {
                    return Result.Failure<EnrollAcceptedApplicantResponse>(
                        DomainError.Conflict(
                            "Students.Enrollment.PaymentRequired",
                            "Enrollment payment has not been confirmed by the Finance module."));
                }

                paymentWaived = false;
                paymentConfirmed = true;
                paymentReference = "FINANCE-AUTO-CLEARED";
            }
            else
            {
                return Result.Failure<EnrollAcceptedApplicantResponse>(
                    DomainError.Conflict(
                        "Students.Enrollment.PaymentRequired",
                        "Enrollment payment has not been confirmed. A valid payment reference or authorized fee waiver override is required."));
            }
        }

        // 4. Retrieve applicant biographical attributes
        var applicant = await connection.QuerySingleOrDefaultAsync<ApplicantBiographicalRow>(
            new CommandDefinition(
                _selectApplicantSql,
                new { ApplicantId = request.ApplicantId },
                cancellationToken: cancellationToken));

        if (applicant is null)
        {
            return Result.Failure<EnrollAcceptedApplicantResponse>(
                DomainError.NotFound(
                    "Students.Enrollment.ApplicantNotFound",
                    $"Originating applicant record '{request.ApplicantId}' was not found."));
        }

        // 5. Authoritative student number generation
        var enrollmentYear = DateTime.UtcNow.Year;
        if (!string.IsNullOrWhiteSpace(request.InitialAcademicYear))
        {
            var yearToken = request.InitialAcademicYear.Split(['/', '-'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (int.TryParse(yearToken, out var parsedYear) && parsedYear >= 2000 && parsedYear <= 2100)
            {
                enrollmentYear = parsedYear;
            }
        }

        var studentNumber = await studentNumberGenerator.GenerateNextStudentNumberAsync(enrollmentYear, cancellationToken);

        var studentId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var auditUser = currentUser.Username ?? "system";
        var enrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // 6. Execute atomic Dapper transaction
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            // 6.1 Insert into students.students
            await connection.ExecuteAsync(new CommandDefinition(
                _insertStudentSql,
                new
                {
                    Id = studentId,
                    StudentNumber = studentNumber,
                    ApplicantId = request.ApplicantId,
                    FullName = applicant.FullName,
                    PreferredName = applicant.PreferredName,
                    DateOfBirth = applicant.DateOfBirth,
                    PlaceOfBirth = applicant.PlaceOfBirth,
                    Gender = applicant.Gender,
                    Nationality = applicant.Nationality ?? "Indonesian",
                    GovernmentIdType = applicant.GovernmentIdType,
                    GovernmentIdNumber = applicant.GovernmentIdNumber,
                    Address = applicant.Address,
                    Phone = applicant.Phone,
                    Email = applicant.Email,
                    EnrollmentDate = enrollmentDate,
                    Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                    CreatedBy = auditUser
                },
                transaction: tx,
                cancellationToken: cancellationToken));

            // 6.2 Insert into students.enrollments
            await connection.ExecuteAsync(new CommandDefinition(
                _insertEnrollmentSql,
                new
                {
                    Id = enrollmentId,
                    StudentId = studentId,
                    ApplicationId = application.ApplicationId,
                    EnrollmentDate = enrollmentDate,
                    InitialCampus = request.InitialCampus.Trim(),
                    InitialLevel = request.InitialLevel.Trim(),
                    InitialAcademicYear = request.InitialAcademicYear.Trim(),
                    InitialProgram = string.IsNullOrWhiteSpace(request.InitialProgram) ? null : request.InitialProgram.Trim(),
                    InitialCohort = string.IsNullOrWhiteSpace(request.InitialCohort) ? null : request.InitialCohort.Trim(),
                    ApprovedBy = auditUser,
                    EnrollmentPaymentConfirmed = paymentConfirmed,
                    PaymentReference = paymentReference,
                    PaymentWaived = paymentWaived,
                    WaiverReference = waiverReference,
                    Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                    CreatedBy = auditUser
                },
                transaction: tx,
                cancellationToken: cancellationToken));

            // 6.3 Promote verified applicant documents
            await connection.ExecuteAsync(new CommandDefinition(
                _promoteVerifiedDocumentsSql,
                new
                {
                    StudentId = studentId,
                    ApplicationId = application.ApplicationId,
                    CreatedBy = auditUser
                },
                transaction: tx,
                cancellationToken: cancellationToken));

            // 6.4 Ingest guardians & relationships
            IReadOnlyList<EnrollGuardianRequest> guardiansToProcess = request.Guardians ?? [];

            if (guardiansToProcess.Count == 0)
            {
                var contacts = (await connection.QueryAsync<AdmissionContactRow>(new CommandDefinition(
                    _selectApplicationContactsSql,
                    new { ApplicationId = application.ApplicationId },
                    transaction: tx,
                    cancellationToken: cancellationToken))).ToList();

                guardiansToProcess = contacts.Select(c => new EnrollGuardianRequest(
                    FullName: c.FullName,
                    RelationshipType: c.RelationshipType,
                    Phone: c.Phone,
                    Email: c.Email,
                    Address: c.Address,
                    IsPrimaryContact: c.IsPrimaryContact,
                    IsEmergencyContact: c.IsPrimaryContact,
                    IsFinancialContact: c.IsPrimaryContact,
                    IsAuthorizedPickup: c.IsPrimaryContact)).ToList();
            }

            foreach (var g in guardiansToProcess)
            {
                if (string.IsNullOrWhiteSpace(g.FullName))
                {
                    continue;
                }

                var trimmedName = g.FullName.Trim();
                var govIdNum = string.IsNullOrWhiteSpace(g.GovernmentIdNumber) ? null : g.GovernmentIdNumber.Trim();
                var phone = string.IsNullOrWhiteSpace(g.Phone) ? null : g.Phone.Trim();

                Guid? existingGuardianId = null;

                if (g.GuardianId.HasValue)
                {
                    existingGuardianId = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                        _selectGuardianByIdSql,
                        new { GuardianId = g.GuardianId.Value },
                        transaction: tx,
                        cancellationToken: cancellationToken));
                }
                else if (!string.IsNullOrWhiteSpace(govIdNum))
                {
                    existingGuardianId = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                        _selectGuardianByGovIdSql,
                        new { GovernmentIdNumber = govIdNum },
                        transaction: tx,
                        cancellationToken: cancellationToken));
                }
                else if (!string.IsNullOrWhiteSpace(phone))
                {
                    existingGuardianId = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                        _selectGuardianByNameAndPhoneSql,
                        new { FullName = trimmedName, Phone = phone },
                        transaction: tx,
                        cancellationToken: cancellationToken));
                }

                var guardianId = existingGuardianId ?? Guid.NewGuid();

                if (!existingGuardianId.HasValue)
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        _insertGuardianSql,
                        new
                        {
                            Id = guardianId,
                            FullName = trimmedName,
                            GovernmentIdType = string.IsNullOrWhiteSpace(g.GovernmentIdType) ? null : g.GovernmentIdType.Trim(),
                            GovernmentIdNumber = govIdNum,
                            Phone = phone,
                            Email = string.IsNullOrWhiteSpace(g.Email) ? null : g.Email.Trim(),
                            Address = string.IsNullOrWhiteSpace(g.Address) ? null : g.Address.Trim(),
                            Occupation = string.IsNullOrWhiteSpace(g.Occupation) ? null : g.Occupation.Trim(),
                            Notes = string.IsNullOrWhiteSpace(g.Notes) ? null : g.Notes.Trim(),
                            CreatedBy = auditUser
                        },
                        transaction: tx,
                        cancellationToken: cancellationToken));
                }

                // Enforce single active primary contact per student
                if (g.IsPrimaryContact)
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        _demotePrimaryContactsSql,
                        new
                        {
                            StudentId = studentId,
                            UpdatedBy = auditUser
                        },
                        transaction: tx,
                        cancellationToken: cancellationToken));
                }

                var relType = GuardianRelationshipTypes.IsValid(g.RelationshipType)
                    ? GuardianRelationshipTypes.Normalize(g.RelationshipType)
                    : GuardianRelationshipTypes.Other;

                await connection.ExecuteAsync(new CommandDefinition(
                    _insertRelationshipSql,
                    new
                    {
                        Id = Guid.NewGuid(),
                        StudentId = studentId,
                        GuardianId = guardianId,
                        RelationshipType = relType,
                        IsPrimaryContact = g.IsPrimaryContact,
                        IsEmergencyContact = g.IsEmergencyContact,
                        IsFinancialContact = g.IsFinancialContact,
                        IsAuthorizedPickup = g.IsAuthorizedPickup,
                        CreatedBy = auditUser
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));
            }

            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }

        // 7. Strictly post-commit domain event publication (ADR-003, TD-04)
        await publisher.Publish(
            new StudentEnrolledEvent(studentId, request.ApplicantId, request.AcademicYearId),
            cancellationToken);

        return Result.Success(new EnrollAcceptedApplicantResponse(
            studentId,
            studentNumber,
            enrollmentId));
    }

    private sealed record ApplicationPrerequisiteRow(
        Guid ApplicationId,
        Guid ApplicantId,
        Guid AcademicYearId,
        string ApplicationStatus,
        string? DecisionType);

    private sealed record ApplicantBiographicalRow(
        Guid Id,
        string FullName,
        string? PreferredName,
        DateOnly? DateOfBirth,
        string? PlaceOfBirth,
        string? Gender,
        string? Nationality,
        string? GovernmentIdType,
        string? GovernmentIdNumber,
        string? Address,
        string? Phone,
        string? Email);

    private sealed record AdmissionContactRow(
        string FullName,
        string RelationshipType,
        string? Phone,
        string? Email,
        string? Address,
        bool IsPrimaryContact);
}
