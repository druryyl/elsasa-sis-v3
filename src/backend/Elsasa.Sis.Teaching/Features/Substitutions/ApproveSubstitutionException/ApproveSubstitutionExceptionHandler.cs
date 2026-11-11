namespace Elsasa.Sis.Teaching.Features.Substitutions.ApproveSubstitutionException;

using System.Text.Json;
using Dapper;
using Elsasa.Sis.Contracts.Teaching.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler for approving an escalated emergency substitution exception by Teaching Authority (TD-05, P4-S11).
/// </summary>
public sealed class ApproveSubstitutionExceptionHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static ApproveSubstitutionExceptionHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectSubstitutionSql = """
        SELECT
            s.id AS Id,
            s.substitution_number AS SubstitutionNumber,
            s.original_assignment_id AS OriginalAssignmentId,
            s.original_teacher_id AS OriginalTeacherId,
            s.substitute_teacher_id AS SubstituteTeacherId,
            s.coverage_start_date AS CoverageStartDate,
            s.coverage_end_date AS CoverageEndDate,
            s.coverage_scope_type AS CoverageScopeType,
            s.partial_scope_details AS PartialScopeDetails,
            s.reason AS Reason,
            s.workload_impact_hours AS WorkloadImpactHours,
            s.status AS Status,
            s.handover_notes AS HandoverNotes,
            s.approved_by AS ApprovedBy,
            s.approved_date AS ApprovedDate,
            s.created_at AS CreatedAt
        FROM teaching.teaching_substitutions s WITH (UPDLOCK, ROWLOCK)
        WHERE s.id = @Id;
        """;

    private const string _updateSubstitutionApprovalSql = """
        UPDATE teaching.teaching_substitutions
        SET status = 'approved',
            approved_by = @ApprovedBy,
            approved_date = @ApprovedDate,
            handover_notes = CASE
                WHEN @DecisionRemarks IS NOT NULL AND @DecisionRemarks <> ''
                THEN ISNULL(handover_notes + CHAR(13) + CHAR(10), '') + '[Authority Approval Remarks]: ' + @DecisionRemarks
                ELSE handover_notes
            END,
            updated_at = SYSUTCDATETIME(),
            updated_by = @ApprovedBy
        WHERE id = @Id;
        """;

    private const string _resolveCapacityConflictsSql = """
        UPDATE teaching.capacity_conflicts
        SET status = 'resolved',
            resolution_action = 'AUTHORITY_EXCEPTION_APPROVED',
            resolved_by = @ApprovedBy,
            resolved_date = @ResolvedDate,
            resolution_notes = @ResolutionNotes,
            updated_at = SYSUTCDATETIME(),
            updated_by = @ApprovedBy
        WHERE (teaching_assignment_id = @OriginalAssignmentId OR teacher_profile_id = @SubstituteTeacherId)
          AND conflict_classification = 'substitution_exception'
          AND severity = 'blocking'
          AND status = 'unresolved';
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
            @EntityType,
            @EntityId,
            @EffectiveDate,
            SYSUTCDATETIME(),
            @ActorRole,
            @ActorId,
            @EventSummary,
            @ChangePayload,
            @Notes,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    public async Task<Result<ApproveSubstitutionExceptionResponse>> HandleAsync(
        Guid id,
        ApproveSubstitutionExceptionRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result.Failure<ApproveSubstitutionExceptionResponse>(DomainError.Validation(
                "Teaching.InvalidSubstitutionId",
                "Substitution ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        if (connection is not SqlConnection sqlConnection)
        {
            return Result.Failure<ApproveSubstitutionExceptionResponse>(DomainError.Failure(
                "Teaching.InvalidConnection",
                "Expected a SQL Server connection for transactional operations."));
        }

        await using var tx = (SqlTransaction)await sqlConnection.BeginTransactionAsync(cancellationToken);

        try
        {
            var sub = await connection.QuerySingleOrDefaultAsync<SubstitutionRecord>(new CommandDefinition(
                _selectSubstitutionSql,
                new { Id = id },
                transaction: tx,
                cancellationToken: cancellationToken));

            if (sub is null)
            {
                await tx.RollbackAsync(cancellationToken);
                return Result.Failure<ApproveSubstitutionExceptionResponse>(DomainError.NotFound(
                    "Teaching.SubstitutionNotFound",
                    $"Substitution with ID '{id}' was not found."));
            }

            if (sub.Status == "approved")
            {
                await tx.RollbackAsync(cancellationToken);
                return Result.Failure<ApproveSubstitutionExceptionResponse>(DomainError.Conflict(
                    "Teaching.SubstitutionAlreadyApproved",
                    $"Substitution '{sub.SubstitutionNumber}' is already approved."));
            }

            if (sub.Status != "proposed")
            {
                await tx.RollbackAsync(cancellationToken);
                return Result.Failure<ApproveSubstitutionExceptionResponse>(DomainError.Conflict(
                    "Teaching.InvalidSubstitutionStatus",
                    $"Only substitutions with status 'proposed' can be approved as exceptions. Current status is '{sub.Status}'."));
            }

            var approver = currentUser.Username ?? "teaching-authority";
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var remarks = request?.DecisionRemarks?.Trim();

            // 1. Update substitution status to approved
            await connection.ExecuteAsync(new CommandDefinition(
                _updateSubstitutionApprovalSql,
                new
                {
                    Id = id,
                    ApprovedBy = approver,
                    ApprovedDate = today,
                    DecisionRemarks = remarks
                },
                transaction: tx,
                cancellationToken: cancellationToken));

            // 2. Resolve blocking capacity conflict(s)
            await connection.ExecuteAsync(new CommandDefinition(
                _resolveCapacityConflictsSql,
                new
                {
                    OriginalAssignmentId = sub.OriginalAssignmentId,
                    SubstituteTeacherId = sub.SubstituteTeacherId,
                    ApprovedBy = approver,
                    ResolvedDate = today,
                    ResolutionNotes = string.IsNullOrWhiteSpace(remarks)
                        ? $"Approved by Teaching Authority ({approver}) under emergency substitution exception."
                        : remarks
                },
                transaction: tx,
                cancellationToken: cancellationToken));

            // 3. Append history entry
            await connection.ExecuteAsync(new CommandDefinition(
                _insertTeachingHistorySql,
                new
                {
                    Id = Guid.NewGuid(),
                    TeacherProfileId = sub.SubstituteTeacherId,
                    EventType = "SUBSTITUTION_APPROVED",
                    EntityType = "teaching_substitution",
                    EntityId = id,
                    EffectiveDate = sub.CoverageStartDate,
                    ActorRole = "Teaching Authority",
                    ActorId = approver,
                    EventSummary = $"Emergency substitution {sub.SubstitutionNumber} exception approved by Teaching Authority ({approver}).",
                    ChangePayload = JsonSerializer.Serialize(new
                    {
                        SubstitutionId = id,
                        SubstitutionNumber = sub.SubstitutionNumber,
                        ApprovedBy = approver,
                        ApprovedDate = today,
                        DecisionRemarks = remarks,
                        IsDelegatedApproval = false
                    }),
                    Notes = remarks,
                    CreatedBy = approver
                },
                transaction: tx,
                cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);

            // 4. Publish in-memory post-commit domain event
            await publisher.Publish(new TeachingSubstitutionAssignedEvent(
                substitutionId: id,
                substitutionNumber: sub.SubstitutionNumber,
                originalAssignmentId: sub.OriginalAssignmentId,
                originalTeacherId: sub.OriginalTeacherId,
                substituteTeacherId: sub.SubstituteTeacherId,
                coverageStartDate: sub.CoverageStartDate,
                coverageEndDate: sub.CoverageEndDate,
                workloadImpactHours: sub.WorkloadImpactHours,
                approvedBy: approver,
                isDelegatedApproval: false),
                cancellationToken);

            return Result.Success(new ApproveSubstitutionExceptionResponse(
                Id: id,
                SubstitutionNumber: sub.SubstitutionNumber,
                OriginalAssignmentId: sub.OriginalAssignmentId,
                OriginalTeacherId: sub.OriginalTeacherId,
                SubstituteTeacherId: sub.SubstituteTeacherId,
                CoverageStartDate: sub.CoverageStartDate,
                CoverageEndDate: sub.CoverageEndDate,
                CoverageScopeType: sub.CoverageScopeType,
                PartialScopeDetails: sub.PartialScopeDetails,
                Reason: sub.Reason,
                WorkloadImpactHours: sub.WorkloadImpactHours,
                Status: "approved",
                HandoverNotes: sub.HandoverNotes,
                ApprovedBy: approver,
                ApprovedDate: today,
                UpdatedAt: DateTimeOffset.UtcNow));
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private sealed record SubstitutionRecord(
        Guid Id,
        string SubstitutionNumber,
        Guid OriginalAssignmentId,
        Guid OriginalTeacherId,
        Guid SubstituteTeacherId,
        DateOnly CoverageStartDate,
        DateOnly CoverageEndDate,
        string CoverageScopeType,
        string? PartialScopeDetails,
        string Reason,
        decimal WorkloadImpactHours,
        string Status,
        string? HandoverNotes,
        string ApprovedBy,
        DateOnly ApprovedDate,
        DateTimeOffset CreatedAt);
}
