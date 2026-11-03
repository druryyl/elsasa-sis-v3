namespace Elsasa.Sis.Students.Features.Records.SearchStudents;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing paginated student searches across personal details, placements, and guardian contacts (ADR-004, ADR-005).
/// </summary>
public sealed class SearchStudentsHandler(ISqlConnectionFactory connectionFactory)
{
    static SearchStudentsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _searchSql = """
        SELECT COUNT(1)
        FROM students.students s
        OUTER APPLY (
            SELECT TOP (1)
                e.initial_campus,
                e.initial_level
            FROM students.enrollments e
            WHERE e.student_id = s.id
            ORDER BY e.enrollment_date DESC, e.created_at DESC
        ) latest_enrollment
        WHERE (@Status IS NULL OR s.current_status = @Status)
          AND (@Campus IS NULL OR latest_enrollment.initial_campus = @Campus)
          AND (@Level IS NULL OR latest_enrollment.initial_level = @Level)
          AND (@SearchPattern IS NULL OR (
                s.full_name LIKE @SearchPattern
                OR s.preferred_name LIKE @SearchPattern
                OR s.student_number LIKE @SearchPattern
                OR s.government_id_number LIKE @SearchPattern));

        SELECT
            s.id AS Id,
            s.student_number AS StudentNumber,
            s.full_name AS FullName,
            s.preferred_name AS PreferredName,
            s.current_status AS CurrentStatus,
            s.enrollment_date AS EnrollmentDate,
            COALESCE(latest_enrollment.initial_campus, N'') AS InitialCampus,
            COALESCE(latest_enrollment.initial_level, N'') AS InitialLevel,
            s.is_independent AS IsIndependent,
            primary_guardian.full_name AS PrimaryGuardianName,
            primary_guardian.phone AS PrimaryGuardianPhone
        FROM students.students s
        OUTER APPLY (
            SELECT TOP (1)
                e.initial_campus,
                e.initial_level
            FROM students.enrollments e
            WHERE e.student_id = s.id
            ORDER BY e.enrollment_date DESC, e.created_at DESC
        ) latest_enrollment
        OUTER APPLY (
            SELECT TOP (1)
                g.full_name,
                g.phone
            FROM students.student_guardian_relationships rel
            INNER JOIN students.guardians g ON g.id = rel.guardian_id
            WHERE rel.student_id = s.id
              AND rel.is_primary_contact = 1
              AND rel.is_active = 1
        ) primary_guardian
        WHERE (@Status IS NULL OR s.current_status = @Status)
          AND (@Campus IS NULL OR latest_enrollment.initial_campus = @Campus)
          AND (@Level IS NULL OR latest_enrollment.initial_level = @Level)
          AND (@SearchPattern IS NULL OR (
                s.full_name LIKE @SearchPattern
                OR s.preferred_name LIKE @SearchPattern
                OR s.student_number LIKE @SearchPattern
                OR s.government_id_number LIKE @SearchPattern))
        ORDER BY s.enrollment_date DESC, s.created_at DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        """;

    /// <summary>
    /// Executes parameterized student search query with pagination and total count calculation.
    /// </summary>
    public async Task<Result<SearchStudentsResponse>> HandleAsync(
        string? searchTerm,
        string? campus,
        string? level,
        string? status,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var effectivePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var effectivePageSize = pageSize < 1 ? 20 : (pageSize > 100 ? 100 : pageSize);
        var offset = (effectivePageNumber - 1) * effectivePageSize;
        var searchPattern = string.IsNullOrWhiteSpace(searchTerm) ? null : $"%{searchTerm.Trim()}%";

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        await using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            _searchSql,
            new
            {
                Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
                Campus = string.IsNullOrWhiteSpace(campus) ? null : campus.Trim(),
                Level = string.IsNullOrWhiteSpace(level) ? null : level.Trim(),
                SearchPattern = searchPattern,
                Offset = offset,
                PageSize = effectivePageSize
            },
            cancellationToken: cancellationToken));

        var totalCount = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<StudentListItemDto>()).AsList();

        var response = SearchStudentsResponse.Create(items, totalCount, effectivePageNumber, effectivePageSize);
        return Result.Success(response);
    }
}
