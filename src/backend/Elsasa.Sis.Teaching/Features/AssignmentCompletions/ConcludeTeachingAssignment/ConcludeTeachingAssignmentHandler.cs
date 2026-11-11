namespace Elsasa.Sis.Teaching.Features.AssignmentCompletions.ConcludeTeachingAssignment;

using System.Text.Json;
using Dapper;
using Elsasa.Sis.Contracts.Teaching.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler concluding a teaching assignment, persisting completion verification, releasing capacity, and logging history (TD-01, TD-04, P4-S13).
/// </summary>
public sealed class ConcludeTeachingAssignmentHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static ConcludeTeachingAssignmentHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAssignmentForConclusionSql = """
        SELECT
            ta.id AS Id,
            ta.assignment_number AS AssignmentNumber,
            ta.teaching_demand_id AS TeachingDemandId,
            ta.teacher_profile_id AS TeacherProfileId,
            ta.academic_period_id AS AcademicPeriodId,
            ta.assignment_scope AS AssignmentScope,
            ta.scope_label AS ScopeLabel,
            ta.workload_commitment_value AS WorkloadCommitmentValue,
            ta.status AS Status,
            ar.counts_toward_workload AS CountsTowardWorkload
        FROM teaching.teaching_assignments ta WITH (UPDLOCK, ROWLOCK)
        INNER JOIN teaching.assignment_roles ar ON ar.id = ta.assignment_role_id
        WHERE ta.id = @Id;
        """;

    private const string _checkCompletionExistsSql = """
        SELECT 1
        FROM teaching.assignment_completions WITH (UPDLOCK, ROWLOCK)
        WHERE teaching_assignment_id = @AssignmentId;
        """;

    private const string _insertAssignmentCompletionSql = """
        INSERT INTO teaching.assignment_completions (
            id,
            teaching_assignment_id,
            completion_date,
            academic_confirmation_received,
            academic_confirmation_source,
            handover_confirmed,
            unresolved_items_status,
            unresolved_items_description,
            capacity_released_date,
            completed_by,
            status,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @TeachingAssignmentId,
            @CompletionDate,
            @AcademicConfirmationReceived,
            @AcademicConfirmationSource,
            @HandoverConfirmed,
            @UnresolvedItemsStatus,
            @UnresolvedItemsDescription,
            @CapacityReleasedDate,
            @CompletedBy,
            @Status,
            @Notes,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    private const string _updateAssignmentStatusSql = """
        UPDATE teaching.teaching_assignments
        SET status = 'completed',
            notes = CASE WHEN @Notes IS NOT NULL AND @Notes <> '' THEN @Notes ELSE notes END,
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private const string _selectCommitmentForUpdateSql = """
        SELECT
            wc.id AS Id,
            wc.planned_workload_hours AS PlannedWorkloadHours,
            wc.committed_workload_hours AS CommittedWorkloadHours,
            wc.max_capacity_limit_hours AS MaxCapacityLimitHours,
            wc.active_assignment_count AS ActiveAssignmentCount,
            wc.approved_exception_count AS ApprovedExceptionCount,
            wp.warning_threshold AS WarningThreshold,
            wp.prohibited_threshold AS ProhibitedThreshold
        FROM teaching.workload_commitments wc WITH (UPDLOCK, ROWLOCK)
        LEFT JOIN teaching.workload_policies wp ON wp.id = wc.workload_policy_id
        WHERE wc.teacher_profile_id = @TeacherProfileId AND wc.academic_period_id = @AcademicPeriodId;
        """;

    private const string _updateCommitmentFromCompletedSql = """
        UPDATE teaching.workload_commitments
        SET committed_workload_hours = @CommittedWorkloadHours,
            remaining_capacity_hours = @RemainingCapacityHours,
            active_assignment_count = @ActiveAssignmentCount,
            status = @Status,
            as_of_timestamp = SYSUTCDATETIME(),
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE id = @CommitmentId;
        """;

    private const string _insertTeachingHistorySql = """
        INSERT INTO teaching.teaching_history_entries (
            id,
            teacher_profile_id,
            event_type,
            entity_type,
            entity_id,
            effective_date,
            event_timestamp,
            actor_role,
            actor_id,
            event_summary,
            change_payload,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @TeacherProfileId,
            'AssignmentCompleted',
            'teaching_assignment',
            @EntityId,
            @EffectiveDate,
            SYSUTCDATETIME(),
            'Teaching Authority',
            @ActorId,
            @EventSummary,
            @ChangePayload,
            @Notes,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    public async Task<Result<ConcludeTeachingAssignmentResponse>> HandleAsync(
        Guid id,
        ConcludeTeachingAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var auditUser = !string.IsNullOrWhiteSpace(currentUser.Username) ? currentUser.Username : "Teaching Authority";
        var completionId = Guid.NewGuid();
        var historyEntryId = Guid.NewGuid();
        var completionDate = request.CompletionDate != default ? request.CompletionDate : DateOnly.FromDateTime(DateTime.UtcNow);
        var capacityReleasedDate = completionDate;

        // Normalize unresolved items status to match database CHECK constraint ('none', 'resolved', 'escalated')
        var rawUnresolved = (request.UnresolvedItemsStatus ?? "none").Trim().ToLowerInvariant();
        var dbUnresolvedStatus = rawUnresolved switch
        {
            "minor_pending" => "resolved",
            "escalated_defect" => "escalated",
            "resolved" => "resolved",
            "escalated" => "escalated",
            _ => "none"
        };

        // Determine assignment completion status ('completed' or 'completed_with_exceptions')
        var completionStatus = (!request.AcademicConfirmationReceived || !request.HandoverConfirmed || dbUnresolvedStatus == "escalated")
            ? "completed_with_exceptions"
            : "completed";

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        if (connection is SqlConnection sqlConn && sqlConn.State != System.Data.ConnectionState.Open)
        {
            await sqlConn.OpenAsync(cancellationToken);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Retrieve assignment with exclusive update lock
            var assignment = await connection.QuerySingleOrDefaultAsync<AssignmentRow>(new CommandDefinition(
                _selectAssignmentForConclusionSql,
                new { Id = id },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (assignment is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<ConcludeTeachingAssignmentResponse>(DomainError.NotFound(
                    "Teaching.AssignmentNotFound",
                    $"Teaching assignment with ID '{id}' was not found."));
            }

            if (string.Equals(assignment.Status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<ConcludeTeachingAssignmentResponse>(DomainError.Conflict(
                    "Teaching.AssignmentAlreadyCompleted",
                    $"Teaching assignment '{assignment.AssignmentNumber}' has already been concluded."));
            }

            if (!string.Equals(assignment.Status, "approved", StringComparison.OrdinalIgnoreCase))
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<ConcludeTeachingAssignmentResponse>(DomainError.Conflict(
                    "Teaching.InvalidAssignmentStatus",
                    $"Teaching assignment '{assignment.AssignmentNumber}' cannot be concluded because it is in '{assignment.Status}' status. Only 'approved' assignments can be concluded."));
            }

            // 2. Verify completion record does not already exist
            var alreadyCompleted = await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
                _checkCompletionExistsSql,
                new { AssignmentId = id },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (alreadyCompleted.HasValue)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<ConcludeTeachingAssignmentResponse>(DomainError.Conflict(
                    "Teaching.AssignmentAlreadyConcluded",
                    $"A completion record already exists for teaching assignment '{assignment.AssignmentNumber}'."));
            }

            // 3. Insert into teaching.assignment_completions
            await connection.ExecuteAsync(new CommandDefinition(
                _insertAssignmentCompletionSql,
                new
                {
                    Id = completionId,
                    TeachingAssignmentId = id,
                    CompletionDate = completionDate,
                    AcademicConfirmationReceived = request.AcademicConfirmationReceived,
                    AcademicConfirmationSource = string.IsNullOrWhiteSpace(request.AcademicConfirmationSource) ? null : request.AcademicConfirmationSource.Trim(),
                    HandoverConfirmed = request.HandoverConfirmed,
                    UnresolvedItemsStatus = dbUnresolvedStatus,
                    UnresolvedItemsDescription = string.IsNullOrWhiteSpace(request.UnresolvedItemsDescription) ? null : request.UnresolvedItemsDescription.Trim(),
                    CapacityReleasedDate = capacityReleasedDate,
                    CompletedBy = auditUser,
                    Status = completionStatus,
                    Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                    CreatedBy = auditUser
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // 4. Update teaching.teaching_assignments to 'completed'
            await connection.ExecuteAsync(new CommandDefinition(
                _updateAssignmentStatusSql,
                new
                {
                    Id = id,
                    Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                    UpdatedBy = auditUser
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // 5. Deduct committed workload hours from teaching.workload_commitments and decrement active_assignment_count
            var releasedHours = assignment.CountsTowardWorkload ? assignment.WorkloadCommitmentValue : 0.00m;

            var commitment = await connection.QuerySingleOrDefaultAsync<CommitmentDetailRow>(new CommandDefinition(
                _selectCommitmentForUpdateSql,
                new
                {
                    TeacherProfileId = assignment.TeacherProfileId,
                    AcademicPeriodId = assignment.AcademicPeriodId
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (commitment is not null)
            {
                var newCommitted = Math.Max(0.00m, commitment.CommittedWorkloadHours - releasedHours);
                var newRemaining = commitment.MaxCapacityLimitHours - newCommitted;
                var newActiveCount = Math.Max(0, commitment.ActiveAssignmentCount - 1);
                var warning = commitment.WarningThreshold ?? 20.00m;
                var prohibited = commitment.ProhibitedThreshold ?? 28.00m;

                string commitmentStatus;
                if (newCommitted > prohibited)
                {
                    commitmentStatus = commitment.ApprovedExceptionCount > 0 ? "overloaded_with_exception" : "breached";
                }
                else if (newCommitted >= warning)
                {
                    commitmentStatus = "approaching_limit";
                }
                else if (newCommitted == 0)
                {
                    commitmentStatus = "under_allocated";
                }
                else
                {
                    commitmentStatus = "normal";
                }

                await connection.ExecuteAsync(new CommandDefinition(
                    _updateCommitmentFromCompletedSql,
                    new
                    {
                        CommitmentId = commitment.Id,
                        CommittedWorkloadHours = newCommitted,
                        RemainingCapacityHours = newRemaining,
                        ActiveAssignmentCount = newActiveCount,
                        Status = commitmentStatus,
                        UpdatedBy = auditUser
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
            }

            // 6. Append entry into teaching.teaching_history_entries
            var changePayload = JsonSerializer.Serialize(new
            {
                completionId,
                assignmentId = assignment.Id,
                assignmentNumber = assignment.AssignmentNumber,
                releasedHours,
                completionDate,
                academicConfirmationReceived = request.AcademicConfirmationReceived,
                academicConfirmationSource = request.AcademicConfirmationSource,
                handoverConfirmed = request.HandoverConfirmed,
                unresolvedItemsStatus = dbUnresolvedStatus,
                status = completionStatus
            });

            var summary = $"Teaching assignment '{assignment.AssignmentNumber}' was concluded with status '{completionStatus}' and {releasedHours:0.00} workload hours released.";

            await connection.ExecuteAsync(new CommandDefinition(
                _insertTeachingHistorySql,
                new
                {
                    Id = historyEntryId,
                    TeacherProfileId = assignment.TeacherProfileId,
                    EntityId = assignment.Id,
                    EffectiveDate = completionDate,
                    ActorId = auditUser,
                    EventSummary = summary,
                    ChangePayload = changePayload,
                    Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                    CreatedBy = auditUser
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);

            // 7. Post-commit event publishing
            await publisher.Publish(
                new TeachingAssignmentCompletedEvent(
                    assignment.Id,
                    completionId,
                    assignment.AssignmentNumber,
                    assignment.TeacherProfileId,
                    assignment.AcademicPeriodId,
                    releasedHours,
                    completionDate,
                    auditUser),
                cancellationToken);

            var response = new ConcludeTeachingAssignmentResponse(
                completionId,
                assignment.Id,
                assignment.AssignmentNumber,
                completionDate,
                request.AcademicConfirmationReceived,
                request.AcademicConfirmationSource,
                request.HandoverConfirmed,
                dbUnresolvedStatus,
                request.UnresolvedItemsDescription,
                capacityReleasedDate,
                releasedHours,
                auditUser,
                completionStatus,
                request.Notes,
                DateTimeOffset.UtcNow);

            return Result.Success(response);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private sealed record AssignmentRow(
        Guid Id,
        string AssignmentNumber,
        Guid TeachingDemandId,
        Guid TeacherProfileId,
        Guid AcademicPeriodId,
        string AssignmentScope,
        string ScopeLabel,
        decimal WorkloadCommitmentValue,
        string Status,
        bool CountsTowardWorkload);

    private sealed record CommitmentDetailRow(
        Guid Id,
        decimal PlannedWorkloadHours,
        decimal CommittedWorkloadHours,
        decimal MaxCapacityLimitHours,
        int ActiveAssignmentCount,
        int ApprovedExceptionCount,
        decimal? WarningThreshold,
        decimal? ProhibitedThreshold);
}
