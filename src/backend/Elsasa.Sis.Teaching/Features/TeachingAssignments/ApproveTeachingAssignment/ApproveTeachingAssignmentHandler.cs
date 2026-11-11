namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.ApproveTeachingAssignment;

using System.Text.Json;
using Dapper;
using Elsasa.Sis.Contracts.Teaching.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler approving a teaching assignment with atomic workload commitment updates, demand staffing evaluation, and audit logging (TD-01, TD-04, P3-S06).
/// </summary>
public sealed class ApproveTeachingAssignmentHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static ApproveTeachingAssignmentHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAssignmentForApprovalSql = """
        SELECT
            ta.id AS Id,
            ta.assignment_number AS AssignmentNumber,
            ta.teaching_demand_id AS TeachingDemandId,
            ta.teacher_profile_id AS TeacherProfileId,
            ta.assignment_role_id AS AssignmentRoleId,
            ta.assignment_scope AS AssignmentScope,
            ta.scope_reference_id AS ScopeReferenceId,
            ta.scope_label AS ScopeLabel,
            ta.academic_period_id AS AcademicPeriodId,
            ta.workload_commitment_value AS WorkloadCommitmentValue,
            ta.status AS Status,
            ar.counts_toward_workload AS CountsTowardWorkload
        FROM teaching.teaching_assignments ta WITH (UPDLOCK, ROWLOCK)
        INNER JOIN teaching.assignment_roles ar ON ar.id = ta.assignment_role_id
        WHERE ta.id = @Id;
        """;

    private const string _updateAssignmentApprovalSql = """
        UPDATE teaching.teaching_assignments
        SET status = 'approved',
            approved_by = @ApprovedBy,
            approved_date = @ApprovedDate,
            notes = CASE WHEN @Notes IS NOT NULL AND @Notes <> '' THEN @Notes ELSE notes END,
            updated_at = SYSUTCDATETIME(),
            updated_by = @ApprovedBy
        WHERE id = @Id;
        """;

    private const string _selectCommitmentWithPolicySql = """
        SELECT
            wc.id AS Id,
            wc.workload_policy_id AS WorkloadPolicyId,
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

    private const string _updateCommitmentApprovalSql = """
        UPDATE teaching.workload_commitments
        SET planned_workload_hours = @PlannedWorkloadHours,
            committed_workload_hours = @CommittedWorkloadHours,
            remaining_capacity_hours = @RemainingCapacityHours,
            active_assignment_count = @ActiveAssignmentCount,
            status = @Status,
            as_of_timestamp = SYSUTCDATETIME(),
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE id = @CommitmentId;
        """;

    private const string _selectActivePolicySql = """
        SELECT TOP 1
            id AS Id,
            max_standard_capacity AS MaxStandardCapacity,
            warning_threshold AS WarningThreshold,
            prohibited_threshold AS ProhibitedThreshold
        FROM teaching.workload_policies
        WHERE status = 'active'
        ORDER BY effective_start_date DESC, created_at DESC;
        """;

    private const string _insertCommitmentApprovalSql = """
        INSERT INTO teaching.workload_commitments (
            id,
            teacher_profile_id,
            academic_period_id,
            workload_policy_id,
            planned_workload_hours,
            committed_workload_hours,
            max_capacity_limit_hours,
            remaining_capacity_hours,
            status,
            active_assignment_count,
            approved_exception_count,
            as_of_timestamp,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @TeacherProfileId,
            @AcademicPeriodId,
            @WorkloadPolicyId,
            0.00,
            @CommittedWorkloadHours,
            @MaxCapacityLimitHours,
            @RemainingCapacityHours,
            @Status,
            1,
            0,
            SYSUTCDATETIME(),
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    private const string _selectDemandHeadcountSql = """
        SELECT required_teachers_count
        FROM teaching.teaching_demands
        WHERE id = @DemandId;
        """;

    private const string _countApprovedAssignmentsSql = """
        SELECT COUNT(1)
        FROM teaching.teaching_assignments
        WHERE teaching_demand_id = @DemandId AND status = 'approved';
        """;

    private const string _updateDemandStaffedSql = """
        UPDATE teaching.teaching_demands
        SET status = 'staffed',
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
            'ASSIGNMENT_APPROVED',
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

    public async Task<Result<ApproveTeachingAssignmentResponse>> HandleAsync(
        Guid id,
        ApproveTeachingAssignmentRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        if (connection is SqlConnection sqlConn && sqlConn.State != System.Data.ConnectionState.Open)
        {
            await sqlConn.OpenAsync(cancellationToken);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var auditUser = !string.IsNullOrWhiteSpace(currentUser.Username) ? currentUser.Username : "Teaching Authority";
        var approvedDate = request?.ApprovedDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        try
        {
            // 1. Retrieve assignment with exclusive update lock
            var assignment = await connection.QuerySingleOrDefaultAsync<AssignmentRow>(new CommandDefinition(
                _selectAssignmentForApprovalSql,
                new { Id = id },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (assignment is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<ApproveTeachingAssignmentResponse>(DomainError.NotFound(
                    "Teaching.AssignmentNotFound",
                    $"Teaching assignment with ID '{id}' was not found."));
            }

            if (!string.Equals(assignment.Status, "proposed", StringComparison.OrdinalIgnoreCase))
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<ApproveTeachingAssignmentResponse>(DomainError.Conflict(
                    "Teaching.InvalidAssignmentStatus",
                    $"Teaching assignment '{assignment.AssignmentNumber}' cannot be approved because it is in '{assignment.Status}' status. Must be 'proposed'."));
            }

            // 2. Mark assignment as approved
            await connection.ExecuteAsync(new CommandDefinition(
                _updateAssignmentApprovalSql,
                new
                {
                    Id = id,
                    ApprovedBy = auditUser,
                    ApprovedDate = approvedDate,
                    Notes = string.IsNullOrWhiteSpace(request?.Notes) ? null : request.Notes.Trim()
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // 3. Update workload commitments atomically
            var val = assignment.CountsTowardWorkload ? assignment.WorkloadCommitmentValue : 0.00m;

            var commitment = await connection.QuerySingleOrDefaultAsync<CommitmentDetailRow>(new CommandDefinition(
                _selectCommitmentWithPolicySql,
                new
                {
                    TeacherProfileId = assignment.TeacherProfileId,
                    AcademicPeriodId = assignment.AcademicPeriodId
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (commitment is not null)
            {
                var newPlanned = Math.Max(0.00m, commitment.PlannedWorkloadHours - val);
                var newCommitted = commitment.CommittedWorkloadHours + val;
                var newRemaining = commitment.MaxCapacityLimitHours - newCommitted;
                var newActiveCount = commitment.ActiveAssignmentCount + 1;
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
                    _updateCommitmentApprovalSql,
                    new
                    {
                        CommitmentId = commitment.Id,
                        PlannedWorkloadHours = newPlanned,
                        CommittedWorkloadHours = newCommitted,
                        RemainingCapacityHours = newRemaining,
                        ActiveAssignmentCount = newActiveCount,
                        Status = commitmentStatus,
                        UpdatedBy = auditUser
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
            }
            else
            {
                // Edge case: commitment row did not exist prior to approval
                var policy = await connection.QuerySingleOrDefaultAsync<ActivePolicyRow>(new CommandDefinition(
                    _selectActivePolicySql,
                    transaction: transaction,
                    cancellationToken: cancellationToken));

                var maxCap = policy?.MaxStandardCapacity ?? 24.00m;
                var warning = policy?.WarningThreshold ?? 20.00m;
                var prohibited = policy?.ProhibitedThreshold ?? 28.00m;
                var remaining = maxCap - val;

                string commitmentStatus;
                if (val > prohibited)
                {
                    commitmentStatus = "breached";
                }
                else if (val >= warning)
                {
                    commitmentStatus = "approaching_limit";
                }
                else if (val == 0)
                {
                    commitmentStatus = "under_allocated";
                }
                else
                {
                    commitmentStatus = "normal";
                }

                if (policy is not null)
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        _insertCommitmentApprovalSql,
                        new
                        {
                            Id = Guid.NewGuid(),
                            TeacherProfileId = assignment.TeacherProfileId,
                            AcademicPeriodId = assignment.AcademicPeriodId,
                            WorkloadPolicyId = policy.Id,
                            CommittedWorkloadHours = val,
                            MaxCapacityLimitHours = maxCap,
                            RemainingCapacityHours = remaining,
                            Status = commitmentStatus,
                            CreatedBy = auditUser
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                }
            }

            // 4. Evaluate teaching demand staffing headcount
            var requiredHeadcount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                _selectDemandHeadcountSql,
                new { DemandId = assignment.TeachingDemandId },
                transaction: transaction,
                cancellationToken: cancellationToken));

            var approvedCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                _countApprovedAssignmentsSql,
                new { DemandId = assignment.TeachingDemandId },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (approvedCount >= requiredHeadcount)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    _updateDemandStaffedSql,
                    new
                    {
                        DemandId = assignment.TeachingDemandId,
                        UpdatedBy = auditUser
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
            }

            // 5. Append audit entry to teaching.teaching_history_entries
            var changePayload = JsonSerializer.Serialize(new
            {
                AssignmentId = assignment.Id,
                assignment.AssignmentNumber,
                assignment.TeachingDemandId,
                assignment.AssignmentScope,
                assignment.ScopeLabel,
                assignment.WorkloadCommitmentValue,
                ApprovedDate = approvedDate,
                ApprovedBy = auditUser
            });

            await connection.ExecuteAsync(new CommandDefinition(
                _insertTeachingHistorySql,
                new
                {
                    Id = Guid.NewGuid(),
                    TeacherProfileId = assignment.TeacherProfileId,
                    EntityId = assignment.Id,
                    EffectiveDate = approvedDate,
                    ActorId = auditUser,
                    EventSummary = $"Penugasan {assignment.AssignmentNumber} disetujui untuk lingkup '{assignment.ScopeLabel}' dengan beban {assignment.WorkloadCommitmentValue} jam.",
                    ChangePayload = changePayload,
                    Notes = string.IsNullOrWhiteSpace(request?.Notes) ? null : request.Notes.Trim(),
                    CreatedBy = auditUser
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);

            // 6. Publish domain event
            var approvedEvent = new TeachingAssignmentApprovedEvent(
                assignment.Id,
                assignment.AssignmentNumber,
                assignment.TeachingDemandId,
                assignment.TeacherProfileId,
                assignment.AssignmentRoleId,
                assignment.AssignmentScope,
                assignment.ScopeReferenceId,
                assignment.AcademicPeriodId,
                assignment.WorkloadCommitmentValue,
                auditUser,
                approvedDate);

            await publisher.Publish(approvedEvent, cancellationToken);

            return Result.Success(new ApproveTeachingAssignmentResponse(
                assignment.Id,
                assignment.AssignmentNumber,
                "approved",
                approvedDate,
                auditUser,
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
        Guid AssignmentRoleId,
        string AssignmentScope,
        Guid ScopeReferenceId,
        string ScopeLabel,
        Guid AcademicPeriodId,
        decimal WorkloadCommitmentValue,
        string Status,
        bool CountsTowardWorkload);

    private sealed record CommitmentDetailRow(
        Guid Id,
        Guid WorkloadPolicyId,
        decimal PlannedWorkloadHours,
        decimal CommittedWorkloadHours,
        decimal MaxCapacityLimitHours,
        int ActiveAssignmentCount,
        int ApprovedExceptionCount,
        decimal? WarningThreshold,
        decimal? ProhibitedThreshold);

    private sealed record ActivePolicyRow(
        Guid Id,
        decimal MaxStandardCapacity,
        decimal WarningThreshold,
        decimal ProhibitedThreshold);
}
