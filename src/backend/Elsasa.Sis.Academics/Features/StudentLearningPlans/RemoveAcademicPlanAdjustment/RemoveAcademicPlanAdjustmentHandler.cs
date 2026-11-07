namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.RemoveAcademicPlanAdjustment;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for removing an academic plan adjustment from a draft student learning plan (UC-ACD-09, P3-S07, TD-03).
/// Enforces:
/// - Parent plan must exist and have status = 'draft'. Modifications to active/revised/archived plans are rejected.
/// - Adjustment must exist on the specified student learning plan.
/// </summary>
public sealed class RemoveAcademicPlanAdjustmentHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _selectPlanSql = """
        SELECT
            id AS Id,
            status AS Status
        FROM academics.student_learning_plans
        WHERE id = @Id;
        """;

    private const string _selectAdjustmentSql = """
        SELECT id
        FROM academics.academic_plan_adjustments
        WHERE id = @AdjustmentId
          AND student_learning_plan_id = @PlanId;
        """;

    private const string _deleteAdjustmentSql = """
        DELETE FROM academics.academic_plan_adjustments
        WHERE id = @AdjustmentId
          AND student_learning_plan_id = @PlanId;
        """;

    private sealed record PlanRow(Guid Id, string Status);

    /// <summary>
    /// Executes removal of an adjustment from a draft student learning plan.
    /// </summary>
    /// <param name="planId">The parent student learning plan identifier.</param>
    /// <param name="adjustmentId">The adjustment identifier to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or domain error.</returns>
    public async Task<Result> HandleAsync(
        Guid planId,
        Guid adjustmentId,
        CancellationToken cancellationToken = default)
    {
        if (planId == Guid.Empty)
        {
            return Result.Failure(
                DomainError.Validation("Academics.InvalidPlanId", "Student learning plan ID cannot be empty."));
        }

        if (adjustmentId == Guid.Empty)
        {
            return Result.Failure(
                DomainError.Validation("Academics.InvalidAdjustmentId", "Academic plan adjustment ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Fetch parent plan to verify existence and draft status
        var plan = await connection.QuerySingleOrDefaultAsync<PlanRow>(
            new CommandDefinition(
                _selectPlanSql,
                new { Id = planId },
                cancellationToken: cancellationToken));

        if (plan is null)
        {
            return Result.Failure(
                DomainError.NotFound(
                    "Academics.LearningPlanNotFound",
                    $"Student learning plan with ID '{planId}' was not found."));
        }

        if (!string.Equals(plan.Status, "draft", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(
                DomainError.Conflict(
                    "Academics.ImmutableLearningPlan",
                    $"Student learning plan with ID '{planId}' cannot be modified because its status is '{plan.Status}'. Only draft plans can be modified."));
        }

        // 2. Verify adjustment exists on the plan
        var existingAdjustmentId = await connection.QueryFirstOrDefaultAsync<Guid?>(
            new CommandDefinition(
                _selectAdjustmentSql,
                new { PlanId = planId, AdjustmentId = adjustmentId },
                cancellationToken: cancellationToken));

        if (!existingAdjustmentId.HasValue || existingAdjustmentId.Value == Guid.Empty)
        {
            return Result.Failure(
                DomainError.NotFound(
                    "Academics.AdjustmentNotFound",
                    $"Academic plan adjustment with ID '{adjustmentId}' was not found on student learning plan '{planId}'."));
        }

        // 3. Delete adjustment row
        await connection.ExecuteAsync(new CommandDefinition(
            _deleteAdjustmentSql,
            new { PlanId = planId, AdjustmentId = adjustmentId },
            cancellationToken: cancellationToken));

        return Result.Success();
    }
}
