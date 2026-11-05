namespace Elsasa.Sis.Academics.Features.LearningStructures.GetLearningStructures;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing paginated inquiries for learning structure frameworks (ADR-004, ADR-005, P3-S06).
/// Supports filtering by academic period, lifecycle status, and keyword search.
/// </summary>
public sealed class GetLearningStructuresHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _countSql = """
        SELECT COUNT(1)
        FROM academics.learning_structures ls
        WHERE (@AcademicPeriodId IS NULL OR ls.academic_period_id = @AcademicPeriodId)
          AND (@Status IS NULL OR ls.status = @Status)
          AND (@SearchPattern IS NULL OR ls.name LIKE @SearchPattern OR ls.version LIKE @SearchPattern);
        """;

    private const string _selectSql = """
        SELECT
            ls.id AS Id,
            ls.name AS Name,
            ls.description AS Description,
            ls.academic_period_id AS AcademicPeriodId,
            ls.academic_period_label AS AcademicPeriodLabel,
            ls.version AS Version,
            ls.effective_date AS EffectiveDate,
            ls.end_date AS EndDate,
            ls.status AS Status,
            ls.approved_by AS ApprovedBy,
            ls.approved_date AS ApprovedDate,
            ls.supersedes_id AS SupersedesId,
            ls.created_at AS CreatedAt,
            ls.created_by AS CreatedBy,
            ls.updated_at AS UpdatedAt,
            ls.updated_by AS UpdatedBy,
            (SELECT COUNT(1) FROM academics.learning_units u WHERE u.learning_structure_id = ls.id) AS UnitCount,
            (SELECT COUNT(1) FROM academics.academic_requirements r WHERE r.learning_structure_id = ls.id) AS RequirementCount,
            CASE WHEN ls.supersedes_id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasPredecessor
        FROM academics.learning_structures ls
        WHERE (@AcademicPeriodId IS NULL OR ls.academic_period_id = @AcademicPeriodId)
          AND (@Status IS NULL OR ls.status = @Status)
          AND (@SearchPattern IS NULL OR ls.name LIKE @SearchPattern OR ls.version LIKE @SearchPattern)
        ORDER BY ls.created_at DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        """;

    /// <summary>
    /// Executes the paginated query against <c>academics.learning_structures</c>.
    /// </summary>
    public async Task<Result<GetLearningStructuresResponse>> HandleAsync(
        Guid? academicPeriodId,
        string? status,
        string? search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = page is null or < 1 ? 1 : page.Value;
        var safePageSize = pageSize is null or < 1 ? 20 : (pageSize.Value > 100 ? 100 : pageSize.Value);
        var offset = (safePage - 1) * safePageSize;
        var searchPattern = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";
        var trimmedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim();

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var parameters = new
        {
            AcademicPeriodId = academicPeriodId,
            Status = trimmedStatus,
            SearchPattern = searchPattern,
            Offset = offset,
            PageSize = safePageSize
        };

        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _countSql,
            parameters,
            cancellationToken: cancellationToken));

        var items = await connection.QueryAsync<LearningStructureListItemDto>(new CommandDefinition(
            _selectSql,
            parameters,
            cancellationToken: cancellationToken));

        return Result.Success(GetLearningStructuresResponse.Create(
            items.AsList(),
            totalCount,
            safePage,
            safePageSize));
    }
}
