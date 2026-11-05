namespace Elsasa.Sis.Academics.Features.LearningActivities.CancelLearningActivity;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for cancelling a scheduled learning activity (ADR-004, ADR-005, P3-S07).
/// </summary>
public sealed class CancelLearningActivityHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static CancelLearningActivityHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectActivitySql = """
        SELECT
            id AS Id,
            learning_unit_id AS LearningUnitId,
            title AS Title,
            status AS Status
        FROM academics.learning_activities
        WHERE id = @Id;
        """;

    private const string _updateCancelSql = """
        UPDATE academics.learning_activities
        SET
            status = 'cancelled',
            notes = @CancellationReason,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private sealed record ActivityStatusRow(Guid Id, Guid LearningUnitId, string Title, string Status);

    public async Task<Result<CancelLearningActivityResponse>> HandleAsync(
        Guid id,
        CancelLearningActivityRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify activity exists
        var activity = await connection.QuerySingleOrDefaultAsync<ActivityStatusRow>(new CommandDefinition(
            _selectActivitySql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (activity is null)
        {
            return Result.Failure<CancelLearningActivityResponse>(
                DomainError.NotFound(
                    "Academic.LearningActivityNotFound",
                    $"Learning activity with ID '{id}' was not found."));
        }

        // 2. Verify planned status
        if (!string.Equals(activity.Status, "planned", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<CancelLearningActivityResponse>(
                DomainError.Conflict(
                    "Academic.InvalidActivityStatus",
                    $"Cannot cancel learning activity in '{activity.Status}' status. Only planned activities can be cancelled."));
        }

        // 3. Update to cancelled
        var updatedAt = DateTimeOffset.UtcNow;
        var updatedBy = currentUser.Username ?? "system";

        await connection.ExecuteAsync(new CommandDefinition(
            _updateCancelSql,
            new
            {
                Id = id,
                request.CancellationReason,
                UpdatedAt = updatedAt,
                UpdatedBy = updatedBy
            },
            cancellationToken: cancellationToken));

        var response = new CancelLearningActivityResponse(
            activity.Id,
            activity.LearningUnitId,
            activity.Title,
            "cancelled",
            request.CancellationReason,
            updatedAt,
            updatedBy);

        return Result.Success(response);
    }
}
