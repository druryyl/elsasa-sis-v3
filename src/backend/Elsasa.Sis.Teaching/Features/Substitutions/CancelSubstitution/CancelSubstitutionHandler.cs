namespace Elsasa.Sis.Teaching.Features.Substitutions.CancelSubstitution;

using System.Text.Json;
using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler for cancelling an emergency substitution arrangement (TD-05, P4-S11).
/// </summary>
public sealed class CancelSubstitutionHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static CancelSubstitutionHandler()
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
            s.status AS Status
        FROM teaching.teaching_substitutions s WITH (UPDLOCK, ROWLOCK)
        WHERE s.id = @Id;
        """;

    private const string _cancelSubstitutionSql = """
        UPDATE teaching.teaching_substitutions
        SET status = 'cancelled',
            handover_notes = CASE
                WHEN @Reason IS NOT NULL AND @Reason <> ''
                THEN ISNULL(handover_notes + CHAR(13) + CHAR(10), '') + '[Cancellation Reason]: ' + @Reason
                ELSE handover_notes
            END,
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private const string _resolvePendingConflictsSql = """
        UPDATE teaching.capacity_conflicts
        SET status = 'resolved',
            resolution_action = 'SUBSTITUTION_CANCELLED',
            resolved_by = @UpdatedBy,
            resolved_date = @ResolvedDate,
            resolution_notes = 'Conflict resolved because the substitution arrangement was cancelled.',
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
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

    public async Task<Result<CancelSubstitutionResponse>> HandleAsync(
        Guid id,
        CancelSubstitutionRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result.Failure<CancelSubstitutionResponse>(DomainError.Validation(
                "Teaching.InvalidSubstitutionId",
                "Substitution ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        if (connection is not SqlConnection sqlConnection)
        {
            return Result.Failure<CancelSubstitutionResponse>(DomainError.Failure(
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
                return Result.Failure<CancelSubstitutionResponse>(DomainError.NotFound(
                    "Teaching.SubstitutionNotFound",
                    $"Substitution with ID '{id}' was not found."));
            }

            if (sub.Status == "cancelled")
            {
                await tx.RollbackAsync(cancellationToken);
                return Result.Failure<CancelSubstitutionResponse>(DomainError.Conflict(
                    "Teaching.SubstitutionAlreadyCancelled",
                    $"Substitution '{sub.SubstitutionNumber}' is already cancelled."));
            }

            if (sub.Status == "completed")
            {
                await tx.RollbackAsync(cancellationToken);
                return Result.Failure<CancelSubstitutionResponse>(DomainError.Conflict(
                    "Teaching.SubstitutionAlreadyCompleted",
                    $"Cannot cancel substitution '{sub.SubstitutionNumber}' because it is already completed."));
            }

            var auditUser = currentUser.Username ?? "coordinator";
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var reason = request?.Reason?.Trim();

            // 1. Cancel substitution
            await connection.ExecuteAsync(new CommandDefinition(
                _cancelSubstitutionSql,
                new
                {
                    Id = id,
                    Reason = reason,
                    UpdatedBy = auditUser
                },
                transaction: tx,
                cancellationToken: cancellationToken));

            // 2. Resolve any associated pending blocking conflicts
            await connection.ExecuteAsync(new CommandDefinition(
                _resolvePendingConflictsSql,
                new
                {
                    OriginalAssignmentId = sub.OriginalAssignmentId,
                    SubstituteTeacherId = sub.SubstituteTeacherId,
                    UpdatedBy = auditUser,
                    ResolvedDate = today
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
                    EventType = "SUBSTITUTION_CANCELLED",
                    EntityType = "teaching_substitution",
                    EntityId = id,
                    EffectiveDate = today,
                    ActorRole = "Teaching Coordinator",
                    ActorId = auditUser,
                    EventSummary = $"Emergency substitution {sub.SubstitutionNumber} cancelled.",
                    ChangePayload = JsonSerializer.Serialize(new
                    {
                        SubstitutionId = id,
                        SubstitutionNumber = sub.SubstitutionNumber,
                        PreviousStatus = sub.Status,
                        NewStatus = "cancelled",
                        Reason = reason
                    }),
                    Notes = reason,
                    CreatedBy = auditUser
                },
                transaction: tx,
                cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);

            return Result.Success(new CancelSubstitutionResponse(
                Id: id,
                SubstitutionNumber: sub.SubstitutionNumber,
                Status: "cancelled",
                CancelledAt: DateTimeOffset.UtcNow));
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
        string Status);
}
