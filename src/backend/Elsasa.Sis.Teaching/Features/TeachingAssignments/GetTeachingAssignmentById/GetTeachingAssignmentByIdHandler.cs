namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.GetTeachingAssignmentById;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying a teaching assignment by its unique identifier (TD-01, P3-S06).
/// </summary>
public sealed class GetTeachingAssignmentByIdHandler(ISqlConnectionFactory connectionFactory)
{
    static GetTeachingAssignmentByIdHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAssignmentByIdSql = """
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
        WHERE ta.id = @Id;
        """;

    public async Task<Result<TeachingAssignmentDto>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var assignment = await connection.QuerySingleOrDefaultAsync<TeachingAssignmentDto>(new CommandDefinition(
            _selectAssignmentByIdSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (assignment is null)
        {
            return Result.Failure<TeachingAssignmentDto>(DomainError.NotFound(
                "Teaching.AssignmentNotFound",
                $"Teaching assignment with ID '{id}' was not found."));
        }

        return Result.Success(assignment);
    }
}
