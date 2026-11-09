namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.UpdateStudentLearningPlan;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler updating metadata and notes for a draft student learning plan (UC-ACD-09, P2-S04).
/// Enforces immutability guard: only plans in 'draft' status may be modified directly.
/// </summary>
public sealed class UpdateStudentLearningPlanHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectPlanSql = """
        SELECT
            id AS Id,
            status AS Status,
            version AS Version
        FROM academics.student_learning_plans
        WHERE id = @Id;
        """;

    private const string _updatePlanSql = """
        UPDATE academics.student_learning_plans
        SET
            notes = @Notes,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private sealed record PlanRow(Guid Id, string Status, int Version);

    /// <summary>
    /// Executes draft learning plan update.
    /// </summary>
    /// <param name="id">The unique identifier of the student learning plan.</param>
    /// <param name="request">The update request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated plan details or domain error.</returns>
    public async Task<Result<UpdateStudentLearningPlanResponse>> HandleAsync(
        Guid id,
        UpdateStudentLearningPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            return Result.Failure<UpdateStudentLearningPlanResponse>(
                DomainError.Validation("Academics.InvalidPlanId", "Student learning plan ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Fetch current status to enforce draft immutability guard
        var existing = await connection.QuerySingleOrDefaultAsync<PlanRow>(
            new CommandDefinition(
                _selectPlanSql,
                new { Id = id },
                cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<UpdateStudentLearningPlanResponse>(
                DomainError.NotFound(
                    "Academics.LearningPlanNotFound",
                    $"Student learning plan with ID '{id}' was not found."));
        }

        if (!string.Equals(existing.Status, "draft", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<UpdateStudentLearningPlanResponse>(
                DomainError.Conflict(
                    "Academics.ImmutableLearningPlan",
                    $"Student learning plan with ID '{id}' cannot be modified because its status is '{existing.Status}'. Only draft plans can be modified."));
        }

        // 2. Perform update
        var now = DateTimeOffset.UtcNow;
        var updatedBy = currentUser.Username ?? currentUser.Email ?? "system";

        await connection.ExecuteAsync(new CommandDefinition(
            _updatePlanSql,
            new
            {
                Id = id,
                request.Notes,
                UpdatedAt = now,
                UpdatedBy = updatedBy
            },
            cancellationToken: cancellationToken));

        return Result.Success(new UpdateStudentLearningPlanResponse(
            id,
            existing.Status,
            existing.Version,
            request.Notes,
            now,
            updatedBy));
    }
}
