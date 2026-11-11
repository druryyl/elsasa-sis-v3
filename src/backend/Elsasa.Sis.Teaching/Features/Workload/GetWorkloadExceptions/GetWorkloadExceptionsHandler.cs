namespace Elsasa.Sis.Teaching.Features.Workload.GetWorkloadExceptions;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for querying workload exceptions with optional filtering by period, teacher, and status (TD-04, P3-S10).
/// </summary>
public sealed class GetWorkloadExceptionsHandler(ISqlConnectionFactory connectionFactory)
{
    static GetWorkloadExceptionsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    public async Task<Result<IReadOnlyList<WorkloadExceptionDto>>> HandleAsync(
        Guid? academicPeriodId = null,
        Guid? teacherProfileId = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (academicPeriodId.HasValue && academicPeriodId.Value != Guid.Empty)
        {
            conditions.Add("ta.academic_period_id = @AcademicPeriodId");
            parameters.Add("AcademicPeriodId", academicPeriodId.Value);
        }

        if (teacherProfileId.HasValue && teacherProfileId.Value != Guid.Empty)
        {
            conditions.Add("we.teacher_profile_id = @TeacherProfileId");
            parameters.Add("TeacherProfileId", teacherProfileId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            conditions.Add("we.status = @Status");
            parameters.Add("Status", status.Trim().ToLowerInvariant());
        }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;

        var sql = $"""
            SELECT
                we.id AS Id,
                we.exception_number AS ExceptionNumber,
                we.teaching_assignment_id AS TeachingAssignmentId,
                we.teacher_profile_id AS TeacherProfileId,
                we.workload_policy_id AS WorkloadPolicyId,
                we.exceeded_hours AS ExceededHours,
                we.justification AS Justification,
                we.recommended_by AS RecommendedBy,
                we.recommended_date AS RecommendedDate,
                we.approved_by AS ApprovedBy,
                we.approved_date AS ApprovedDate,
                we.effective_start_date AS EffectiveStartDate,
                we.effective_end_date AS EffectiveEndDate,
                we.status AS Status,
                we.decision_remarks AS DecisionRemarks,
                we.created_at AS CreatedAt
            FROM teaching.workload_exceptions we
            INNER JOIN teaching.teaching_assignments ta ON ta.id = we.teaching_assignment_id
            {whereClause}
            ORDER BY we.created_at DESC;
            """;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<WorkloadExceptionDto>(new CommandDefinition(
            sql,
            parameters,
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<WorkloadExceptionDto>>(rows.ToList().AsReadOnly());
    }
}
