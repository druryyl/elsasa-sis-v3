namespace Elsasa.Sis.Academics.Features.Assignments.CreateAssignment;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for creating coursework assignments under a learning activity (TD-05, UC-ACD-04, P5-S10).
/// </summary>
public sealed class CreateAssignmentHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static CreateAssignmentHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectActivitySql = """
        SELECT
            id AS Id,
            status AS Status
        FROM academics.learning_activities
        WHERE id = @ActivityId;
        """;

    private const string _insertAssignmentSql = """
        INSERT INTO academics.assignments (
            id,
            learning_activity_id,
            title,
            description,
            release_date,
            due_date,
            status,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @LearningActivityId,
            @Title,
            @Description,
            @ReleaseDate,
            @DueDate,
            'draft',
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private sealed record ActivityStatusRow(Guid Id, string Status);

    public async Task<Result<CreateAssignmentResponse>> HandleAsync(
        Guid activityId,
        CreateAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify parent learning activity exists
        var activity = await connection.QuerySingleOrDefaultAsync<ActivityStatusRow>(new CommandDefinition(
            _selectActivitySql,
            new { ActivityId = activityId },
            cancellationToken: cancellationToken));

        if (activity is null)
        {
            return Result.Failure<CreateAssignmentResponse>(
                DomainError.NotFound(
                    "Academic.LearningActivityNotFound",
                    $"Learning activity with ID '{activityId}' was not found."));
        }

        // 2. Verify activity is not cancelled (TD-05)
        if (string.Equals(activity.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<CreateAssignmentResponse>(
                DomainError.Conflict(
                    "Academic.LearningActivityCancelled",
                    "Cannot create coursework assignments for a cancelled learning activity."));
        }

        // 3. Insert assignment
        var assignmentId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var createdBy = currentUser.Username ?? "system";
        const string status = "draft";

        await connection.ExecuteAsync(new CommandDefinition(
            _insertAssignmentSql,
            new
            {
                Id = assignmentId,
                LearningActivityId = activityId,
                Title = request.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                request.ReleaseDate,
                request.DueDate,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                CreatedAt = createdAt,
                CreatedBy = createdBy
            },
            cancellationToken: cancellationToken));

        var response = new CreateAssignmentResponse(
            assignmentId,
            activityId,
            request.Title.Trim(),
            string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            request.ReleaseDate,
            request.DueDate,
            status,
            string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            createdAt,
            createdBy);

        return Result.Success(response);
    }
}
