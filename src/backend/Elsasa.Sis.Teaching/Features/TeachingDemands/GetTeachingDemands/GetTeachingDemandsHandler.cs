namespace Elsasa.Sis.Teaching.Features.TeachingDemands.GetTeachingDemands;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying teaching demands worklist with filters and pagination (TD-02, P2-S04).
/// </summary>
public sealed class GetTeachingDemandsHandler(ISqlConnectionFactory connectionFactory)
{
    static GetTeachingDemandsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    public async Task<Result<PaginatedList<TeachingDemandDto>>> HandleAsync(
        Guid? academicPeriodId = null,
        string? sourceDomain = null,
        string? status = null,
        string? campusScope = null,
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize < 1 ? 20 : (pageSize > 100 ? 100 : pageSize);
        var offset = (safePage - 1) * safePageSize;

        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (academicPeriodId.HasValue && academicPeriodId.Value != Guid.Empty)
        {
            conditions.Add("td.academic_period_id = @AcademicPeriodId");
            parameters.Add("AcademicPeriodId", academicPeriodId.Value);
        }

        if (!string.IsNullOrWhiteSpace(sourceDomain))
        {
            conditions.Add("td.source_domain = @SourceDomain");
            parameters.Add("SourceDomain", sourceDomain.Trim());
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            conditions.Add("td.status = @Status");
            parameters.Add("Status", status.Trim().ToLowerInvariant());
        }

        if (!string.IsNullOrWhiteSpace(campusScope) && !campusScope.Trim().Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            conditions.Add("(td.campus_scope = @CampusScope OR td.campus_scope = 'All')");
            parameters.Add("CampusScope", campusScope.Trim());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            conditions.Add("(td.demand_code LIKE @SearchPattern OR td.source_work_label LIKE @SearchPattern OR td.required_capability_code LIKE @SearchPattern)");
            parameters.Add("SearchPattern", $"%{search.Trim()}%");
        }

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", safePageSize);

        var whereSql = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;

        var countSql = $"""
            SELECT COUNT(1)
            FROM teaching.teaching_demands td
            {whereSql};
            """;

        var querySql = $"""
            SELECT
                td.id AS Id,
                td.demand_code AS DemandCode,
                td.source_domain AS SourceDomain,
                td.source_work_reference AS SourceWorkReference,
                td.source_work_label AS SourceWorkLabel,
                td.academic_period_id AS AcademicPeriodId,
                td.required_capability_code AS RequiredCapabilityCode,
                td.required_assignment_role_id AS RequiredAssignmentRoleId,
                ar.name AS RequiredAssignmentRoleName,
                td.expected_workload_hours AS ExpectedWorkloadHours,
                td.workload_unit AS WorkloadUnit,
                td.requires_assessment_authority AS RequiresAssessmentAuthority,
                td.requested_start_date AS RequestedStartDate,
                td.requested_end_date AS RequestedEndDate,
                td.campus_scope AS CampusScope,
                td.required_teachers_count AS RequiredTeachersCount,
                td.status AS Status,
                td.requested_by AS RequestedBy,
                td.requested_at AS RequestedAt,
                td.decline_reason AS DeclineReason,
                td.clarification_notes AS ClarificationNotes,
                td.created_at AS CreatedAt
            FROM teaching.teaching_demands td
            LEFT JOIN teaching.assignment_roles ar ON ar.id = td.required_assignment_role_id
            {whereSql}
            ORDER BY td.created_at DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            countSql,
            parameters,
            cancellationToken: cancellationToken));

        var items = await connection.QueryAsync<TeachingDemandDto>(new CommandDefinition(
            querySql,
            parameters,
            cancellationToken: cancellationToken));

        var paginated = PaginatedList<TeachingDemandDto>.Create(
            items.ToList(),
            totalCount,
            safePage,
            safePageSize);

        return Result.Success(paginated);
    }
}
