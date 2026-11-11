namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.GetCapacityConflicts;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying capacity, schedule, or authorization conflicts with filters and pagination (TD-01, TD-05, P3-S07).
/// </summary>
public sealed class GetCapacityConflictsHandler(ISqlConnectionFactory connectionFactory)
{
    static GetCapacityConflictsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    public async Task<Result<PaginatedList<CapacityConflictDto>>> HandleAsync(
        Guid? academicPeriodId = null,
        string? severity = null,
        string? status = null,
        Guid? teacherProfileId = null,
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
            conditions.Add("""
                (
                    ta.academic_period_id = @AcademicPeriodId
                    OR td.academic_period_id = @AcademicPeriodId
                    OR EXISTS (
                        SELECT 1 FROM teaching.workload_commitments wc
                        WHERE wc.teacher_profile_id = cc.teacher_profile_id
                          AND wc.academic_period_id = @AcademicPeriodId
                    )
                )
                """);
            parameters.Add("AcademicPeriodId", academicPeriodId.Value);
        }

        if (!string.IsNullOrWhiteSpace(severity))
        {
            conditions.Add("cc.severity = @Severity");
            parameters.Add("Severity", severity.Trim().ToLowerInvariant());
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            conditions.Add("cc.status = @Status");
            parameters.Add("Status", status.Trim().ToLowerInvariant());
        }

        if (teacherProfileId.HasValue && teacherProfileId.Value != Guid.Empty)
        {
            conditions.Add("cc.teacher_profile_id = @TeacherProfileId");
            parameters.Add("TeacherProfileId", teacherProfileId.Value);
        }

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", safePageSize);

        var whereSql = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;

        var countSql = $"""
            SELECT COUNT(1)
            FROM teaching.capacity_conflicts cc
            LEFT JOIN teaching.teaching_assignments ta ON ta.id = cc.teaching_assignment_id
            LEFT JOIN teaching.teaching_demands td ON td.id = cc.teaching_demand_id
            {whereSql};
            """;

        var querySql = $"""
            SELECT
                cc.id AS Id,
                cc.teaching_assignment_id AS TeachingAssignmentId,
                cc.teaching_demand_id AS TeachingDemandId,
                cc.teacher_profile_id AS TeacherProfileId,
                cc.conflict_type AS ConflictType,
                cc.conflict_classification AS ConflictClassification,
                cc.severity AS Severity,
                cc.description AS Description,
                cc.detected_date AS DetectedDate,
                cc.status AS Status,
                cc.resolution_action AS ResolutionAction,
                cc.resolved_by AS ResolvedBy,
                cc.resolved_date AS ResolvedDate,
                cc.resolution_notes AS ResolutionNotes,
                cc.created_at AS CreatedAt
            FROM teaching.capacity_conflicts cc
            LEFT JOIN teaching.teaching_assignments ta ON ta.id = cc.teaching_assignment_id
            LEFT JOIN teaching.teaching_demands td ON td.id = cc.teaching_demand_id
            {whereSql}
            ORDER BY cc.created_at DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            countSql,
            parameters,
            cancellationToken: cancellationToken));

        var items = await connection.QueryAsync<CapacityConflictDto>(new CommandDefinition(
            querySql,
            parameters,
            cancellationToken: cancellationToken));

        var paginated = PaginatedList<CapacityConflictDto>.Create(
            items.ToList(),
            totalCount,
            safePage,
            safePageSize);

        return Result.Success(paginated);
    }
}
