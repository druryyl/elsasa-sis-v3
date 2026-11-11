namespace Elsasa.Sis.Teaching.Features.Workload.ApproveWorkloadException;

using System.Text.Json;
using Dapper;
using Elsasa.Sis.Contracts.Teaching.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler executing Teaching Authority workload exception approval (TD-04, P3-S10).
/// Enforces institutional governance (no self-approval), updates commitments, resolves conflicts,
/// appends audit log, and publishes <see cref="WorkloadExceptionApprovedEvent"/>.
/// </summary>
public sealed class ApproveWorkloadExceptionHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static ApproveWorkloadExceptionHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectExceptionSql = """
        SELECT
            we.id AS Id,
            we.exception_number AS ExceptionNumber,
            we.teaching_assignment_id AS TeachingAssignmentId,
            we.teacher_profile_id AS TeacherProfileId,
            we.workload_policy_id AS WorkloadPolicyId,
            we.exceeded_hours AS ExceededHours,
            we.justification AS Justification,
            we.recommended_by AS RecommendedBy,
            we.recommended_date AS RecommendedDate,
            we.status AS Status,
            ta.academic_period_id AS AcademicPeriodId,
            tp.email AS TeacherEmail,
            tp.employee_ref AS TeacherEmployeeRef
        FROM teaching.workload_exceptions we
        INNER JOIN teaching.teaching_assignments ta ON ta.id = we.teaching_assignment_id
        INNER JOIN teaching.teacher_profiles tp ON tp.id = we.teacher_profile_id
        WHERE we.id = @Id;
        """;

    private const string _updateExceptionSql = """
        UPDATE teaching.workload_exceptions
        SET
            status = 'approved',
            approved_by = @ApprovedBy,
            approved_date = @ApprovedDate,
            decision_remarks = @DecisionRemarks,
            updated_at = SYSUTCDATETIME(),
            updated_by = @ApprovedBy
        WHERE id = @Id AND status IN ('requested', 'recommended');
        """;

    private const string _updateCommitmentSql = """
        UPDATE teaching.workload_commitments
        SET
            approved_exception_count = approved_exception_count + 1,
            status = CASE
                WHEN status = 'breached' THEN 'overloaded_with_exception'
                ELSE status
            END,
            as_of_timestamp = SYSUTCDATETIME()
        WHERE teacher_profile_id = @TeacherProfileId
          AND academic_period_id = @AcademicPeriodId;
        """;

    private const string _selectCommitmentSummarySql = """
        SELECT
            approved_exception_count AS ApprovedExceptionCount,
            status AS Status
        FROM teaching.workload_commitments
        WHERE teacher_profile_id = @TeacherProfileId
          AND academic_period_id = @AcademicPeriodId;
        """;

    private const string _resolveConflictsSql = """
        UPDATE teaching.capacity_conflicts
        SET
            status = 'waived_with_exception',
            resolution_action = 'WorkloadExceptionApproved',
            resolved_by = @ApprovedBy,
            resolved_date = @ApprovedDate,
            resolution_notes = @ResolutionNotes,
            updated_at = SYSUTCDATETIME(),
            updated_by = @ApprovedBy
        WHERE teacher_profile_id = @TeacherProfileId
          AND (teaching_assignment_id = @TeachingAssignmentId OR teaching_assignment_id IS NULL)
          AND severity = 'blocking'
          AND status = 'unresolved';
        """;

    private const string _insertHistoryEntrySql = """
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
            'WorkloadExceptionApproved',
            'WorkloadException',
            @EntityId,
            @EffectiveDate,
            SYSUTCDATETIME(),
            'TeachingAuthority',
            @ActorId,
            @EventSummary,
            @ChangePayload,
            @Notes,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    public async Task<Result<ApproveWorkloadExceptionResponse>> HandleAsync(
        Guid id,
        ApproveWorkloadExceptionRequest? request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Fetch workload exception with linked assignment and teacher profile
        var existing = await connection.QuerySingleOrDefaultAsync<WorkloadExceptionDetail>(new CommandDefinition(
            _selectExceptionSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<ApproveWorkloadExceptionResponse>(DomainError.NotFound(
                "Teaching.WorkloadExceptionNotFound",
                $"Workload exception with ID '{id}' was not found."));
        }

        // 2. Validate current status
        if (string.Equals(existing.Status, "approved", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ApproveWorkloadExceptionResponse>(DomainError.Conflict(
                "Teaching.WorkloadExceptionAlreadyApproved",
                $"Workload exception '{existing.ExceptionNumber}' is already approved."));
        }

        if (string.Equals(existing.Status, "rejected", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ApproveWorkloadExceptionResponse>(DomainError.Conflict(
                "Teaching.WorkloadExceptionRejected",
                $"Workload exception '{existing.ExceptionNumber}' has already been rejected and cannot be approved."));
        }

        // 3. Enforce Governance: Prohibit teacher self-approval
        var isSelfApproval = (currentUser.Id.HasValue && currentUser.Id.Value == existing.TeacherProfileId)
            || (!string.IsNullOrWhiteSpace(currentUser.Email) && string.Equals(currentUser.Email, existing.TeacherEmail, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(currentUser.Username) && (
                string.Equals(currentUser.Username, existing.TeacherEmail, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(currentUser.Username, existing.TeacherEmployeeRef, StringComparison.OrdinalIgnoreCase)));

        if (isSelfApproval)
        {
            return Result.Failure<ApproveWorkloadExceptionResponse>(DomainError.Conflict(
                "Teaching.SelfApprovalProhibited",
                "Self-approval is prohibited: Teaching Authority cannot approve their own workload exception."));
        }

        var approvedBy = currentUser.Username ?? "system";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 4. Execute atomic state transitions in single SqlTransaction
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // A. Update workload_exceptions status
            var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
                _updateExceptionSql,
                new
                {
                    Id = id,
                    ApprovedBy = approvedBy,
                    ApprovedDate = today.ToDateTime(TimeOnly.MinValue),
                    DecisionRemarks = string.IsNullOrWhiteSpace(request?.DecisionRemarks) ? null : request.DecisionRemarks.Trim()
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (rowsAffected == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<ApproveWorkloadExceptionResponse>(DomainError.Conflict(
                    "Teaching.WorkloadExceptionAlreadyProcessed",
                    $"Workload exception '{existing.ExceptionNumber}' could not be approved because its status has changed."));
            }

            // B. Increment approved_exception_count & transition breached -> overloaded_with_exception in commitments
            var commitmentUpdated = await connection.ExecuteAsync(new CommandDefinition(
                _updateCommitmentSql,
                new
                {
                    TeacherProfileId = existing.TeacherProfileId,
                    AcademicPeriodId = existing.AcademicPeriodId
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            int approvedExceptionCount = 1;
            string commitmentStatus = "overloaded_with_exception";

            if (commitmentUpdated > 0)
            {
                var updatedCommitment = await connection.QuerySingleOrDefaultAsync<CommitmentSummary>(new CommandDefinition(
                    _selectCommitmentSummarySql,
                    new
                    {
                        TeacherProfileId = existing.TeacherProfileId,
                        AcademicPeriodId = existing.AcademicPeriodId
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

                if (updatedCommitment is not null)
                {
                    approvedExceptionCount = updatedCommitment.ApprovedExceptionCount;
                    commitmentStatus = updatedCommitment.Status;
                }
            }

            // C. Resolve associated blocking capacity conflicts
            var resolvedConflictsCount = await connection.ExecuteAsync(new CommandDefinition(
                _resolveConflictsSql,
                new
                {
                    TeacherProfileId = existing.TeacherProfileId,
                    TeachingAssignmentId = existing.TeachingAssignmentId,
                    ApprovedBy = approvedBy,
                    ApprovedDate = today.ToDateTime(TimeOnly.MinValue),
                    ResolutionNotes = $"Waived via approved workload exception {existing.ExceptionNumber}."
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // D. Append entry to teaching.teaching_history_entries
            var changePayload = JsonSerializer.Serialize(new
            {
                ExceptionId = existing.Id,
                ExceptionNumber = existing.ExceptionNumber,
                TeachingAssignmentId = existing.TeachingAssignmentId,
                ExceededHours = existing.ExceededHours,
                ApprovedBy = approvedBy,
                ApprovedDate = today,
                DecisionRemarks = request?.DecisionRemarks
            });

            await connection.ExecuteAsync(new CommandDefinition(
                _insertHistoryEntrySql,
                new
                {
                    Id = Guid.NewGuid(),
                    TeacherProfileId = existing.TeacherProfileId,
                    EntityId = existing.Id,
                    EffectiveDate = today.ToDateTime(TimeOnly.MinValue),
                    ActorId = currentUser.UserId?.ToString() ?? currentUser.Username ?? "system",
                    EventSummary = $"Workload exception {existing.ExceptionNumber} approved by {approvedBy} for {existing.ExceededHours:F2} overload hours.",
                    ChangePayload = changePayload,
                    Notes = string.IsNullOrWhiteSpace(request?.DecisionRemarks) ? null : request.DecisionRemarks.Trim(),
                    CreatedBy = approvedBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);

            // E. Publish in-memory domain event after transaction commits
            var domainEvent = new WorkloadExceptionApprovedEvent(
                existing.Id,
                existing.ExceptionNumber,
                existing.TeachingAssignmentId,
                existing.TeacherProfileId,
                existing.AcademicPeriodId,
                existing.ExceededHours,
                approvedBy,
                today);

            await publisher.Publish(domainEvent, cancellationToken);

            return Result.Success(new ApproveWorkloadExceptionResponse(
                existing.Id,
                existing.ExceptionNumber,
                existing.TeachingAssignmentId,
                existing.TeacherProfileId,
                existing.AcademicPeriodId,
                existing.ExceededHours,
                "approved",
                approvedBy,
                today,
                request?.DecisionRemarks?.Trim(),
                approvedExceptionCount,
                commitmentStatus,
                resolvedConflictsCount,
                DateTimeOffset.UtcNow));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private sealed record WorkloadExceptionDetail(
        Guid Id,
        string ExceptionNumber,
        Guid TeachingAssignmentId,
        Guid TeacherProfileId,
        Guid WorkloadPolicyId,
        decimal ExceededHours,
        string Justification,
        string RecommendedBy,
        DateOnly RecommendedDate,
        string Status,
        Guid AcademicPeriodId,
        string? TeacherEmail,
        string? TeacherEmployeeRef);

    private sealed record CommitmentSummary(int ApprovedExceptionCount, string Status);
}
