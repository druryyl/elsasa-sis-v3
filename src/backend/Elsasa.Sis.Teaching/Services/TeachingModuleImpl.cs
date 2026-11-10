namespace Elsasa.Sis.Teaching.Services;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Elsasa.Sis.Teaching.Features.TeacherProfiles.GetTeacherProfileById;

/// <summary>
/// Authoritative implementation of the synchronous Teaching cross-domain query contract (ADR-001, ADR-003, ADR-010, TD-08).
/// Provides parameterized read operations for teacher profiles, active faculty summaries, and authorization checks.
/// Strictly exposes read inquiries with zero cross-domain write methods.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Standard SIS module implementation naming")]
public sealed class TeachingModuleImpl(
    ISqlConnectionFactory connectionFactory,
    GetTeacherProfileByIdHandler? profileByIdHandler = null) : ITeachingModule
{
    static TeachingModuleImpl()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private readonly GetTeacherProfileByIdHandler _profileByIdHandler = profileByIdHandler ?? new GetTeacherProfileByIdHandler(connectionFactory);

    private const string _isTeacherAuthorizedSql = """
        SELECT TOP 1 1
        FROM teaching.teaching_authorizations ta
        INNER JOIN teaching.teacher_profiles tp ON tp.id = ta.teacher_profile_id
        LEFT JOIN teaching.teaching_capabilities tc ON tc.id = ta.authorized_capability_id
        WHERE ta.teacher_profile_id = @TeacherId
          AND tp.standing = 'active'
          AND ta.status IN ('active', 'authorized_under_supervision')
          AND (@Campus IS NULL OR @Campus = '' OR @Campus = 'All' OR ta.campus_scope = 'All' OR ta.campus_scope = @Campus)
          AND (
              tc.capability_code = @SubjectOrCapability
              OR tc.capability_name = @SubjectOrCapability
              OR ta.scope_description LIKE '%' + @SubjectOrCapability + '%'
              OR (ta.program_scope IS NOT NULL AND ta.program_scope LIKE '%' + @SubjectOrCapability + '%')
          )
          AND ta.effective_start_date <= @EffectiveDate
          AND (ta.effective_end_date IS NULL OR ta.effective_end_date >= @EffectiveDate);
        """;

    private const string _selectActiveTeachersSql = """
        SELECT
            tp.id AS Id,
            tp.teacher_number AS TeacherNumber,
            tp.full_name AS FullName,
            tp.display_name AS DisplayName,
            tp.teaching_role_id AS TeachingRoleId,
            COALESCE(tr.name, N'') AS TeachingRoleName,
            tp.campus_affiliation AS CampusAffiliation,
            tp.department AS Department,
            tp.standing AS Standing,
            tp.email AS Email,
            tp.phone AS Phone,
            CASE 
                WHEN EXISTS (
                    SELECT 1 
                    FROM teaching.teaching_authorizations ta 
                    WHERE ta.teacher_profile_id = tp.id 
                      AND ta.status IN ('active', 'authorized_under_supervision')
                      AND ta.unverified_credential_warning = 1
                ) THEN CAST(1 AS BIT) 
                ELSE CAST(0 AS BIT) 
            END AS HasUnverifiedWarning,
            (
                SELECT COUNT(1) 
                FROM teaching.teaching_authorizations ta 
                WHERE ta.teacher_profile_id = tp.id 
                      AND ta.status IN ('active', 'authorized_under_supervision')
            ) AS ActiveAuthorizationsCount
        FROM teaching.teacher_profiles tp
        LEFT JOIN teaching.teaching_roles tr ON tr.id = tp.teaching_role_id
        WHERE tp.standing = 'active'
          AND (@Campus IS NULL OR @Campus = '' OR @Campus = 'All' OR tp.campus_affiliation = @Campus OR tp.campus_affiliation = 'All')
        ORDER BY tp.full_name ASC, tp.teacher_number ASC;
        """;

    /// <inheritdoc />
    public async Task<Result<TeacherProfileDto?>> GetTeacherProfileAsync(
        Guid teacherId,
        CancellationToken ct = default)
    {
        if (teacherId == Guid.Empty)
        {
            return Result.Success<TeacherProfileDto?>(null);
        }

        try
        {
            var result = await _profileByIdHandler.HandleAsync(teacherId, ct);
            if (result.IsFailure)
            {
                if (result.Error.Type == ErrorType.NotFound || result.Error.Code == "Teaching.TeacherNotFound")
                {
                    return Result.Success<TeacherProfileDto?>(null);
                }

                return Result.Failure<TeacherProfileDto?>(result.Error);
            }

            return Result.Success<TeacherProfileDto?>(result.Value);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<TeacherProfileDto?>(
                DomainError.Failure(
                    "Teaching.TeacherProfileQueryFailed",
                    $"Failed to retrieve teacher profile dossier for '{teacherId}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> IsTeacherAuthorizedAsync(
        Guid teacherId,
        string subjectOrCapability,
        string campus,
        DateOnly effectiveDate,
        CancellationToken ct = default)
    {
        if (teacherId == Guid.Empty || string.IsNullOrWhiteSpace(subjectOrCapability))
        {
            return Result.Success(false);
        }

        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var normalizedSubject = subjectOrCapability.Trim();
            var normalizedCampus = string.IsNullOrWhiteSpace(campus) ? null : campus.Trim();

            var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                _isTeacherAuthorizedSql,
                new
                {
                    TeacherId = teacherId,
                    SubjectOrCapability = normalizedSubject,
                    Campus = normalizedCampus,
                    EffectiveDate = effectiveDate
                },
                cancellationToken: ct));

            return Result.Success(exists);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<bool>(
                DomainError.Failure(
                    "Teaching.AuthorizationCheckFailed",
                    $"Failed to check authorization for teacher '{teacherId}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TeacherSummaryDto>>> GetActiveTeachersAsync(
        string? campus = null,
        CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var normalizedCampus = string.IsNullOrWhiteSpace(campus) ? null : campus.Trim();

            var teachers = (await connection.QueryAsync<TeacherSummaryDto>(new CommandDefinition(
                _selectActiveTeachersSql,
                new { Campus = normalizedCampus },
                cancellationToken: ct))).AsList();

            return Result.Success<IReadOnlyList<TeacherSummaryDto>>(teachers);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<TeacherSummaryDto>>(
                DomainError.Failure(
                    "Teaching.ActiveTeachersQueryFailed",
                    $"Failed to query active teachers: {ex.Message}"));
        }
    }
}
