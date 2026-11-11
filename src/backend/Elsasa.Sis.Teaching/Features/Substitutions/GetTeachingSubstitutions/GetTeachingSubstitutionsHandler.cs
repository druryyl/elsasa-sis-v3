namespace Elsasa.Sis.Teaching.Features.Substitutions.GetTeachingSubstitutions;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying teaching substitutions with filters and pagination (TD-05, P4-S11).
/// </summary>
public sealed class GetTeachingSubstitutionsHandler(ISqlConnectionFactory connectionFactory)
{
    static GetTeachingSubstitutionsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    public async Task<Result<PaginatedList<TeachingSubstitutionDto>>> HandleAsync(
        Guid? originalAssignmentId = null,
        Guid? substituteTeacherId = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize < 1 ? 20 : (pageSize > 100 ? 100 : pageSize);
        var offset = (safePage - 1) * safePageSize;

        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (originalAssignmentId.HasValue && originalAssignmentId.Value != Guid.Empty)
        {
            conditions.Add("s.original_assignment_id = @OriginalAssignmentId");
            parameters.Add("OriginalAssignmentId", originalAssignmentId.Value);
        }

        if (substituteTeacherId.HasValue && substituteTeacherId.Value != Guid.Empty)
        {
            conditions.Add("s.substitute_teacher_id = @SubstituteTeacherId");
            parameters.Add("SubstituteTeacherId", substituteTeacherId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            conditions.Add("s.status = @Status");
            parameters.Add("Status", status.Trim().ToLowerInvariant());
        }

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", safePageSize);

        var whereSql = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;

        var countSql = $"""
            SELECT COUNT(1)
            FROM teaching.teaching_substitutions s
            {whereSql};
            """;

        var querySql = $"""
            SELECT
                s.id AS Id,
                s.substitution_number AS SubstitutionNumber,
                s.original_assignment_id AS OriginalAssignmentId,
                s.original_teacher_id AS OriginalTeacherId,
                s.substitute_teacher_id AS SubstituteTeacherId,
                s.coverage_start_date AS CoverageStartDate,
                s.coverage_end_date AS CoverageEndDate,
                s.coverage_scope_type AS CoverageScopeType,
                s.partial_scope_details AS PartialScopeDetails,
                s.reason AS Reason,
                s.workload_impact_hours AS WorkloadImpactHours,
                s.status AS Status,
                s.handover_notes AS HandoverNotes,
                s.approved_by AS ApprovedBy,
                s.approved_date AS ApprovedDate,
                s.created_at AS CreatedAt
            FROM teaching.teaching_substitutions s
            {whereSql}
            ORDER BY s.created_at DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            countSql,
            parameters,
            cancellationToken: cancellationToken));

        var items = await connection.QueryAsync<TeachingSubstitutionDto>(new CommandDefinition(
            querySql,
            parameters,
            cancellationToken: cancellationToken));

        var paginated = PaginatedList<TeachingSubstitutionDto>.Create(items.ToList(), totalCount, safePage, safePageSize);
        return Result.Success(paginated);
    }
}
