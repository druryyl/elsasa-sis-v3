namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.RemoveSlpLearningUnit;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler removing a constituent learning unit from a draft student learning plan (UC-ACD-09, P2-S05).
/// Validates draft status of parent plan and removes the constituent unit row.
/// </summary>
public sealed class RemoveSlpLearningUnitHandler(
    ISqlConnectionFactory connectionFactory)
{
    private const string _selectPlanSql = """
        SELECT
            id AS Id,
            status AS Status
        FROM academics.student_learning_plans
        WHERE id = @PlanId;
        """;

    private const string _selectSlpUnitSql = """
        SELECT TOP 1 id AS Id
        FROM academics.slp_learning_units
        WHERE student_learning_plan_id = @PlanId
          AND (id = @UnitId OR learning_unit_id = @UnitId);
        """;

    private const string _deleteSlpUnitSql = """
        DELETE FROM academics.slp_learning_units
        WHERE id = @Id;
        """;

    private sealed record PlanRow(Guid Id, string Status);
    private sealed record SlpUnitRow(Guid Id);

    /// <summary>
    /// Executes removing a constituent learning unit from a draft student learning plan.
    /// </summary>
    /// <param name="planId">The unique identifier of the student learning plan.</param>
    /// <param name="unitId">The unique identifier of the constituent unit entry or learning unit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or domain error.</returns>
    public async Task<Result> HandleAsync(
        Guid planId,
        Guid unitId,
        CancellationToken cancellationToken = default)
    {
        if (planId == Guid.Empty)
        {
            return Result.Failure(
                DomainError.Validation("Academics.InvalidPlanId", "Student learning plan ID cannot be empty."));
        }

        if (unitId == Guid.Empty)
        {
            return Result.Failure(
                DomainError.Validation("Academics.InvalidUnitId", "Constituent unit ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Validate parent plan exists and has status = 'draft'
        var plan = await connection.QuerySingleOrDefaultAsync<PlanRow>(
            new CommandDefinition(
                _selectPlanSql,
                new { PlanId = planId },
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

        // 2. Validate constituent unit row exists
        var slpUnit = await connection.QuerySingleOrDefaultAsync<SlpUnitRow>(
            new CommandDefinition(
                _selectSlpUnitSql,
                new { PlanId = planId, UnitId = unitId },
                cancellationToken: cancellationToken));

        if (slpUnit is null)
        {
            return Result.Failure(
                DomainError.NotFound(
                    "Academics.SlpLearningUnitNotFound",
                    $"Constituent learning unit '{unitId}' was not found in student learning plan '{planId}'."));
        }

        // 3. Delete row from academics.slp_learning_units
        await connection.ExecuteAsync(
            new CommandDefinition(
                _deleteSlpUnitSql,
                new { slpUnit.Id },
                cancellationToken: cancellationToken));

        return Result.Success();
    }
}
