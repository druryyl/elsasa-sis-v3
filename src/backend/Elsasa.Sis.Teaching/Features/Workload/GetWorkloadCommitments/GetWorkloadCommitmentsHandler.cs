namespace Elsasa.Sis.Teaching.Features.Workload.GetWorkloadCommitments;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying materialized faculty capacity snapshots joined with teacher profiles and active policies (TD-04, P3-S09).
/// </summary>
public sealed class GetWorkloadCommitmentsHandler(ISqlConnectionFactory connectionFactory)
{
    static GetWorkloadCommitmentsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    public async Task<Result<IReadOnlyList<TeacherWorkloadCommitmentDto>>> HandleAsync(
        Guid? academicPeriodId = null,
        Guid? teacherProfileId = null,
        string? status = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (academicPeriodId.HasValue && academicPeriodId.Value != Guid.Empty)
        {
            conditions.Add("wc.academic_period_id = @AcademicPeriodId");
            parameters.Add("AcademicPeriodId", academicPeriodId.Value);
        }

        if (teacherProfileId.HasValue && teacherProfileId.Value != Guid.Empty)
        {
            conditions.Add("wc.teacher_profile_id = @TeacherProfileId");
            parameters.Add("TeacherProfileId", teacherProfileId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            conditions.Add("wc.status = @Status");
            parameters.Add("Status", status.Trim().ToLowerInvariant());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            conditions.Add("(tp.full_name LIKE @SearchPattern OR tp.teacher_number LIKE @SearchPattern OR tp.display_name LIKE @SearchPattern)");
            parameters.Add("SearchPattern", $"%{search.Trim()}%");
        }

        var whereSql = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;

        var querySql = $"""
            SELECT
                wc.id AS Id,
                wc.teacher_profile_id AS TeacherProfileId,
                tp.teacher_number AS TeacherNumber,
                tp.full_name AS FullName,
                tp.display_name AS DisplayName,
                tp.campus_affiliation AS CampusAffiliation,
                tp.standing AS Standing,
                wc.academic_period_id AS AcademicPeriodId,
                wc.workload_policy_id AS WorkloadPolicyId,
                wp.code AS PolicyCode,
                wp.name AS PolicyName,
                wp.max_standard_capacity AS MaxStandardCapacity,
                wp.warning_threshold AS WarningThreshold,
                wp.prohibited_threshold AS ProhibitedThreshold,
                wc.planned_workload_hours AS PlannedWorkloadHours,
                wc.committed_workload_hours AS CommittedWorkloadHours,
                wc.max_capacity_limit_hours AS MaxCapacityLimitHours,
                wc.remaining_capacity_hours AS RemainingCapacityHours,
                wc.status AS Status,
                wc.active_assignment_count AS ActiveAssignmentCount,
                wc.approved_exception_count AS ApprovedExceptionCount,
                wc.as_of_timestamp AS AsOfTimestamp,
                wc.created_at AS CreatedAt
            FROM teaching.workload_commitments wc
            INNER JOIN teaching.teacher_profiles tp ON tp.id = wc.teacher_profile_id
            INNER JOIN teaching.workload_policies wp ON wp.id = wc.workload_policy_id
            {whereSql}
            ORDER BY tp.full_name ASC;
            """;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var items = await connection.QueryAsync<TeacherWorkloadCommitmentDto>(new CommandDefinition(
            querySql,
            parameters,
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<TeacherWorkloadCommitmentDto>>(items.ToList().AsReadOnly());
    }
}
