namespace Elsasa.Sis.Teaching.Features.Reassignments.GetTeachingReassignmentById;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying a teaching reassignment by ID (TD-01, P4-S12).
/// </summary>
public sealed class GetTeachingReassignmentByIdHandler(ISqlConnectionFactory connectionFactory)
{
    static GetTeachingReassignmentByIdHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectSql = """
        SELECT
            tr.id AS Id,
            tr.reassignment_number AS ReassignmentNumber,
            tr.original_assignment_id AS OriginalAssignmentId,
            tr.predecessor_teacher_id AS PredecessorTeacherId,
            tr.new_teacher_id AS NewTeacherId,
            tr.replacement_assignment_id AS ReplacementAssignmentId,
            tr.effective_date AS EffectiveDate,
            tr.reassignment_type AS ReassignmentType,
            tr.reason AS Reason,
            tr.requires_handover AS RequiresHandover,
            tr.handover_status AS HandoverStatus,
            tr.handover_completed_date AS HandoverCompletedDate,
            tr.handover_notes AS HandoverNotes,
            tr.status AS Status,
            tr.approved_by AS ApprovedBy,
            tr.approved_date AS ApprovedDate,
            tr.created_at AS CreatedAt
        FROM teaching.teaching_reassignments tr
        WHERE tr.id = @Id;
        """;

    public async Task<Result<TeachingReassignmentDto>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<TeachingReassignmentDto>(new CommandDefinition(
            _selectSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (row is null)
        {
            return Result.Failure<TeachingReassignmentDto>(DomainError.NotFound(
                "Teaching.ReassignmentNotFound",
                $"Teaching reassignment with ID '{id}' was not found."));
        }

        return Result.Success(row);
    }
}
