namespace Elsasa.Sis.Academics.Features.Assignments.ReleaseAssignment;

using Dapper;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler for releasing a coursework assignment (TD-05, TD-08, UC-ACD-04, P5-S10).
/// </summary>
public sealed class ReleaseAssignmentHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static ReleaseAssignmentHandler()
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
            status = 'released',
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

    public async Task<Result<ReleaseAssignmentResponse>> HandleAsync(
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
            return Result.Failure<ReleaseAssignmentResponse>(
                DomainError.NotFound(
                    "Academic.AssignmentNotFound",
                    $"Coursework assignment with ID '{id}' was not found."));
        }

        // 2. Verify assignment is in draft status
        if (!string.Equals(assignment.Status, "draft", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ReleaseAssignmentResponse>(
                DomainError.Conflict(
                    "Academic.InvalidAssignmentStatus",
                    $"Cannot release assignment in '{assignment.Status}' status. Only draft assignments can be released."));
        }

        // 3. Update status to released
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

        // 4. Publish post-commit domain event alerting student workspaces (TD-08, ADR-003)
        await publisher.Publish(
            new AssignmentReleasedEvent(
                assignment.Id,
                assignment.LearningActivityId,
                assignment.Title,
                assignment.DueDate),
            cancellationToken);

        var response = new ReleaseAssignmentResponse(
            assignment.Id,
            assignment.LearningActivityId,
            assignment.Title,
            assignment.Description,
            assignment.ReleaseDate,
            assignment.DueDate,
            "released",
            assignment.Notes,
            assignment.CreatedAt,
            assignment.CreatedBy,
            updatedAt,
            updatedBy);

        return Result.Success(response);
    }
}
