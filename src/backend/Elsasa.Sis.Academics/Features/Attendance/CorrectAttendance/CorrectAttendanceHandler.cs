namespace Elsasa.Sis.Academics.Features.Attendance.CorrectAttendance;

using Dapper;
using Elsasa.Sis.Academics.Features.Attendance.Common;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler executing auditable historical attendance corrections (P4-S09, GAP-005, OQ-003, TD-04, UC-ACD-15).
/// Locks the targeted record with UPDLOCK/ROWLOCK, verifies status mutation, updates attendance state,
/// appends an immutable correction audit log entry, and emits post-commit <see cref="AttendanceCorrectedEvent"/>.
/// </summary>
public sealed class CorrectAttendanceHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher,
    TimeProvider? timeProvider = null)
{
    static CorrectAttendanceHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectRecordForUpdateSql = """
        SELECT
            id AS Id,
            learning_activity_id AS LearningActivityId,
            student_id AS StudentId,
            status AS Status,
            recorded_by AS RecordedBy,
            recorded_date AS RecordedDate,
            evidence AS Evidence,
            notes AS Notes,
            created_at AS CreatedAt,
            created_by AS CreatedBy
        FROM academics.attendance_records WITH (UPDLOCK, ROWLOCK)
        WHERE id = @Id;
        """;

    private const string _updateAttendanceRecordSql = """
        UPDATE academics.attendance_records
        SET status = @NewStatus,
            evidence = @Evidence,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private const string _insertAttendanceCorrectionSql = """
        INSERT INTO academics.attendance_corrections (
            id,
            attendance_record_id,
            previous_status,
            new_status,
            reason,
            corrected_by,
            corrected_date,
            evidence,
            created_at
        )
        VALUES (
            @Id,
            @AttendanceRecordId,
            @PreviousStatus,
            @NewStatus,
            @Reason,
            @CorrectedBy,
            @CorrectedDate,
            @Evidence,
            @CreatedAt
        );
        """;

    private sealed record AttendanceRecordRow(
        Guid Id,
        Guid LearningActivityId,
        Guid StudentId,
        string Status,
        string RecordedBy,
        DateOnly RecordedDate,
        string? Evidence,
        string? Notes,
        DateTimeOffset CreatedAt,
        string CreatedBy);

    /// <summary>
    /// Executes the auditable correction of an attendance record.
    /// </summary>
    /// <param name="id">Attendance record ID.</param>
    /// <param name="request">Correction payload containing new status, justification reason, and optional evidence.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated attendance record and its audit entry, or a domain error.</returns>
    public async Task<Result<CorrectAttendanceResponse>> HandleAsync(
        Guid id,
        CorrectAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var provider = timeProvider ?? TimeProvider.System;
        var now = provider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.DateTime);
        var correctedBy = currentUser.Username ?? currentUser.Email ?? "system";
        var normalizedNewStatus = request.NewStatus.Trim().ToLowerInvariant();

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // 1. Fetch current attendance record locking with UPDLOCK, ROWLOCK
        var current = await connection.QuerySingleOrDefaultAsync<AttendanceRecordRow>(new CommandDefinition(
            _selectRecordForUpdateSql,
            new { Id = id },
            transaction: transaction,
            cancellationToken: cancellationToken));

        if (current is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<CorrectAttendanceResponse>(
                DomainError.NotFound(
                    "Academic.AttendanceRecordNotFound",
                    $"Attendance record with ID '{id}' was not found."));
        }

        // 2. Reject if new status equals current status
        if (string.Equals(current.Status, normalizedNewStatus, StringComparison.OrdinalIgnoreCase))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<CorrectAttendanceResponse>(
                DomainError.Validation(
                    "Academic.SameAttendanceStatus",
                    "New status must differ from current status."));
        }

        var effectiveEvidence = string.IsNullOrWhiteSpace(request.Evidence)
            ? current.Evidence
            : request.Evidence.Trim();

        // 3. Update attendance_records
        await connection.ExecuteAsync(new CommandDefinition(
            _updateAttendanceRecordSql,
            new
            {
                Id = id,
                NewStatus = normalizedNewStatus,
                Evidence = effectiveEvidence,
                UpdatedAt = now,
                UpdatedBy = correctedBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 4. Insert immutable row into academics.attendance_corrections
        var correctionId = Guid.NewGuid();
        var trimmedReason = request.Reason.Trim();
        var trimmedEvidence = string.IsNullOrWhiteSpace(request.Evidence) ? null : request.Evidence.Trim();

        await connection.ExecuteAsync(new CommandDefinition(
            _insertAttendanceCorrectionSql,
            new
            {
                Id = correctionId,
                AttendanceRecordId = id,
                PreviousStatus = current.Status,
                NewStatus = normalizedNewStatus,
                Reason = trimmedReason,
                CorrectedBy = correctedBy,
                CorrectedDate = today,
                Evidence = trimmedEvidence,
                CreatedAt = now
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        // 5. Publish AttendanceCorrectedEvent post-commit (ADR-003, TD-08)
        await publisher.Publish(
            new AttendanceCorrectedEvent(
                current.Id,
                current.LearningActivityId,
                current.StudentId,
                current.Status,
                normalizedNewStatus,
                trimmedReason,
                correctedBy,
                now),
            cancellationToken);

        var correctionDto = new AttendanceCorrectionDto(
            correctionId,
            current.Id,
            current.Status,
            normalizedNewStatus,
            trimmedReason,
            correctedBy,
            today,
            trimmedEvidence,
            now);

        var response = new CorrectAttendanceResponse(
            current.Id,
            current.LearningActivityId,
            current.StudentId,
            normalizedNewStatus,
            current.RecordedBy,
            current.RecordedDate,
            effectiveEvidence,
            current.Notes,
            current.CreatedAt,
            current.CreatedBy,
            now,
            correctedBy,
            correctionDto);

        return Result.Success(response);
    }
}
