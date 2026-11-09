namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.ReviseStudentLearningPlan;

using Dapper;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Feature handler executing the clone-on-revise lifecycle transition (UC-ACD-09, P3-S09, TD-02).
/// Clones an active plan into a new draft plan with version = version + 1, deep-cloning all constituent
/// units and adjustments while leaving the active plan operative.
/// </summary>
public sealed partial class ReviseStudentLearningPlanHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher,
    ILogger<ReviseStudentLearningPlanHandler> logger)
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "SLP {PriorPlanId} revised to {NewPlanId} (v{NewVersion}) by {Username}")]
    private static partial void LogPlanRevised(
        ILogger logger,
        Guid priorPlanId,
        Guid newPlanId,
        int newVersion,
        string username);

    static ReviseStudentLearningPlanHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectPlanSql = """
        SELECT
            id AS Id,
            student_id AS StudentId,
            academic_period_id AS AcademicPeriodId,
            academic_period_label AS AcademicPeriodLabel,
            status AS Status,
            version AS Version
        FROM academics.student_learning_plans
        WHERE id = @Id;
        """;

    private const string _selectPendingDraftPlanSql = """
        SELECT id
        FROM academics.student_learning_plans
        WHERE student_id = @StudentId
          AND academic_period_id = @AcademicPeriodId
          AND status = 'draft';
        """;

    private const string _insertClonedPlanSql = """
        INSERT INTO academics.student_learning_plans (
            id,
            student_id,
            academic_period_id,
            academic_period_label,
            status,
            version,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @NewPlanId,
            @StudentId,
            @AcademicPeriodId,
            @AcademicPeriodLabel,
            'draft',
            @NewVersion,
            @Notes,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    private const string _cloneLearningUnitsSql = """
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
        SELECT
            NEWID(),
            @NewPlanId,
            learning_unit_id,
            sequence_order,
            is_required,
            notes,
            SYSUTCDATETIME(),
            @CreatedBy
        FROM academics.slp_learning_units
        WHERE student_learning_plan_id = @PriorPlanId;
        """;

    private const string _cloneAdjustmentsSql = """
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
        SELECT
            NEWID(),
            @NewPlanId,
            adjustment_type,
            description,
            rationale,
            effective_date,
            end_date,
            '',
            '1900-01-01',
            notes,
            SYSUTCDATETIME(),
            @CreatedBy
        FROM academics.academic_plan_adjustments
        WHERE student_learning_plan_id = @PriorPlanId;
        """;

    private sealed record TargetPlanRow(
        Guid Id,
        Guid StudentId,
        Guid AcademicPeriodId,
        string AcademicPeriodLabel,
        string Status,
        int Version);

    /// <summary>
    /// Executes the clone-on-revise engine in a single transaction.
    /// </summary>
    /// <param name="planId">The unique identifier of the active plan being revised.</param>
    /// <param name="request">The revision request containing notes/rationale.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Details of the newly created draft plan revision, or a domain error.</returns>
    public async Task<Result<ReviseStudentLearningPlanResponse>> HandleAsync(
        Guid planId,
        ReviseStudentLearningPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (planId == Guid.Empty)
        {
            return Result.Failure<ReviseStudentLearningPlanResponse>(
                DomainError.Validation("Academics.InvalidPlanId", "Student learning plan ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Fetch target plan row
        var plan = await connection.QuerySingleOrDefaultAsync<TargetPlanRow>(
            new CommandDefinition(
                _selectPlanSql,
                new { Id = planId },
                cancellationToken: cancellationToken));

        if (plan is null)
        {
            return Result.Failure<ReviseStudentLearningPlanResponse>(
                DomainError.NotFound(
                    "Academics.LearningPlanNotFound",
                    $"Student learning plan with ID '{planId}' was not found."));
        }

        if (!string.Equals(plan.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ReviseStudentLearningPlanResponse>(
                DomainError.Conflict(
                    "Academics.PlanNotActive",
                    $"Student learning plan cannot be revised because its status is '{plan.Status}'. Only active plans can be revised."));
        }

        // 2. Execute single database transaction
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // Validate there is no currently pending draft revision for this student and period
            var pendingDraftPlanId = await connection.QueryFirstOrDefaultAsync<Guid?>(
                new CommandDefinition(
                    _selectPendingDraftPlanSql,
                    new { plan.StudentId, plan.AcademicPeriodId },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            if (pendingDraftPlanId.HasValue && pendingDraftPlanId.Value != Guid.Empty)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<ReviseStudentLearningPlanResponse>(
                    DomainError.Conflict(
                        "Academics.PendingDraftPlanAlreadyExists",
                        $"A pending draft learning plan already exists for student '{plan.StudentId}' in academic period '{plan.AcademicPeriodId}'. Only one draft revision may be pending at a time."));
            }

            var newPlanId = Guid.NewGuid();
            var newVersion = plan.Version + 1;
            var author = currentUser.Username ?? currentUser.Email ?? "system";
            var now = DateTimeOffset.UtcNow;

            // Step 1: Insert cloned plan row with status = 'draft', version = prior_plan.version + 1
            await connection.ExecuteAsync(new CommandDefinition(
                _insertClonedPlanSql,
                new
                {
                    NewPlanId = newPlanId,
                    plan.StudentId,
                    plan.AcademicPeriodId,
                    plan.AcademicPeriodLabel,
                    NewVersion = newVersion,
                    request.Notes,
                    CreatedBy = author
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // Step 2: Deep-clone constituent units from academics.slp_learning_units
            await connection.ExecuteAsync(new CommandDefinition(
                _cloneLearningUnitsSql,
                new
                {
                    NewPlanId = newPlanId,
                    PriorPlanId = plan.Id,
                    CreatedBy = author
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // Step 3: Deep-clone adjustments from academics.academic_plan_adjustments with unapproved initial state
            await connection.ExecuteAsync(new CommandDefinition(
                _cloneAdjustmentsSql,
                new
                {
                    NewPlanId = newPlanId,
                    PriorPlanId = plan.Id,
                    CreatedBy = author
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // Step 4: Original active plan remains strictly 'active' and untouched.
            // Step 5: Commit transaction
            await transaction.CommitAsync(cancellationToken);

            // Step 6: Publish post-commit domain event
            await publisher.Publish(
                new StudentLearningPlanRevisedEvent(
                    plan.Id,
                    newPlanId,
                    plan.StudentId,
                    plan.AcademicPeriodId,
                    newVersion,
                    author,
                    now),
                cancellationToken);

            LogPlanRevised(logger, plan.Id, newPlanId, newVersion, author);

            return Result.Success(new ReviseStudentLearningPlanResponse(
                newPlanId,
                plan.Id,
                plan.StudentId,
                plan.AcademicPeriodId,
                plan.AcademicPeriodLabel,
                "draft",
                newVersion,
                request.Notes,
                now,
                author));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
