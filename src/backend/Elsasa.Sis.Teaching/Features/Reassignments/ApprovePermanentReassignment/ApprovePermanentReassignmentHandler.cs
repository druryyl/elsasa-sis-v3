namespace Elsasa.Sis.Teaching.Features.Reassignments.ApprovePermanentReassignment;

using System.Text.Json;
using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Contracts.Teaching.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler approving a permanent teaching responsibility reassignment, creating a replacement assignment,
/// transferring workload commitments, logging audit history, and publishing domain events (TD-01, TD-04, P4-S12).
/// </summary>
public sealed class ApprovePermanentReassignmentHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static ApprovePermanentReassignmentHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectReassignmentSql = """
        SELECT
            id AS Id,
            reassignment_number AS ReassignmentNumber,
            original_assignment_id AS OriginalAssignmentId,
            predecessor_teacher_id AS PredecessorTeacherId,
            new_teacher_id AS NewTeacherId,
            replacement_assignment_id AS ReplacementAssignmentId,
            effective_date AS EffectiveDate,
            reassignment_type AS ReassignmentType,
            reason AS Reason,
            requires_handover AS RequiresHandover,
            handover_status AS HandoverStatus,
            handover_completed_date AS HandoverCompletedDate,
            handover_notes AS HandoverNotes,
            status AS Status,
            approved_by AS ApprovedBy,
            approved_date AS ApprovedDate,
            created_at AS CreatedAt
        FROM teaching.teaching_reassignments WITH (UPDLOCK, ROWLOCK)
        WHERE id = @Id;
        """;

    private const string _selectOriginalAssignmentSql = """
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
            ta.effective_start_date AS EffectiveStartDate,
            ta.effective_end_date AS EffectiveEndDate,
            ta.workload_commitment_value AS WorkloadCommitmentValue,
            ta.workload_unit AS WorkloadUnit,
            ta.status AS Status,
            ta.notes AS Notes,
            ar.counts_toward_workload AS CountsTowardWorkload
        FROM teaching.teaching_assignments ta WITH (UPDLOCK, ROWLOCK)
        INNER JOIN teaching.assignment_roles ar ON ar.id = ta.assignment_role_id
        WHERE ta.id = @Id;
        """;

    private const string _countAssignmentPrefixSql = """
        SELECT COUNT(1)
        FROM teaching.teaching_assignments
        WHERE assignment_number LIKE @Prefix;
        """;

    private const string _checkAssignmentNumberSql = """
        SELECT 1
        FROM teaching.teaching_assignments
        WHERE assignment_number = @AssignmentNumber;
        """;

    private const string _insertReplacementAssignmentSql = """
        INSERT INTO teaching.teaching_assignments (
            id,
            assignment_number,
            teaching_demand_id,
            teacher_profile_id,
            assignment_role_id,
            assignment_scope,
            scope_reference_id,
            scope_label,
            academic_period_id,
            effective_start_date,
            effective_end_date,
            workload_commitment_value,
            workload_unit,
            status,
            approved_by,
            approved_date,
            teacher_acknowledged,
            teacher_acknowledged_date,
            predecessor_assignment_id,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @AssignmentNumber,
            @TeachingDemandId,
            @TeacherProfileId,
            @AssignmentRoleId,
            @AssignmentScope,
            @ScopeReferenceId,
            @ScopeLabel,
            @AcademicPeriodId,
            @EffectiveStartDate,
            @EffectiveEndDate,
            @WorkloadCommitmentValue,
            @WorkloadUnit,
            'approved',
            @ApprovedBy,
            @ApprovedDate,
            0,
            NULL,
            @PredecessorAssignmentId,
            @Notes,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    private const string _updateOriginalAssignmentSql = """
        UPDATE teaching.teaching_assignments
        SET status = 'completed',
            notes = @Notes,
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private const string _updateReassignmentApprovalSql = """
        UPDATE teaching.teaching_reassignments
        SET status = 'approved',
            replacement_assignment_id = @ReplacementAssignmentId,
            approved_by = @ApprovedBy,
            approved_date = @ApprovedDate,
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private const string _selectCommitmentForUpdateSql = """
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

    private const string _updateCommitmentSql = """
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

    private const string _insertCommitmentSql = """
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
            @ActiveAssignmentCount,
            0,
            SYSUTCDATETIME(),
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    private const string _insertHistorySql = """
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
            'teaching_reassignments',
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

    public async Task<Result<ApprovePermanentReassignmentResponse>> HandleAsync(
        Guid id,
        ApprovePermanentReassignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        if (connection is not SqlConnection sqlConnection)
        {
            throw new InvalidOperationException("ApprovePermanentReassignment requires a valid SQL Server connection.");
        }

        using var transaction = sqlConnection.BeginTransaction();

        try
        {
            // 1. Fetch reassignment with lock
            var reassignment = await connection.QuerySingleOrDefaultAsync<ReassignmentRow>(new CommandDefinition(
                _selectReassignmentSql,
                new { Id = id },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (reassignment is null)
            {
                transaction.Rollback();
                return Result.Failure<ApprovePermanentReassignmentResponse>(DomainError.NotFound(
                    "Teaching.ReassignmentNotFound",
                    $"Reassignment with ID '{id}' was not found."));
            }

            if (!string.Equals(reassignment.Status, "proposed", StringComparison.OrdinalIgnoreCase))
            {
                transaction.Rollback();
                return Result.Failure<ApprovePermanentReassignmentResponse>(DomainError.Conflict(
                    "Teaching.ReassignmentNotPending",
                    $"Reassignment '{reassignment.ReassignmentNumber}' is in '{reassignment.Status}' status and cannot be approved."));
            }

            // 2. Fetch original assignment with lock
            var origAssignment = await connection.QuerySingleOrDefaultAsync<OriginalAssignmentRow>(new CommandDefinition(
                _selectOriginalAssignmentSql,
                new { Id = reassignment.OriginalAssignmentId },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (origAssignment is null)
            {
                transaction.Rollback();
                return Result.Failure<ApprovePermanentReassignmentResponse>(DomainError.NotFound(
                    "Teaching.OriginalAssignmentNotFound",
                    $"Original teaching assignment with ID '{reassignment.OriginalAssignmentId}' was not found."));
            }

            if (!string.Equals(origAssignment.Status, "approved", StringComparison.OrdinalIgnoreCase))
            {
                transaction.Rollback();
                return Result.Failure<ApprovePermanentReassignmentResponse>(DomainError.Conflict(
                    "Teaching.OriginalAssignmentNotApproved",
                    $"Original assignment '{origAssignment.AssignmentNumber}' is in '{origAssignment.Status}' status and cannot be permanently reassigned."));
            }

            var auditUser = !string.IsNullOrWhiteSpace(currentUser.Username) ? currentUser.Username : "Teaching Authority";
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // 3. Generate ASN-YYYY-XXXX for replacement assignment
            var year = DateTime.UtcNow.Year;
            var asnPrefix = $"ASN-{year}-";
            var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                _countAssignmentPrefixSql,
                new { Prefix = asnPrefix + "%" },
                transaction: transaction,
                cancellationToken: cancellationToken));

            var seq = count + 1;
            string replacementAsnNumber;
            while (true)
            {
                var candidate = $"{asnPrefix}{seq:D4}";
                var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                    _checkAssignmentNumberSql,
                    new { AssignmentNumber = candidate },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

                if (!exists)
                {
                    replacementAsnNumber = candidate;
                    break;
                }

                seq++;
            }

            var replacementAssignmentId = Guid.NewGuid();
            var replacementNotes = string.IsNullOrWhiteSpace(request.Notes)
                ? $"Reassigned permanently from assignment {origAssignment.AssignmentNumber} (Reassignment: {reassignment.ReassignmentNumber})."
                : $"Reassigned permanently from assignment {origAssignment.AssignmentNumber} (Reassignment: {reassignment.ReassignmentNumber}). {request.Notes.Trim()}";

            // 4. Create new replacement assignment
            await connection.ExecuteAsync(new CommandDefinition(
                _insertReplacementAssignmentSql,
                new
                {
                    Id = replacementAssignmentId,
                    AssignmentNumber = replacementAsnNumber,
                    TeachingDemandId = origAssignment.TeachingDemandId,
                    TeacherProfileId = reassignment.NewTeacherId,
                    AssignmentRoleId = origAssignment.AssignmentRoleId,
                    AssignmentScope = origAssignment.AssignmentScope,
                    ScopeReferenceId = origAssignment.ScopeReferenceId,
                    ScopeLabel = origAssignment.ScopeLabel,
                    AcademicPeriodId = origAssignment.AcademicPeriodId,
                    EffectiveStartDate = reassignment.EffectiveDate,
                    EffectiveEndDate = origAssignment.EffectiveEndDate,
                    WorkloadCommitmentValue = origAssignment.WorkloadCommitmentValue,
                    WorkloadUnit = origAssignment.WorkloadUnit,
                    ApprovedBy = auditUser,
                    ApprovedDate = today,
                    PredecessorAssignmentId = origAssignment.Id,
                    Notes = replacementNotes,
                    CreatedBy = auditUser
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // 5. Update original assignment status to 'completed' (superseded by replacement)
            var supersededNotes = string.IsNullOrWhiteSpace(origAssignment.Notes)
                ? $"Superseded by replacement assignment {replacementAsnNumber}."
                : $"{origAssignment.Notes} | Superseded by replacement assignment {replacementAsnNumber}.";

            await connection.ExecuteAsync(new CommandDefinition(
                _updateOriginalAssignmentSql,
                new
                {
                    Id = origAssignment.Id,
                    Notes = supersededNotes,
                    UpdatedBy = auditUser
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // 6. Update reassignment status to 'approved' and link replacement assignment
            await connection.ExecuteAsync(new CommandDefinition(
                _updateReassignmentApprovalSql,
                new
                {
                    Id = reassignment.Id,
                    ReplacementAssignmentId = replacementAssignmentId,
                    ApprovedBy = auditUser,
                    ApprovedDate = today,
                    UpdatedBy = auditUser
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // 7. Transfer workload commitments if role counts toward workload
            if (origAssignment.CountsTowardWorkload && origAssignment.WorkloadCommitmentValue > 0)
            {
                var val = origAssignment.WorkloadCommitmentValue;

                // Deduct from predecessor teacher
                var predCommitment = await connection.QuerySingleOrDefaultAsync<CommitmentRow>(new CommandDefinition(
                    _selectCommitmentForUpdateSql,
                    new
                    {
                        TeacherProfileId = reassignment.PredecessorTeacherId,
                        AcademicPeriodId = origAssignment.AcademicPeriodId
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

                if (predCommitment is not null)
                {
                    var newCommitted = Math.Max(0.00m, predCommitment.CommittedWorkloadHours - val);
                    var newActive = Math.Max(0, predCommitment.ActiveAssignmentCount - 1);
                    var newRemaining = predCommitment.MaxCapacityLimitHours - newCommitted;
                    var warn = predCommitment.WarningThreshold ?? 20.00m;
                    var proh = predCommitment.ProhibitedThreshold ?? 28.00m;
                    var newStatus = CalculateCommitmentStatus(newCommitted, warn, proh, predCommitment.ApprovedExceptionCount);

                    await connection.ExecuteAsync(new CommandDefinition(
                        _updateCommitmentSql,
                        new
                        {
                            CommitmentId = predCommitment.Id,
                            PlannedWorkloadHours = predCommitment.PlannedWorkloadHours,
                            CommittedWorkloadHours = newCommitted,
                            RemainingCapacityHours = newRemaining,
                            ActiveAssignmentCount = newActive,
                            Status = newStatus,
                            UpdatedBy = auditUser
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                }

                // Add to new teacher
                var newCommitment = await connection.QuerySingleOrDefaultAsync<CommitmentRow>(new CommandDefinition(
                    _selectCommitmentForUpdateSql,
                    new
                    {
                        TeacherProfileId = reassignment.NewTeacherId,
                        AcademicPeriodId = origAssignment.AcademicPeriodId
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

                if (newCommitment is not null)
                {
                    var newCommitted = newCommitment.CommittedWorkloadHours + val;
                    var newActive = newCommitment.ActiveAssignmentCount + 1;
                    var newRemaining = newCommitment.MaxCapacityLimitHours - newCommitted;
                    var warn = newCommitment.WarningThreshold ?? 20.00m;
                    var proh = newCommitment.ProhibitedThreshold ?? 28.00m;
                    var newStatus = CalculateCommitmentStatus(newCommitted, warn, proh, newCommitment.ApprovedExceptionCount);

                    await connection.ExecuteAsync(new CommandDefinition(
                        _updateCommitmentSql,
                        new
                        {
                            CommitmentId = newCommitment.Id,
                            PlannedWorkloadHours = newCommitment.PlannedWorkloadHours,
                            CommittedWorkloadHours = newCommitted,
                            RemainingCapacityHours = newRemaining,
                            ActiveAssignmentCount = newActive,
                            Status = newStatus,
                            UpdatedBy = auditUser
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                }
                else
                {
                    var policy = await connection.QuerySingleOrDefaultAsync<ActivePolicyRow>(new CommandDefinition(
                        _selectActivePolicySql,
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                    var maxCap = policy?.MaxStandardCapacity ?? 24.00m;
                    var warn = policy?.WarningThreshold ?? 20.00m;
                    var proh = policy?.ProhibitedThreshold ?? 28.00m;
                    var remaining = maxCap - val;
                    var newStatus = CalculateCommitmentStatus(val, warn, proh, 0);

                    if (policy is not null)
                    {
                        await connection.ExecuteAsync(new CommandDefinition(
                            _insertCommitmentSql,
                            new
                            {
                                Id = Guid.NewGuid(),
                                TeacherProfileId = reassignment.NewTeacherId,
                                AcademicPeriodId = origAssignment.AcademicPeriodId,
                                WorkloadPolicyId = policy.Id,
                                CommittedWorkloadHours = val,
                                MaxCapacityLimitHours = maxCap,
                                RemainingCapacityHours = remaining,
                                Status = newStatus,
                                ActiveAssignmentCount = 1,
                                CreatedBy = auditUser
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken));
                    }
                }
            }

            // 8. Append audit history entries for both teachers
            var changePayload = JsonSerializer.Serialize(new
            {
                ReassignmentId = reassignment.Id,
                reassignment.ReassignmentNumber,
                OriginalAssignmentId = origAssignment.Id,
                OriginalAssignmentNumber = origAssignment.AssignmentNumber,
                ReplacementAssignmentId = replacementAssignmentId,
                ReplacementAssignmentNumber = replacementAsnNumber,
                reassignment.PredecessorTeacherId,
                reassignment.NewTeacherId,
                reassignment.EffectiveDate,
                ApprovedBy = auditUser,
                ApprovedDate = today
            });

            // For predecessor teacher
            await connection.ExecuteAsync(new CommandDefinition(
                _insertHistorySql,
                new
                {
                    Id = Guid.NewGuid(),
                    TeacherProfileId = reassignment.PredecessorTeacherId,
                    EventType = "PermanentReassignmentTransferred",
                    EntityId = reassignment.Id,
                    EffectiveDate = reassignment.EffectiveDate,
                    ActorId = auditUser,
                    EventSummary = $"Tanggung jawab mengajar {origAssignment.ScopeLabel} dialihkan kepada guru baru (Nomor: {reassignment.ReassignmentNumber}).",
                    ChangePayload = changePayload,
                    Notes = request.Notes,
                    CreatedBy = auditUser
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // For new teacher
            await connection.ExecuteAsync(new CommandDefinition(
                _insertHistorySql,
                new
                {
                    Id = Guid.NewGuid(),
                    TeacherProfileId = reassignment.NewTeacherId,
                    EventType = "PermanentReassignmentAssigned",
                    EntityId = reassignment.Id,
                    EffectiveDate = reassignment.EffectiveDate,
                    ActorId = auditUser,
                    EventSummary = $"Menerima pengalihan permanen tanggung jawab mengajar {origAssignment.ScopeLabel} (Nomor: {reassignment.ReassignmentNumber}, Penugasan: {replacementAsnNumber}).",
                    ChangePayload = changePayload,
                    Notes = request.Notes,
                    CreatedBy = auditUser
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            transaction.Commit();

            // 9. Publish in-memory domain event post-commit
            await publisher.Publish(
                new TeachingAssignmentApprovedEvent(
                    replacementAssignmentId,
                    replacementAsnNumber,
                    origAssignment.TeachingDemandId,
                    reassignment.NewTeacherId,
                    origAssignment.AssignmentRoleId,
                    origAssignment.AssignmentScope,
                    origAssignment.ScopeReferenceId,
                    origAssignment.AcademicPeriodId,
                    origAssignment.WorkloadCommitmentValue,
                    auditUser,
                    today),
                cancellationToken);

            var updatedReassignmentDto = new TeachingReassignmentDto(
                Id: reassignment.Id,
                ReassignmentNumber: reassignment.ReassignmentNumber,
                OriginalAssignmentId: reassignment.OriginalAssignmentId,
                PredecessorTeacherId: reassignment.PredecessorTeacherId,
                NewTeacherId: reassignment.NewTeacherId,
                ReplacementAssignmentId: replacementAssignmentId,
                EffectiveDate: reassignment.EffectiveDate,
                ReassignmentType: reassignment.ReassignmentType,
                Reason: reassignment.Reason,
                RequiresHandover: reassignment.RequiresHandover,
                HandoverStatus: reassignment.HandoverStatus,
                HandoverCompletedDate: reassignment.HandoverCompletedDate,
                HandoverNotes: reassignment.HandoverNotes,
                Status: "approved",
                ApprovedBy: auditUser,
                ApprovedDate: today,
                CreatedAt: reassignment.CreatedAt);

            var replacementAssignmentDto = new TeachingAssignmentDto(
                Id: replacementAssignmentId,
                AssignmentNumber: replacementAsnNumber,
                TeachingDemandId: origAssignment.TeachingDemandId,
                TeacherProfileId: reassignment.NewTeacherId,
                AssignmentRoleId: origAssignment.AssignmentRoleId,
                AssignmentRoleName: null,
                AssignmentScope: origAssignment.AssignmentScope,
                ScopeReferenceId: origAssignment.ScopeReferenceId,
                ScopeLabel: origAssignment.ScopeLabel,
                AcademicPeriodId: origAssignment.AcademicPeriodId,
                EffectiveStartDate: reassignment.EffectiveDate,
                EffectiveEndDate: origAssignment.EffectiveEndDate,
                WorkloadCommitmentValue: origAssignment.WorkloadCommitmentValue,
                WorkloadUnit: origAssignment.WorkloadUnit,
                Status: "approved",
                ApprovedBy: auditUser,
                ApprovedDate: today,
                TeacherAcknowledged: false,
                TeacherAcknowledgedDate: null,
                PredecessorAssignmentId: origAssignment.Id,
                Notes: replacementNotes,
                CreatedAt: DateTimeOffset.UtcNow);

            return Result.Success(new ApprovePermanentReassignmentResponse(
                updatedReassignmentDto,
                replacementAssignmentDto));
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static string CalculateCommitmentStatus(decimal committed, decimal warning, decimal prohibited, int approvedExceptions)
    {
        if (committed > prohibited)
        {
            return approvedExceptions > 0 ? "overloaded_with_exception" : "breached";
        }
        if (committed >= warning)
        {
            return "approaching_limit";
        }
        if (committed == 0)
        {
            return "under_allocated";
        }
        return "normal";
    }

    private sealed record ReassignmentRow(
        Guid Id,
        string ReassignmentNumber,
        Guid OriginalAssignmentId,
        Guid PredecessorTeacherId,
        Guid NewTeacherId,
        Guid? ReplacementAssignmentId,
        DateOnly EffectiveDate,
        string ReassignmentType,
        string Reason,
        bool RequiresHandover,
        string HandoverStatus,
        DateOnly? HandoverCompletedDate,
        string? HandoverNotes,
        string Status,
        string ApprovedBy,
        DateOnly ApprovedDate,
        DateTimeOffset CreatedAt);

    private sealed record OriginalAssignmentRow(
        Guid Id,
        string AssignmentNumber,
        Guid TeachingDemandId,
        Guid TeacherProfileId,
        Guid AssignmentRoleId,
        string AssignmentScope,
        Guid ScopeReferenceId,
        string ScopeLabel,
        Guid AcademicPeriodId,
        DateOnly EffectiveStartDate,
        DateOnly EffectiveEndDate,
        decimal WorkloadCommitmentValue,
        string WorkloadUnit,
        string Status,
        string? Notes,
        bool CountsTowardWorkload);

    private sealed record CommitmentRow(
        Guid Id,
        Guid? WorkloadPolicyId,
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
