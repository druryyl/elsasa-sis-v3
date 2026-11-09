namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.ApproveStudentLearningPlan;

using Dapper;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

/// <summary>
/// Feature handler executing formal Academic Authority approval of a student learning plan (UC-ACD-09, P3-S08, TD-02).
/// Executes atomic status hand-off, immutable audit logging in academics.slp_approvals, and stamps all adjustments in a single transaction.
/// </summary>
public sealed partial class ApproveStudentLearningPlanHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher,
    ILogger<ApproveStudentLearningPlanHandler> logger)
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Student learning plan {PlanId} (v{Version}) formally approved as {ApprovalType} by {Authority}")]
    private static partial void LogPlanApproved(
        ILogger logger,
        Guid planId,
        int version,
        string approvalType,
        string authority);

    static ApproveStudentLearningPlanHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectPlanSql = """
        SELECT
            id AS Id,
            student_id AS StudentId,
            academic_period_id AS AcademicPeriodId,
            status AS Status,
            version AS Version
        FROM academics.student_learning_plans
        WHERE id = @Id;
        """;

    private const string _selectActivePlanSql = """
        SELECT id
        FROM academics.student_learning_plans
        WHERE student_id = @StudentId
          AND academic_period_id = @AcademicPeriodId
          AND status = 'active';
        """;

    private const string _transitionToRevisedSql = """
        UPDATE academics.student_learning_plans
        SET
            status = 'revised',
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private const string _activatePlanSql = """
        UPDATE academics.student_learning_plans
        SET
            status = 'active',
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private const string _insertApprovalSql = """
        INSERT INTO academics.slp_approvals (
            id,
            student_learning_plan_id,
            plan_version,
            approved_by,
            approved_date,
            approval_type,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @ApprovalId,
            @StudentLearningPlanId,
            @PlanVersion,
            @ApprovedBy,
            @ApprovedDate,
            @ApprovalType,
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private const string _stampAdjustmentsSql = """
        UPDATE academics.academic_plan_adjustments
        SET
            approved_by = @ApprovedBy,
            approved_date = @ApprovedDate,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE student_learning_plan_id = @StudentLearningPlanId;
        """;

    private sealed record PlanRow(
        Guid Id,
        Guid StudentId,
        Guid AcademicPeriodId,
        string Status,
        int Version);

    /// <summary>
    /// Executes validation and single-transaction approval of a student learning plan.
    /// </summary>
    /// <param name="planId">The unique identifier of the target student learning plan.</param>
    /// <param name="request">The approval payload containing decision category and optional notes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Approval confirmation details or domain error.</returns>
    public async Task<Result<ApproveStudentLearningPlanResponse>> HandleAsync(
        Guid planId,
        ApproveStudentLearningPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (planId == Guid.Empty)
        {
            return Result.Failure<ApproveStudentLearningPlanResponse>(
                DomainError.Validation("Academics.InvalidPlanId", "Student learning plan ID cannot be empty."));
        }

        var approvalType = request.ApprovalType?.Trim().ToLowerInvariant();
        if (approvalType is not ("initial_approval" or "revision_approval"))
        {
            return Result.Failure<ApproveStudentLearningPlanResponse>(
                DomainError.Validation(
                    "Academics.InvalidApprovalType",
                    "Approval type must be either 'initial_approval' or 'revision_approval'."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Fetch target plan row
        var plan = await connection.QuerySingleOrDefaultAsync<PlanRow>(
            new CommandDefinition(
                _selectPlanSql,
                new { Id = planId },
                cancellationToken: cancellationToken));

        if (plan is null)
        {
            return Result.Failure<ApproveStudentLearningPlanResponse>(
                DomainError.NotFound(
                    "Academics.LearningPlanNotFound",
                    $"Student learning plan with ID '{planId}' was not found."));
        }

        if (!string.Equals(plan.Status, "draft", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ApproveStudentLearningPlanResponse>(
                DomainError.Conflict(
                    "Academics.PlanNotDraft",
                    $"Student learning plan cannot be approved because its status is '{plan.Status}'. Only draft plans can be approved."));
        }

        // 2. Execute single database transaction
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var now = DateTimeOffset.UtcNow;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var authority = currentUser.Username ?? currentUser.Email ?? "authority";

            // Step 1: Atomic hand-off if revision approval
            if (string.Equals(approvalType, "revision_approval", StringComparison.OrdinalIgnoreCase))
            {
                var priorActivePlanId = await connection.QueryFirstOrDefaultAsync<Guid?>(
                    new CommandDefinition(
                        _selectActivePlanSql,
                        new { plan.StudentId, plan.AcademicPeriodId },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                if (priorActivePlanId.HasValue && priorActivePlanId.Value != Guid.Empty)
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        _transitionToRevisedSql,
                        new
                        {
                            Id = priorActivePlanId.Value,
                            UpdatedAt = now,
                            UpdatedBy = authority
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                }
            }
            // Step 2: Enforce single-active invariant if initial approval
            else if (string.Equals(approvalType, "initial_approval", StringComparison.OrdinalIgnoreCase))
            {
                var existingActivePlanId = await connection.QueryFirstOrDefaultAsync<Guid?>(
                    new CommandDefinition(
                        _selectActivePlanSql,
                        new { plan.StudentId, plan.AcademicPeriodId },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                if (existingActivePlanId.HasValue && existingActivePlanId.Value != Guid.Empty)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Failure<ApproveStudentLearningPlanResponse>(
                        DomainError.Conflict(
                            "Academics.ActiveLearningPlanAlreadyExists",
                            $"An active student learning plan already exists for student '{plan.StudentId}' in academic period '{plan.AcademicPeriodId}'. An initial approval cannot be performed when an active plan exists."));
                }
            }

            // Step 3: Transition target draft plan from 'draft' to 'active'
            await connection.ExecuteAsync(new CommandDefinition(
                _activatePlanSql,
                new
                {
                    Id = plan.Id,
                    UpdatedAt = now,
                    UpdatedBy = authority
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // Step 4: Insert immutable audit record into academics.slp_approvals
            var approvalId = Guid.NewGuid();
            await connection.ExecuteAsync(new CommandDefinition(
                _insertApprovalSql,
                new
                {
                    ApprovalId = approvalId,
                    StudentLearningPlanId = plan.Id,
                    PlanVersion = plan.Version,
                    ApprovedBy = authority,
                    ApprovedDate = today,
                    ApprovalType = approvalType,
                    request.Notes,
                    CreatedAt = now,
                    CreatedBy = authority
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // Step 5: Update all adjustments linked to the new active plan
            await connection.ExecuteAsync(new CommandDefinition(
                _stampAdjustmentsSql,
                new
                {
                    StudentLearningPlanId = plan.Id,
                    ApprovedBy = authority,
                    ApprovedDate = today,
                    UpdatedAt = now,
                    UpdatedBy = authority
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // Step 6: Commit transaction
            await transaction.CommitAsync(cancellationToken);

            // Step 7: Publish post-commit domain event
            await publisher.Publish(
                new StudentLearningPlanApprovedEvent(
                    plan.Id,
                    plan.StudentId,
                    plan.AcademicPeriodId,
                    plan.Version,
                    approvalType,
                    authority,
                    now),
                cancellationToken);

            LogPlanApproved(
                logger,
                plan.Id,
                plan.Version,
                approvalType,
                authority);

            return Result.Success(new ApproveStudentLearningPlanResponse(
                plan.Id,
                approvalId,
                plan.Version,
                "active",
                approvalType,
                authority,
                today,
                request.Notes));
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<ApproveStudentLearningPlanResponse>(
                DomainError.Conflict(
                    "Academics.ActiveLearningPlanConflict",
                    "An active student learning plan already exists for this student and academic period."));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
