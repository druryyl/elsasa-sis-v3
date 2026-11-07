namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.SyncSlpParticipations;

using Dapper;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Feature handler executing idempotent batch synchronization of an active student learning plan's constituent units
/// into academics.academic_participations with source = 'learning_plan' (UC-ACD-10, P4-S10, TD-04).
/// </summary>
public sealed partial class SyncSlpParticipationsHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher,
    ILogger<SyncSlpParticipationsHandler> logger)
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Student learning plan {PlanId} for student {StudentId} participations synchronized by {User}: Synced={SyncedCount}, Updated={UpdatedCount}, TotalRequiredUnits={TotalUnits}")]
    private static partial void LogParticipationsSynced(
        ILogger logger,
        Guid planId,
        Guid studentId,
        string user,
        int syncedCount,
        int updatedCount,
        int totalUnits);

    static SyncSlpParticipationsHandler()
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

    private const string _selectTotalRequiredUnitsSql = """
        SELECT COUNT(1)
        FROM academics.slp_learning_units
        WHERE student_learning_plan_id = @PlanId
          AND is_required = 1;
        """;

    private const string _insertNewParticipationsSql = """
        INSERT INTO academics.academic_participations (
            id,
            student_id,
            learning_unit_id,
            academic_period_id,
            academic_period_label,
            participation_type,
            attempt_number,
            source,
            status,
            start_date,
            student_learning_plan_id,
            created_at,
            created_by
        )
        SELECT
            NEWID(),
            slp.student_id,
            slu.learning_unit_id,
            slp.academic_period_id,
            slp.academic_period_label,
            'standard',
            1,
            'learning_plan',
            'in_progress',
            @Today,
            slp.id,
            @Now,
            @CurrentUser
        FROM academics.slp_learning_units slu
        INNER JOIN academics.student_learning_plans slp ON slp.id = slu.student_learning_plan_id
        WHERE slp.id = @PlanId
          AND slp.status = 'active'
          AND slu.is_required = 1
          AND NOT EXISTS (
              SELECT 1
              FROM academics.academic_participations ap
              WHERE ap.student_id = slp.student_id
                AND ap.learning_unit_id = slu.learning_unit_id
                AND ap.academic_period_id = slp.academic_period_id
          );
        """;

    private const string _updateExistingParticipationsSql = """
        UPDATE ap
        SET
            ap.student_learning_plan_id = @PlanId,
            ap.source = 'learning_plan',
            ap.updated_at = @Now,
            ap.updated_by = @CurrentUser
        FROM academics.academic_participations ap
        INNER JOIN academics.slp_learning_units slu ON slu.learning_unit_id = ap.learning_unit_id
        INNER JOIN academics.student_learning_plans slp ON slp.id = slu.student_learning_plan_id
        WHERE slp.id = @PlanId
          AND slp.status = 'active'
          AND slu.is_required = 1
          AND ap.student_id = slp.student_id
          AND ap.academic_period_id = slp.academic_period_id
          AND ap.student_learning_plan_id IS NULL;
        """;

    private sealed record PlanRow(
        Guid Id,
        Guid StudentId,
        Guid AcademicPeriodId,
        string AcademicPeriodLabel,
        string Status,
        int Version);

    /// <summary>
    /// Executes validation and single-transaction idempotent synchronization of constituent learning units into academic participations.
    /// </summary>
    /// <param name="planId">The unique identifier of the target student learning plan.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Synchronization metrics or domain error.</returns>
    public async Task<Result<SyncSlpParticipationsResponse>> HandleAsync(
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        if (planId == Guid.Empty)
        {
            return Result.Failure<SyncSlpParticipationsResponse>(
                DomainError.Validation("Academics.InvalidPlanId", "Student learning plan ID cannot be empty."));
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
            return Result.Failure<SyncSlpParticipationsResponse>(
                DomainError.NotFound(
                    "Academics.LearningPlanNotFound",
                    $"Student learning plan with ID '{planId}' was not found."));
        }

        // 2. Validate plan has status = 'active'
        if (!string.Equals(plan.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<SyncSlpParticipationsResponse>(
                DomainError.Conflict(
                    "Academics.PlanNotActive",
                    $"Student learning plan cannot be synchronized because its status is '{plan.Status}'. Only active plans can be synchronized."));
        }

        // 3. Execute single database transaction
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var now = DateTimeOffset.UtcNow;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var user = currentUser.Username ?? currentUser.Email ?? "system";

            // Step 1: Identify total required constituent units on active plan
            var totalUnits = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    _selectTotalRequiredUnitsSql,
                    new { PlanId = plan.Id },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            // Step 2: Insert new rows into academic_participations for any required unit not already present
            var syncedCount = await connection.ExecuteAsync(
                new CommandDefinition(
                    _insertNewParticipationsSql,
                    new
                    {
                        PlanId = plan.Id,
                        Today = today,
                        Now = now,
                        CurrentUser = user
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            // Step 3: For any existing participation for the same unit/student/period where student_learning_plan_id IS NULL, update
            var updatedCount = await connection.ExecuteAsync(
                new CommandDefinition(
                    _updateExistingParticipationsSql,
                    new
                    {
                        PlanId = plan.Id,
                        Now = now,
                        CurrentUser = user
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            // Step 4: Commit transaction
            await transaction.CommitAsync(cancellationToken);

            // Step 5: Publish post-commit domain event
            var totalReconciledOrSynced = syncedCount + updatedCount;
            await publisher.Publish(
                new StudentLearningPlanParticipationsSyncedEvent(
                    plan.Id,
                    plan.StudentId,
                    totalReconciledOrSynced,
                    user,
                    now),
                cancellationToken);

            LogParticipationsSynced(
                logger,
                plan.Id,
                plan.StudentId,
                user,
                syncedCount,
                updatedCount,
                totalUnits);

            return Result.Success(new SyncSlpParticipationsResponse(
                plan.Id,
                plan.StudentId,
                syncedCount,
                updatedCount,
                totalUnits));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
