namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.SubmitStudentLearningPlan;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Extensions.Logging;

/// <summary>
/// Feature handler validating completeness and submitting a draft student learning plan for Academic Authority review (UC-ACD-09, P3-S08).
/// Enforces:
/// - Plan must exist and have status = 'draft'.
/// - Plan must have at least one constituent unit or adjustment.
/// - Logs submission event and marks plan ready for review via audit/notes.
/// </summary>
public sealed partial class SubmitStudentLearningPlanHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    ILogger<SubmitStudentLearningPlanHandler> logger)
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Student learning plan {PlanId} (v{Version}) submitted for Academic Authority review by {User} (Units: {UnitsCount}, Adjustments: {AdjustmentsCount})")]
    private static partial void LogPlanSubmitted(
        ILogger logger,
        Guid planId,
        int version,
        string user,
        int unitsCount,
        int adjustmentsCount);
    private const string _selectPlanSql = """
        SELECT
            id AS Id,
            status AS Status,
            version AS Version,
            notes AS Notes
        FROM academics.student_learning_plans
        WHERE id = @Id;
        """;

    private const string _selectCountsSql = """
        SELECT
            (SELECT COUNT(1) FROM academics.slp_learning_units WHERE student_learning_plan_id = @Id) AS UnitsCount,
            (SELECT COUNT(1) FROM academics.academic_plan_adjustments WHERE student_learning_plan_id = @Id) AS AdjustmentsCount;
        """;

    private const string _updatePlanSql = """
        UPDATE academics.student_learning_plans
        SET
            notes = CASE WHEN @Notes IS NOT NULL AND LEN(TRIM(@Notes)) > 0 THEN @Notes ELSE notes END,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private sealed record PlanRow(Guid Id, string Status, int Version, string? Notes);
    private sealed record PlanCounts(int UnitsCount, int AdjustmentsCount);

    /// <summary>
    /// Executes completeness checks and marks the student learning plan ready for Academic Authority review.
    /// </summary>
    /// <param name="id">The unique identifier of the student learning plan.</param>
    /// <param name="request">Optional submission notes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Submission confirmation response or domain error.</returns>
    public async Task<Result<SubmitStudentLearningPlanResponse>> HandleAsync(
        Guid id,
        SubmitStudentLearningPlanRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result.Failure<SubmitStudentLearningPlanResponse>(
                DomainError.Validation("Academics.InvalidPlanId", "Student learning plan ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify plan exists
        var plan = await connection.QuerySingleOrDefaultAsync<PlanRow>(
            new CommandDefinition(
                _selectPlanSql,
                new { Id = id },
                cancellationToken: cancellationToken));

        if (plan is null)
        {
            return Result.Failure<SubmitStudentLearningPlanResponse>(
                DomainError.NotFound(
                    "Academics.LearningPlanNotFound",
                    $"Student learning plan with ID '{id}' was not found."));
        }

        // 2. Validate plan is currently in 'draft' status
        if (!string.Equals(plan.Status, "draft", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<SubmitStudentLearningPlanResponse>(
                DomainError.Conflict(
                    "Academics.PlanNotDraft",
                    $"Student learning plan cannot be submitted because its status is '{plan.Status}'. Only draft plans can be submitted for review."));
        }

        // 3. Validate plan has at least one constituent unit or adjustment
        var counts = await connection.QuerySingleOrDefaultAsync<PlanCounts>(
            new CommandDefinition(
                _selectCountsSql,
                new { Id = id },
                cancellationToken: cancellationToken));

        var unitsCount = counts?.UnitsCount ?? 0;
        var adjustmentsCount = counts?.AdjustmentsCount ?? 0;

        if (unitsCount == 0 && adjustmentsCount == 0)
        {
            return Result.Failure<SubmitStudentLearningPlanResponse>(
                DomainError.Validation(
                    "Academics.EmptyLearningPlan",
                    "Student learning plan must contain at least one constituent unit or academic adjustment before submission for review."));
        }

        // 4. Mark plan ready for review and update notes/audit
        var now = DateTimeOffset.UtcNow;
        var submittedBy = currentUser.Username ?? currentUser.Email ?? "coordinator";
        var finalNotes = !string.IsNullOrWhiteSpace(request?.Notes) ? request.Notes.Trim() : plan.Notes;

        await connection.ExecuteAsync(new CommandDefinition(
            _updatePlanSql,
            new
            {
                Id = id,
                Notes = finalNotes,
                UpdatedAt = now,
                UpdatedBy = submittedBy
            },
            cancellationToken: cancellationToken));

        // 5. Log submission event
        LogPlanSubmitted(
            logger,
            id,
            plan.Version,
            submittedBy,
            unitsCount,
            adjustmentsCount);

        return Result.Success(new SubmitStudentLearningPlanResponse(
            id,
            plan.Status,
            plan.Version,
            now,
            submittedBy,
            finalNotes));
    }
}
