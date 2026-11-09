namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.ArchiveStudentLearningPlan;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler transitioning student learning plans to 'archived' or 'completed' status (UC-ACD-09, P2-S04).
/// </summary>
public sealed class ArchiveStudentLearningPlanHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectPlanSql = """
        SELECT
            id AS Id,
            status AS Status,
            version AS Version,
            notes AS Notes
        FROM academics.student_learning_plans
        WHERE id = @Id;
        """;

    private const string _updatePlanSql = """
        UPDATE academics.student_learning_plans
        SET
            status = @TargetStatus,
            notes = CASE WHEN @Notes IS NOT NULL THEN @Notes ELSE notes END,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private sealed record PlanRow(Guid Id, string Status, int Version, string? Notes);

    /// <summary>
    /// Executes archival or completion transition on a student learning plan.
    /// </summary>
    /// <param name="id">The unique identifier of the student learning plan.</param>
    /// <param name="request">The archival request payload (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Archived plan response or domain error.</returns>
    public async Task<Result<ArchiveStudentLearningPlanResponse>> HandleAsync(
        Guid id,
        ArchiveStudentLearningPlanRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result.Failure<ArchiveStudentLearningPlanResponse>(
                DomainError.Validation("Academics.InvalidPlanId", "Student learning plan ID cannot be empty."));
        }

        var targetStatus = string.IsNullOrWhiteSpace(request?.TargetStatus)
            ? "archived"
            : request.TargetStatus.Trim().ToLowerInvariant();

        if (!string.Equals(targetStatus, "archived", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(targetStatus, "completed", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ArchiveStudentLearningPlanResponse>(
                DomainError.Validation(
                    "Academics.InvalidTargetStatus",
                    "Target status must be either 'archived' or 'completed'."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Fetch current status
        var existing = await connection.QuerySingleOrDefaultAsync<PlanRow>(
            new CommandDefinition(
                _selectPlanSql,
                new { Id = id },
                cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<ArchiveStudentLearningPlanResponse>(
                DomainError.NotFound(
                    "Academics.LearningPlanNotFound",
                    $"Student learning plan with ID '{id}' was not found."));
        }

        if (string.Equals(existing.Status, targetStatus, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ArchiveStudentLearningPlanResponse>(
                DomainError.Conflict(
                    "Academics.AlreadyInTargetStatus",
                    $"Student learning plan is already in '{existing.Status}' status."));
        }

        // 2. Perform transition
        var now = DateTimeOffset.UtcNow;
        var updatedBy = currentUser.Username ?? currentUser.Email ?? "system";

        await connection.ExecuteAsync(new CommandDefinition(
            _updatePlanSql,
            new
            {
                Id = id,
                TargetStatus = targetStatus,
                request?.Notes,
                UpdatedAt = now,
                UpdatedBy = updatedBy
            },
            cancellationToken: cancellationToken));

        return Result.Success(new ArchiveStudentLearningPlanResponse(
            id,
            targetStatus,
            existing.Version,
            now,
            updatedBy));
    }
}
