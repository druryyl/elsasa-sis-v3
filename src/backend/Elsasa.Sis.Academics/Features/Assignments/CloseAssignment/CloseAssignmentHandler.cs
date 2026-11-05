namespace Elsasa.Sis.Academics.Features.Assignments.CloseAssignment;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for closing a released coursework assignment (TD-05, UC-ACD-04, P5-S10).
/// </summary>
public sealed class CloseAssignmentHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static CloseAssignmentHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAssignmentSql = """
        SELECT
            id AS Id,
            learning_activity_id AS LearningActivityId,
            title AS Title,
            description AS Description,
            release_date AS ReleaseDate,
            due_date AS DueDate,
            status AS Status,
            notes AS Notes,
            created_at AS CreatedAt,
            created_by AS CreatedBy
        FROM academics.assignments
        WHERE id = @Id;
        """;

    private const string _updateAssignmentSql = """
        UPDATE academics.assignments
        SET
            status = 'closed',
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private sealed record AssignmentRow(
        Guid Id,
        Guid LearningActivityId,
        string Title,
        string? Description,
        DateOnly ReleaseDate,
        DateOnly? DueDate,
        string Status,
        string? Notes,
        DateTimeOffset CreatedAt,
        string CreatedBy);

    public async Task<Result<CloseAssignmentResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify assignment exists
        var assignment = await connection.QuerySingleOrDefaultAsync<AssignmentRow>(new CommandDefinition(
            _selectAssignmentSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (assignment is null)
        {
            return Result.Failure<CloseAssignmentResponse>(
                DomainError.NotFound(
                    "Academic.AssignmentNotFound",
                    $"Coursework assignment with ID '{id}' was not found."));
        }

        // 2. Verify assignment is in released status
        if (!string.Equals(assignment.Status, "released", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<CloseAssignmentResponse>(
                DomainError.Conflict(
                    "Academic.InvalidAssignmentStatus",
                    $"Cannot close assignment in '{assignment.Status}' status. Only released assignments can be closed."));
        }

        // 3. Update status to closed
        var updatedAt = DateTimeOffset.UtcNow;
        var updatedBy = currentUser.Username ?? "system";

        await connection.ExecuteAsync(new CommandDefinition(
            _updateAssignmentSql,
            new
            {
                Id = id,
                UpdatedAt = updatedAt,
                UpdatedBy = updatedBy
            },
            cancellationToken: cancellationToken));

        var response = new CloseAssignmentResponse(
            assignment.Id,
            assignment.LearningActivityId,
            assignment.Title,
            assignment.Description,
            assignment.ReleaseDate,
            assignment.DueDate,
            "closed",
            assignment.Notes,
            assignment.CreatedAt,
            assignment.CreatedBy,
            updatedAt,
            updatedBy);

        return Result.Success(response);
    }
}
