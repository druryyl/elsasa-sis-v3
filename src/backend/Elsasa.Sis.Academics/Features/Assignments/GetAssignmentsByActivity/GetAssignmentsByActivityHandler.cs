namespace Elsasa.Sis.Academics.Features.Assignments.GetAssignmentsByActivity;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for querying coursework assignments belonging to a specific learning activity (P5-S10).
/// </summary>
public sealed class GetAssignmentsByActivityHandler(ISqlConnectionFactory connectionFactory)
{
    static GetAssignmentsByActivityHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkActivitySql = """
        SELECT COUNT(1)
        FROM academics.learning_activities
        WHERE id = @ActivityId;
        """;

    private const string _selectAssignmentsSql = """
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
            created_by AS CreatedBy,
            updated_at AS UpdatedAt,
            updated_by AS UpdatedBy
        FROM academics.assignments
        WHERE learning_activity_id = @ActivityId
        ORDER BY release_date ASC, created_at ASC;
        """;

    public async Task<Result<IReadOnlyList<ActivityAssignmentItemDto>>> HandleAsync(
        Guid activityId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var activityExists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _checkActivitySql,
            new { ActivityId = activityId },
            cancellationToken: cancellationToken));

        if (activityExists == 0)
        {
            return Result.Failure<IReadOnlyList<ActivityAssignmentItemDto>>(
                DomainError.NotFound(
                    "Academic.LearningActivityNotFound",
                    $"Learning activity with ID '{activityId}' was not found."));
        }

        var assignments = await connection.QueryAsync<ActivityAssignmentItemDto>(new CommandDefinition(
            _selectAssignmentsSql,
            new { ActivityId = activityId },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<ActivityAssignmentItemDto>>(assignments.AsList());
    }
}
