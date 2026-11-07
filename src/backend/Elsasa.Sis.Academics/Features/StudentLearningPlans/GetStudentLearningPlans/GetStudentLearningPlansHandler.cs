namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.GetStudentLearningPlans;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying paginated/filterable learning plan roster (SCR-ACD-04, P2-S06).
/// </summary>
public sealed class GetStudentLearningPlansHandler(ISqlConnectionFactory connectionFactory)
{
    static GetStudentLearningPlansHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _countSql = """
        SELECT COUNT(1)
        FROM academics.student_learning_plans p
        LEFT JOIN students.students s ON s.id = p.student_id
        WHERE (@AcademicPeriodId IS NULL OR p.academic_period_id = @AcademicPeriodId)
          AND (@Status IS NULL OR p.status = @Status)
          AND (
              @SearchPattern IS NULL
              OR s.full_name LIKE @SearchPattern
              OR s.student_number LIKE @SearchPattern
          );
        """;

    private const string _itemsSql = """
        SELECT
            p.id AS Id,
            p.student_id AS StudentId,
            s.full_name AS StudentName,
            s.student_number AS StudentNis,
            p.academic_period_id AS AcademicPeriodId,
            p.academic_period_label AS AcademicPeriodLabel,
            p.status AS Status,
            p.version AS Version,
            (SELECT COUNT(1) FROM academics.slp_learning_units u WHERE u.student_learning_plan_id = p.id) AS UnitsCount,
            (SELECT COUNT(1) FROM academics.academic_plan_adjustments a WHERE a.student_learning_plan_id = p.id) AS AdjustmentsCount,
            p.created_at AS CreatedAt,
            p.updated_at AS UpdatedAt
        FROM academics.student_learning_plans p
        LEFT JOIN students.students s ON s.id = p.student_id
        WHERE (@AcademicPeriodId IS NULL OR p.academic_period_id = @AcademicPeriodId)
          AND (@Status IS NULL OR p.status = @Status)
          AND (
              @SearchPattern IS NULL
              OR s.full_name LIKE @SearchPattern
              OR s.student_number LIKE @SearchPattern
          )
        ORDER BY p.created_at DESC, p.id DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        """;

    /// <summary>
    /// Executes paginated query with filtering across period, status, and student search.
    /// </summary>
    /// <param name="query">The query filters and pagination arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged result containing learning plan list items.</returns>
    public async Task<Result<PagedResult<StudentLearningPlanListItemDto>>> HandleAsync(
        GetStudentLearningPlansQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var safePage = query.Page < 1 ? 1 : query.Page;
        var safePageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;
        var offset = (safePage - 1) * safePageSize;

        string? normalizedStatus = null;
        if (!string.IsNullOrWhiteSpace(query.Status) && !string.Equals(query.Status.Trim(), "all", StringComparison.OrdinalIgnoreCase))
        {
            normalizedStatus = query.Status.Trim();
        }

        string? searchPattern = null;
        if (!string.IsNullOrWhiteSpace(query.StudentSearch))
        {
            searchPattern = $"%{query.StudentSearch.Trim()}%";
        }

        var parameters = new
        {
            query.AcademicPeriodId,
            Status = normalizedStatus,
            SearchPattern = searchPattern,
            Offset = offset,
            PageSize = safePageSize
        };

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _countSql,
            parameters,
            cancellationToken: cancellationToken));

        var items = (await connection.QueryAsync<StudentLearningPlanListItemDto>(new CommandDefinition(
            _itemsSql,
            parameters,
            cancellationToken: cancellationToken))).AsList();

        var pagedResult = PagedResult<StudentLearningPlanListItemDto>.Create(
            items,
            totalCount,
            safePage,
            safePageSize);

        return Result.Success(pagedResult);
    }
}
