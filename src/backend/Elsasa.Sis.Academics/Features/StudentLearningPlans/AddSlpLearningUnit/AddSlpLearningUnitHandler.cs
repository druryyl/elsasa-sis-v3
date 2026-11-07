namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.AddSlpLearningUnit;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler linking a constituent learning unit to a draft student learning plan (UC-ACD-09, P2-S05).
/// Validates draft status of parent plan, existence of learning unit, and guards against duplicate entries.
/// </summary>
public sealed class AddSlpLearningUnitHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectPlanSql = """
        SELECT
            id AS Id,
            status AS Status
        FROM academics.student_learning_plans
        WHERE id = @PlanId;
        """;

    private const string _checkLearningUnitSql = """
        SELECT id AS Id
        FROM academics.learning_units
        WHERE id = @LearningUnitId;
        """;

    private const string _checkDuplicateUnitSql = """
        SELECT COUNT(1)
        FROM academics.slp_learning_units
        WHERE student_learning_plan_id = @PlanId
          AND learning_unit_id = @LearningUnitId;
        """;

    private const string _insertSlpUnitSql = """
        INSERT INTO academics.slp_learning_units (
            id,
            student_learning_plan_id,
            learning_unit_id,
            sequence_order,
            is_required,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @StudentLearningPlanId,
            @LearningUnitId,
            @SequenceOrder,
            @IsRequired,
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private sealed record PlanRow(Guid Id, string Status);
    private sealed record UnitRow(Guid Id);

    /// <summary>
    /// Executes adding a constituent learning unit to a draft student learning plan.
    /// </summary>
    /// <param name="planId">The unique identifier of the student learning plan.</param>
    /// <param name="request">The unit addition request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Added constituent unit entry response or domain error.</returns>
    public async Task<Result<AddSlpLearningUnitResponse>> HandleAsync(
        Guid planId,
        AddSlpLearningUnitRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (planId == Guid.Empty)
        {
            return Result.Failure<AddSlpLearningUnitResponse>(
                DomainError.Validation("Academics.InvalidPlanId", "Student learning plan ID cannot be empty."));
        }

        if (request.LearningUnitId == Guid.Empty)
        {
            return Result.Failure<AddSlpLearningUnitResponse>(
                DomainError.Validation("Academics.InvalidLearningUnitId", "Learning unit ID cannot be empty."));
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
            return Result.Failure<AddSlpLearningUnitResponse>(
                DomainError.NotFound(
                    "Academics.LearningPlanNotFound",
                    $"Student learning plan with ID '{planId}' was not found."));
        }

        if (!string.Equals(plan.Status, "draft", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<AddSlpLearningUnitResponse>(
                DomainError.Conflict(
                    "Academics.ImmutableLearningPlan",
                    $"Student learning plan with ID '{planId}' cannot be modified because its status is '{plan.Status}'. Only draft plans can be modified."));
        }

        // 2. Validate learning unit exists in academics.learning_units
        var unit = await connection.QuerySingleOrDefaultAsync<UnitRow>(
            new CommandDefinition(
                _checkLearningUnitSql,
                new { LearningUnitId = request.LearningUnitId },
                cancellationToken: cancellationToken));

        if (unit is null)
        {
            return Result.Failure<AddSlpLearningUnitResponse>(
                DomainError.NotFound(
                    "Academics.LearningUnitNotFound",
                    $"Learning unit with ID '{request.LearningUnitId}' was not found."));
        }

        // 3. Gracefully guard against duplicate unit additions (UQ_academics_slp_units_plan_unit)
        var duplicateCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                _checkDuplicateUnitSql,
                new { PlanId = planId, LearningUnitId = request.LearningUnitId },
                cancellationToken: cancellationToken));

        if (duplicateCount > 0)
        {
            return Result.Failure<AddSlpLearningUnitResponse>(
                DomainError.Conflict(
                    "Academics.DuplicateSlpLearningUnit",
                    $"Learning unit with ID '{request.LearningUnitId}' is already linked to student learning plan '{planId}'."));
        }

        // 4. Insert row into academics.slp_learning_units
        var unitEntryId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var createdBy = currentUser.Username ?? currentUser.Email ?? "system";

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                _insertSlpUnitSql,
                new
                {
                    Id = unitEntryId,
                    StudentLearningPlanId = planId,
                    LearningUnitId = request.LearningUnitId,
                    request.SequenceOrder,
                    request.IsRequired,
                    request.Notes,
                    CreatedAt = now,
                    CreatedBy = createdBy
                },
                cancellationToken: cancellationToken));
        }
        catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
        {
            return Result.Failure<AddSlpLearningUnitResponse>(
                DomainError.Conflict(
                    "Academics.DuplicateSlpLearningUnit",
                    $"Learning unit with ID '{request.LearningUnitId}' is already linked to student learning plan '{planId}'."));
        }

        return Result.Success(new AddSlpLearningUnitResponse(
            unitEntryId,
            planId,
            request.LearningUnitId,
            request.SequenceOrder,
            request.IsRequired,
            request.Notes,
            now,
            createdBy));
    }
}
