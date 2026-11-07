namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.AddAcademicPlanAdjustment;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for adding an academic plan adjustment to a draft student learning plan (UC-ACD-09, P3-S07, TD-03).
/// Enforces:
/// - Plan must exist and have status = 'draft'. Modifying active/revised/archived plans is rejected.
/// - Initial approved_by = '' and approved_date = '1900-01-01' (stamped during plan approval).
/// </summary>
public sealed class AddAcademicPlanAdjustmentHandler(
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

    private const string _insertAdjustmentSql = """
        INSERT INTO academics.academic_plan_adjustments (
            id,
            student_learning_plan_id,
            adjustment_type,
            description,
            rationale,
            effective_date,
            end_date,
            approved_by,
            approved_date,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @StudentLearningPlanId,
            @AdjustmentType,
            @Description,
            @Rationale,
            @EffectiveDate,
            @EndDate,
            @ApprovedBy,
            @ApprovedDate,
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private sealed record PlanRow(Guid Id, string Status, int Version);

    /// <summary>
    /// Executes validation and creation of an academic plan adjustment.
    /// </summary>
    /// <param name="planId">The parent student learning plan identifier.</param>
    /// <param name="request">The adjustment payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created adjustment details or domain error.</returns>
    public async Task<Result<AddAcademicPlanAdjustmentResponse>> HandleAsync(
        Guid planId,
        AddAcademicPlanAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (planId == Guid.Empty)
        {
            return Result.Failure<AddAcademicPlanAdjustmentResponse>(
                DomainError.Validation("Academics.InvalidPlanId", "Student learning plan ID cannot be empty."));
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
            return Result.Failure<AddAcademicPlanAdjustmentResponse>(
                DomainError.NotFound(
                    "Academics.LearningPlanNotFound",
                    $"Student learning plan with ID '{planId}' was not found."));
        }

        if (!string.Equals(plan.Status, "draft", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<AddAcademicPlanAdjustmentResponse>(
                DomainError.Conflict(
                    "Academics.ImmutableLearningPlan",
                    $"Student learning plan with ID '{planId}' cannot be modified because its status is '{plan.Status}'. Only draft plans can be modified."));
        }

        // 2. Insert new adjustment row
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var createdBy = currentUser.Username ?? currentUser.Email ?? "system";
        var adjustmentType = request.AdjustmentType.Trim().ToLowerInvariant();
        var defaultApprovedDate = new DateOnly(1900, 1, 1);

        await connection.ExecuteAsync(new CommandDefinition(
            _insertAdjustmentSql,
            new
            {
                Id = id,
                StudentLearningPlanId = planId,
                AdjustmentType = adjustmentType,
                Description = request.Description.Trim(),
                Rationale = request.Rationale.Trim(),
                request.EffectiveDate,
                request.EndDate,
                ApprovedBy = string.Empty,
                ApprovedDate = defaultApprovedDate,
                request.Notes,
                CreatedAt = now,
                CreatedBy = createdBy
            },
            cancellationToken: cancellationToken));

        return Result.Success(new AddAcademicPlanAdjustmentResponse(
            id,
            planId,
            adjustmentType,
            request.Description.Trim(),
            request.Rationale.Trim(),
            request.EffectiveDate,
            request.EndDate,
            string.Empty,
            defaultApprovedDate,
            request.Notes,
            now,
            createdBy));
    }
}
