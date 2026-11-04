namespace Elsasa.Sis.Students.Features.Records.Status.ChangeStudentStatus;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing authoritative operational student status changes,
/// closing active status rows, inserting new status periods, synchronizing student status,
/// and appending audit history entries (GAP-005, TD-05).
/// </summary>
public sealed class ChangeStudentStatusHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectStudentSql = """
        SELECT id
        FROM students.students
        WHERE id = @Id;
        """;

    private const string _closeActiveStatusSql = """
        UPDATE students.student_statuses
        SET is_current = 0,
            end_date = @EffectiveDate,
            updated_at = SYSUTCDATETIME(),
            updated_by = @User
        WHERE student_id = @StudentId AND is_current = 1;
        """;

    private const string _insertStatusSql = """
        INSERT INTO students.student_statuses
        (
            id,
            student_id,
            status,
            effective_date,
            end_date,
            is_current,
            reason,
            authorized_by,
            decision_reference,
            created_at,
            created_by
        )
        VALUES
        (
            @Id,
            @StudentId,
            @Status,
            @EffectiveDate,
            NULL,
            1,
            @Reason,
            @AuthorizedBy,
            @DecisionReference,
            SYSUTCDATETIME(),
            @User
        );
        """;

    private const string _syncStudentStatusSql = """
        UPDATE students.students
        SET current_status = @Status,
            updated_at = SYSUTCDATETIME(),
            updated_by = @User
        WHERE id = @StudentId;
        """;

    private const string _insertHistorySql = """
        INSERT INTO students.administrative_history_entries
        (
            id,
            student_id,
            event_type,
            event_date,
            detail,
            authorized_by,
            decision_reference,
            is_correction,
            corrects_entry_id,
            created_at,
            created_by
        )
        VALUES
        (
            @Id,
            @StudentId,
            @EventType,
            @EventDate,
            @Detail,
            @AuthorizedBy,
            @DecisionReference,
            0,
            NULL,
            SYSUTCDATETIME(),
            @User
        );
        """;

    /// <summary>
    /// Executes operational status transition for the given student.
    /// </summary>
    /// <param name="studentId">The student unique identifier.</param>
    /// <param name="request">Status change request payload.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A Result indicating success with ChangeStudentStatusResponse or domain error.</returns>
    public async Task<Result<ChangeStudentStatusResponse>> HandleAsync(
        Guid studentId,
        ChangeStudentStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify student existence
        var studentExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            _selectStudentSql,
            new { Id = studentId },
            cancellationToken: cancellationToken));

        if (!studentExists)
        {
            return Result.Failure<ChangeStudentStatusResponse>(DomainError.NotFound(
                "Students.StudentNotFound",
                $"Student with ID '{studentId}' was not found."));
        }

        var normalizedStatus = request.Status.Trim().ToLowerInvariant();
        var user = currentUser.Username ?? "system";
        var statusId = Guid.NewGuid();
        var historyId = Guid.NewGuid();

        var normalizedReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        var authorizedBy = string.IsNullOrWhiteSpace(request.AuthorizedBy) ? null : request.AuthorizedBy.Trim();
        var decisionRef = string.IsNullOrWhiteSpace(request.DecisionReference) ? null : request.DecisionReference.Trim();

        var eventType = normalizedStatus == "active" ? "student_activated" : "status_adjusted";
        var detail = $"Status adjusted to '{normalizedStatus}'. Reason: {normalizedReason}";

        // 2. Open atomic SqlTransaction
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            // 2.1 Close existing active status row
            await connection.ExecuteAsync(new CommandDefinition(
                _closeActiveStatusSql,
                new
                {
                    StudentId = studentId,
                    EffectiveDate = request.EffectiveDate.ToDateTime(TimeOnly.MinValue),
                    User = user
                },
                transaction: tx,
                cancellationToken: cancellationToken));

            // 2.2 Insert new active status row
            await connection.ExecuteAsync(new CommandDefinition(
                _insertStatusSql,
                new
                {
                    Id = statusId,
                    StudentId = studentId,
                    Status = normalizedStatus,
                    EffectiveDate = request.EffectiveDate.ToDateTime(TimeOnly.MinValue),
                    Reason = normalizedReason,
                    AuthorizedBy = authorizedBy,
                    DecisionReference = decisionRef,
                    User = user
                },
                transaction: tx,
                cancellationToken: cancellationToken));

            // 2.3 Synchronize denormalized student column
            await connection.ExecuteAsync(new CommandDefinition(
                _syncStudentStatusSql,
                new
                {
                    StudentId = studentId,
                    Status = normalizedStatus,
                    User = user
                },
                transaction: tx,
                cancellationToken: cancellationToken));

            // 2.4 Append history audit entry
            await connection.ExecuteAsync(new CommandDefinition(
                _insertHistorySql,
                new
                {
                    Id = historyId,
                    StudentId = studentId,
                    EventType = eventType,
                    EventDate = request.EffectiveDate.ToDateTime(TimeOnly.MinValue),
                    Detail = detail,
                    AuthorizedBy = authorizedBy,
                    DecisionReference = decisionRef,
                    User = user
                },
                transaction: tx,
                cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }

        return Result.Success(new ChangeStudentStatusResponse(
            StatusId: statusId,
            StudentId: studentId,
            Status: normalizedStatus,
            IsCurrent: true));
    }
}
