namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.AcknowledgeTeachingAssignment;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler acknowledging a teaching assignment by the assigned teacher (TD-01, P3-S06).
/// </summary>
public sealed class AcknowledgeTeachingAssignmentHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static AcknowledgeTeachingAssignmentHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAssignmentSql = """
        SELECT
            id AS Id,
            assignment_number AS AssignmentNumber,
            status AS Status,
            teacher_acknowledged AS TeacherAcknowledged
        FROM teaching.teaching_assignments
        WHERE id = @Id;
        """;

    private const string _acknowledgeAssignmentSql = """
        UPDATE teaching.teaching_assignments
        SET teacher_acknowledged = 1,
            teacher_acknowledged_date = CAST(SYSUTCDATETIME() AS DATE),
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    public async Task<Result<AcknowledgeTeachingAssignmentResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var assignment = await connection.QuerySingleOrDefaultAsync<AssignmentRow>(new CommandDefinition(
            _selectAssignmentSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (assignment is null)
        {
            return Result.Failure<AcknowledgeTeachingAssignmentResponse>(DomainError.NotFound(
                "Teaching.AssignmentNotFound",
                $"Teaching assignment with ID '{id}' was not found."));
        }

        if (assignment.Status is "cancelled" or "withdrawn" or "rejected")
        {
            return Result.Failure<AcknowledgeTeachingAssignmentResponse>(DomainError.Conflict(
                "Teaching.InvalidAssignmentStatus",
                $"Cannot acknowledge assignment '{assignment.AssignmentNumber}' in '{assignment.Status}' status."));
        }

        var auditUser = currentUser.Username ?? "system";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await connection.ExecuteAsync(new CommandDefinition(
            _acknowledgeAssignmentSql,
            new
            {
                Id = id,
                UpdatedBy = auditUser
            },
            cancellationToken: cancellationToken));

        return Result.Success(new AcknowledgeTeachingAssignmentResponse(
            assignment.Id,
            assignment.AssignmentNumber,
            true,
            today,
            DateTimeOffset.UtcNow));
    }

    private sealed record AssignmentRow(Guid Id, string AssignmentNumber, string Status, bool TeacherAcknowledged);
}
