namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.UpdateTeachingAssignmentStatus;

using System.Text.Json;
using Dapper;
using Elsasa.Sis.Contracts.Teaching.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler updating teaching assignment status across discrete lifecycles with atomic workload adjustments and event publishing (TD-01, TD-04, P3-S06).
/// </summary>
public sealed class UpdateTeachingAssignmentStatusHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static UpdateTeachingAssignmentStatusHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAssignmentForUpdateSql = """
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

    private const string _updateAssignmentStatusSql = """
        UPDATE teaching.teaching_assignments
        SET status = @NewStatus,
            notes = CASE WHEN @Reason IS NOT NULL AND @Reason <> '' THEN @Reason ELSE notes END,
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

    private const string _updateCommitmentFromProposedSql = """
        UPDATE teaching.workload_commitments
        SET planned_workload_hours = @PlannedWorkloadHours,
            as_of_timestamp = SYSUTCDATETIME(),
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE id = @CommitmentId;
        """;

    private const string _updateCommitmentFromApprovedSql = """
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

    private const string _selectDemandStaffingSql = """
        SELECT status, required_teachers_count
        FROM teaching.teaching_demands
        WHERE id = @DemandId;
        """;

    private const string _countApprovedAssignmentsSql = """
        SELECT COUNT(1)
        FROM teaching.teaching_assignments
        WHERE teaching_demand_id = @DemandId AND status = 'approved';
        """;

    private const string _updateDemandStaffingSql = """
        UPDATE teaching.teaching_demands
        SET status = 'accepted_for_staffing',
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE id = @DemandId;
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
            @EventType,
            'teaching_assignment',
            @EntityId,
            @EffectiveDate,
            SYSUTCDATETIME(),
            'Teaching Coordinator',
            @ActorId,
            @EventSummary,
            @ChangePayload,
            @Notes,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    public async Task<Result<UpdateTeachingAssignmentStatusResponse>> HandleAsync(
        Guid id,
        UpdateTeachingAssignmentStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var targetStatus = request.Status.Trim().ToLowerInvariant();
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        var auditUser = currentUser.Username ?? "system";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        if (connection is SqlConnection sqlConn && sqlConn.State != System.Data.ConnectionState.Open)
        {
            await sqlConn.OpenAsync(cancellationToken);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var assignment = await connection.QuerySingleOrDefaultAsync<AssignmentRow>(new CommandDefinition(
                _selectAssignmentForUpdateSql,
                new { Id = id },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (assignment is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<UpdateTeachingAssignmentStatusResponse>(DomainError.NotFound(
                    "Teaching.AssignmentNotFound",
                    $"Teaching assignment with ID '{id}' was not found."));
            }

            var currentStatus = assignment.Status.ToLowerInvariant();

            // Validate transition validity
            var isValidTransition = currentStatus switch
            {
                "proposed" => targetStatus is "withdrawn" or "returned_for_revision" or "cancelled",
                "approved" => targetStatus is "suspended" or "cancelled",
                _ => false
            };

            if (!isValidTransition)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<UpdateTeachingAssignmentStatusResponse>(DomainError.Conflict(
                    "Teaching.InvalidAssignmentTransition",
                    $"Cannot transition teaching assignment '{assignment.AssignmentNumber}' from '{currentStatus}' to '{targetStatus}'."));
            }

            // 1. Update assignment status
            await connection.ExecuteAsync(new CommandDefinition(
                _updateAssignmentStatusSql,
                new
                {
                    Id = id,
                    NewStatus = targetStatus,
                    Reason = reason,
                    UpdatedBy = auditUser
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            var val = assignment.CountsTowardWorkload ? assignment.WorkloadCommitmentValue : 0.00m;

            // 2. Adjust workload commitments atomically
            var commitment = await connection.QuerySingleOrDefaultAsync<CommitmentDetailRow>(new CommandDefinition(
                _selectCommitmentForUpdateSql,
                new
                {
                    TeacherProfileId = assignment.TeacherProfileId,
                    AcademicPeriodId = assignment.AcademicPeriodId
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (commitment is not null && val > 0)
            {
                if (currentStatus == "proposed")
                {
                    var newPlanned = Math.Max(0.00m, commitment.PlannedWorkloadHours - val);
                    await connection.ExecuteAsync(new CommandDefinition(
                        _updateCommitmentFromProposedSql,
                        new
                        {
                            CommitmentId = commitment.Id,
                            PlannedWorkloadHours = newPlanned,
                            UpdatedBy = auditUser
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                }
                else if (currentStatus == "approved" && targetStatus is "suspended" or "cancelled")
                {
                    var newCommitted = Math.Max(0.00m, commitment.CommittedWorkloadHours - val);
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
                        _updateCommitmentFromApprovedSql,
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
            }

            // 3. Re-evaluate teaching demand status if assignment was approved and is now cancelled
            if (currentStatus == "approved" && targetStatus == "cancelled")
            {
                var demandInfo = await connection.QuerySingleOrDefaultAsync<DemandInfoRow>(new CommandDefinition(
                    _selectDemandStaffingSql,
                    new { DemandId = assignment.TeachingDemandId },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

                if (demandInfo is not null && string.Equals(demandInfo.Status, "staffed", StringComparison.OrdinalIgnoreCase))
                {
                    var remainingApproved = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                        _countApprovedAssignmentsSql,
                        new { DemandId = assignment.TeachingDemandId },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                    if (remainingApproved < demandInfo.RequiredTeachersCount)
                    {
                        await connection.ExecuteAsync(new CommandDefinition(
                            _updateDemandStaffingSql,
                            new
                            {
                                DemandId = assignment.TeachingDemandId,
                                UpdatedBy = auditUser
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken));
                    }
                }
            }

            // 4. Append audit log in teaching.teaching_history_entries
            var eventType = targetStatus switch
            {
                "cancelled" => "ASSIGNMENT_CANCELLED",
                "suspended" => "ASSIGNMENT_SUSPENDED",
                "withdrawn" => "ASSIGNMENT_WITHDRAWN",
                "returned_for_revision" => "ASSIGNMENT_REVISION_REQUESTED",
                _ => $"ASSIGNMENT_STATUS_{targetStatus.ToUpperInvariant()}"
            };

            var changePayload = JsonSerializer.Serialize(new
            {
                assignment.AssignmentNumber,
                PreviousStatus = currentStatus,
                NewStatus = targetStatus,
                Reason = reason,
                UpdatedBy = auditUser
            });

            await connection.ExecuteAsync(new CommandDefinition(
                _insertTeachingHistorySql,
                new
                {
                    Id = Guid.NewGuid(),
                    TeacherProfileId = assignment.TeacherProfileId,
                    EventType = eventType,
                    EntityId = assignment.Id,
                    EffectiveDate = today,
                    ActorId = auditUser,
                    EventSummary = $"Status penugasan {assignment.AssignmentNumber} diubah dari '{currentStatus}' ke '{targetStatus}'. Rincian: {reason ?? "Tidak ada catatan."}",
                    ChangePayload = changePayload,
                    Notes = reason,
                    CreatedBy = auditUser
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);

            // 5. Publish domain event if cancelled
            if (targetStatus == "cancelled")
            {
                var releasedHours = currentStatus == "approved" && assignment.CountsTowardWorkload
                    ? assignment.WorkloadCommitmentValue
                    : 0.00m;

                var cancelledEvent = new TeachingAssignmentCancelledEvent(
                    assignment.Id,
                    assignment.AssignmentNumber,
                    assignment.TeacherProfileId,
                    assignment.AcademicPeriodId,
                    releasedHours,
                    auditUser,
                    reason ?? "Cancelled by administrator");

                await publisher.Publish(cancelledEvent, cancellationToken);
            }

            return Result.Success(new UpdateTeachingAssignmentStatusResponse(
                assignment.Id,
                assignment.AssignmentNumber,
                targetStatus,
                DateTimeOffset.UtcNow));
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

    private sealed record DemandInfoRow(string Status, int RequiredTeachersCount);
}
