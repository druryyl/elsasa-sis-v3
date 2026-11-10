namespace Elsasa.Sis.Teaching.Features.TeacherProfiles.RegisterTeacherProfile;

using Dapper;
using Elsasa.Sis.Contracts.Teaching.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler registering a candidate teacher profile into pending_approval standing (P2-S05).
/// </summary>
public sealed class RegisterTeacherProfileHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    private const string _checkRoleSql = """
        SELECT TOP 1 1
        FROM teaching.teaching_roles
        WHERE id = @TeachingRoleId AND is_active = 1;
        """;

    private const string _checkTeacherNumberSql = """
        SELECT TOP 1 1
        FROM teaching.teacher_profiles
        WHERE teacher_number = @TeacherNumber;
        """;

    private const string _checkNationalIdSql = """
        SELECT TOP 1 1
        FROM teaching.teacher_profiles
        WHERE national_id_or_external_ref = @NationalId;
        """;

    private const string _countTeacherNumberPrefixSql = """
        SELECT COUNT(1)
        FROM teaching.teacher_profiles
        WHERE teacher_number LIKE @Prefix;
        """;

    private const string _insertTeacherProfileSql = """
        INSERT INTO teaching.teacher_profiles (
            id,
            teacher_number,
            full_name,
            display_name,
            employee_ref,
            national_id_or_external_ref,
            email,
            phone,
            teaching_role_id,
            campus_affiliation,
            department,
            standing,
            effective_from,
            effective_to,
            standing_reason,
            approved_by,
            approved_date,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @TeacherNumber,
            @FullName,
            @DisplayName,
            @EmployeeRef,
            @NationalIdOrExternalRef,
            @Email,
            @Phone,
            @TeachingRoleId,
            @CampusAffiliation,
            @Department,
            'pending_approval',
            @EffectiveFrom,
            @EffectiveTo,
            @StandingReason,
            NULL,
            NULL,
            @Notes,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    public async Task<Result<RegisterTeacherProfileResponse>> HandleAsync(
        RegisterTeacherProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify teaching role exists and is active
        var roleExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            _checkRoleSql,
            new { TeachingRoleId = request.TeachingRoleId },
            cancellationToken: cancellationToken));

        if (!roleExists)
        {
            return Result.Failure<RegisterTeacherProfileResponse>(DomainError.NotFound(
                "Teaching.TeachingRoleNotFound",
                $"The teaching role with ID '{request.TeachingRoleId}' does not exist or is inactive."));
        }

        // 2. Validate or generate teacher_number
        string teacherNumber;
        if (!string.IsNullOrWhiteSpace(request.TeacherNumber))
        {
            teacherNumber = request.TeacherNumber.Trim();
            var duplicateNumber = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                _checkTeacherNumberSql,
                new { TeacherNumber = teacherNumber },
                cancellationToken: cancellationToken));

            if (duplicateNumber)
            {
                return Result.Failure<RegisterTeacherProfileResponse>(DomainError.Conflict(
                    "Teaching.DuplicateTeacherNumber",
                    $"A teacher profile with number '{teacherNumber}' already exists."));
            }
        }
        else
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"TCH-{year}-";
            var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                _countTeacherNumberPrefixSql,
                new { Prefix = prefix + "%" },
                cancellationToken: cancellationToken));

            var seq = count + 1;
            while (true)
            {
                var candidate = $"{prefix}{seq:D4}";
                var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                    _checkTeacherNumberSql,
                    new { TeacherNumber = candidate },
                    cancellationToken: cancellationToken));

                if (!exists)
                {
                    teacherNumber = candidate;
                    break;
                }
                seq++;
            }
        }

        // 3. Verify national_id_or_external_ref uniqueness if provided
        var normalizedNationalId = string.IsNullOrWhiteSpace(request.NationalIdOrExternalRef)
            ? null
            : request.NationalIdOrExternalRef.Trim();

        if (normalizedNationalId is not null)
        {
            var duplicateNationalId = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                _checkNationalIdSql,
                new { NationalId = normalizedNationalId },
                cancellationToken: cancellationToken));

            if (duplicateNationalId)
            {
                return Result.Failure<RegisterTeacherProfileResponse>(DomainError.Conflict(
                    "Teaching.DuplicateNationalId",
                    $"A teacher profile with national ID or external reference '{normalizedNationalId}' already exists."));
            }
        }

        var profileId = Guid.NewGuid();
        var normalizedFullName = request.FullName.Trim();
        var normalizedDisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim();
        var normalizedEmployeeRef = string.IsNullOrWhiteSpace(request.EmployeeRef) ? null : request.EmployeeRef.Trim();
        var normalizedEmail = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var normalizedPhone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        var normalizedCampus = string.IsNullOrWhiteSpace(request.CampusAffiliation) ? "All" : request.CampusAffiliation.Trim();
        var normalizedDepartment = string.IsNullOrWhiteSpace(request.Department) ? null : request.Department.Trim();
        var normalizedNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        var auditUser = currentUser.Username ?? "system";

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                _insertTeacherProfileSql,
                new
                {
                    Id = profileId,
                    TeacherNumber = teacherNumber,
                    FullName = normalizedFullName,
                    DisplayName = normalizedDisplayName,
                    EmployeeRef = normalizedEmployeeRef,
                    NationalIdOrExternalRef = normalizedNationalId,
                    Email = normalizedEmail,
                    Phone = normalizedPhone,
                    TeachingRoleId = request.TeachingRoleId,
                    CampusAffiliation = normalizedCampus,
                    Department = normalizedDepartment,
                    EffectiveFrom = request.EffectiveFrom.ToDateTime(TimeOnly.MinValue),
                    EffectiveTo = request.EffectiveTo?.ToDateTime(TimeOnly.MinValue),
                    StandingReason = "Registered candidate profile pending formal authority approval.",
                    Notes = normalizedNotes,
                    CreatedBy = auditUser
                },
                cancellationToken: cancellationToken));
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            if (ex.Message.Contains("UQ_teaching_teacher_profiles_number", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<RegisterTeacherProfileResponse>(DomainError.Conflict(
                    "Teaching.DuplicateTeacherNumber",
                    $"A teacher profile with number '{teacherNumber}' already exists."));
            }

            if (ex.Message.Contains("UQ_teaching_teacher_profiles_national_id", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<RegisterTeacherProfileResponse>(DomainError.Conflict(
                    "Teaching.DuplicateNationalId",
                    $"A teacher profile with national ID or external reference '{normalizedNationalId}' already exists."));
            }

            throw;
        }

        // 4. Publish in-memory domain event
        var domainEvent = new TeacherProfileCreatedEvent(
            profileId,
            teacherNumber,
            normalizedFullName,
            "pending_approval",
            auditUser,
            DateTimeOffset.UtcNow);

        await publisher.Publish(domainEvent, cancellationToken);

        return Result.Success(new RegisterTeacherProfileResponse(
            profileId,
            teacherNumber,
            normalizedFullName,
            "pending_approval"));
    }
}
