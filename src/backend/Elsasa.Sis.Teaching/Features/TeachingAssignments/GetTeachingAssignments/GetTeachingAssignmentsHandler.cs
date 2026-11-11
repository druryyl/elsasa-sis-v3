namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.GetTeachingAssignments;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying teaching assignments with filters and pagination (TD-01, P3-S06).
/// </summary>
public sealed class GetTeachingAssignmentsHandler(ISqlConnectionFactory connectionFactory)
{
    static GetTeachingAssignmentsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    public async Task<Result<PaginatedList<TeachingAssignmentDto>>> HandleAsync(
        Guid? academicPeriodId = null,
        string? status = null,
        Guid? teacherProfileId = null,
        string? scopeType = null,
        Guid? assignmentRoleId = null,
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
            conditions.Add("ta.academic_period_id = @AcademicPeriodId");
            parameters.Add("AcademicPeriodId", academicPeriodId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            conditions.Add("ta.status = @Status");
            parameters.Add("Status", status.Trim().ToLowerInvariant());
        }

        if (teacherProfileId.HasValue && teacherProfileId.Value != Guid.Empty)
        {
            conditions.Add("ta.teacher_profile_id = @TeacherProfileId");
            parameters.Add("TeacherProfileId", teacherProfileId.Value);
        }

        if (!string.IsNullOrWhiteSpace(scopeType))
        {
            conditions.Add("ta.assignment_scope = @ScopeType");
            parameters.Add("ScopeType", scopeType.Trim().ToLowerInvariant());
        }

        if (assignmentRoleId.HasValue && assignmentRoleId.Value != Guid.Empty)
        {
            conditions.Add("ta.assignment_role_id = @AssignmentRoleId");
            parameters.Add("AssignmentRoleId", assignmentRoleId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            conditions.Add("(ta.assignment_number LIKE @SearchPattern OR ta.scope_label LIKE @SearchPattern)");
            parameters.Add("SearchPattern", $"%{search.Trim()}%");
        }

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", safePageSize);

        var whereSql = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;

        var countSql = $"""
            SELECT COUNT(1)
            FROM teaching.teaching_assignments ta
            {whereSql};
            """;

        var querySql = $"""
            SELECT
                ta.id AS Id,
                ta.assignment_number AS AssignmentNumber,
                ta.teaching_demand_id AS TeachingDemandId,
                ta.teacher_profile_id AS TeacherProfileId,
                ta.assignment_role_id AS AssignmentRoleId,
                ar.name AS AssignmentRoleName,
                ta.assignment_scope AS AssignmentScope,
                ta.scope_reference_id AS ScopeReferenceId,
                ta.scope_label AS ScopeLabel,
                ta.academic_period_id AS AcademicPeriodId,
                ta.effective_start_date AS EffectiveStartDate,
                ta.effective_end_date AS EffectiveEndDate,
                ta.workload_commitment_value AS WorkloadCommitmentValue,
                ta.workload_unit AS WorkloadUnit,
                ta.status AS Status,
                ta.approved_by AS ApprovedBy,
                ta.approved_date AS ApprovedDate,
                ta.teacher_acknowledged AS TeacherAcknowledged,
                ta.teacher_acknowledged_date AS TeacherAcknowledgedDate,
                ta.predecessor_assignment_id AS PredecessorAssignmentId,
                ta.notes AS Notes,
                ta.created_at AS CreatedAt
            FROM teaching.teaching_assignments ta
            LEFT JOIN teaching.assignment_roles ar ON ar.id = ta.assignment_role_id
            {whereSql}
            ORDER BY ta.created_at DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            countSql,
            parameters,
            cancellationToken: cancellationToken));

        var items = await connection.QueryAsync<TeachingAssignmentDto>(new CommandDefinition(
            querySql,
            parameters,
            cancellationToken: cancellationToken));

        var paginated = PaginatedList<TeachingAssignmentDto>.Create(
            items.ToList(),
            totalCount,
            safePage,
            safePageSize);

        return Result.Success(paginated);
    }
}
