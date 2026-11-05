namespace Elsasa.Sis.Academics.Features.Attendance.RecordSessionAttendance;

using Dapper;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler executing fast roll-call recording for classroom teachers (P4-S08, GAP-005, OQ-003, TD-04, UC-ACD-03).
/// Enforces server-side same-day / 24-hour grace window rules, performs atomic upsert into <c>academics.attendance_records</c>,
/// and publishes post-commit aggregated domain events via MediatR <see cref="IPublisher"/>.
/// </summary>
public sealed class RecordSessionAttendanceHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher,
    TimeProvider? timeProvider = null)
{
    static RecordSessionAttendanceHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectActivitySql = """
        SELECT
            id AS Id,
            learning_unit_id AS LearningUnitId,
            activity_type AS ActivityType,
            title AS Title,
            section_label AS SectionLabel,
            activity_date AS ActivityDate,
            status AS Status,
            created_at AS CreatedAt
        FROM academics.learning_activities
        WHERE id = @Id;
        """;

    private const string _upsertAttendanceRecordSql = """
        MERGE academics.attendance_records WITH (HOLDLOCK) AS target
        USING (SELECT @LearningActivityId AS learning_activity_id, @StudentId AS student_id) AS source
        ON target.learning_activity_id = source.learning_activity_id
        AND target.student_id = source.student_id
        WHEN MATCHED THEN
            UPDATE SET
                status = @Status,
                recorded_by = @RecordedBy,
                recorded_date = @RecordedDate,
                evidence = @Evidence,
                notes = @Notes,
                updated_at = @UpdatedAt,
                updated_by = @UpdatedBy
        WHEN NOT MATCHED THEN
            INSERT (
                id,
                learning_activity_id,
                student_id,
                status,
                recorded_by,
                recorded_date,
                evidence,
                notes,
                created_at,
                created_by
            )
            VALUES (
                @Id,
                @LearningActivityId,
                @StudentId,
                @Status,
                @RecordedBy,
                @RecordedDate,
                @Evidence,
                @Notes,
                @CreatedAt,
                @CreatedBy
            );
        """;

    private const string _selectAggregatedSummarySql = """
        SELECT
            COUNT(1) AS TotalCount,
            ISNULL(SUM(CASE WHEN status = 'present' THEN 1 ELSE 0 END), 0) AS PresentCount,
            ISNULL(SUM(CASE WHEN status = 'absent' THEN 1 ELSE 0 END), 0) AS AbsentCount,
            ISNULL(SUM(CASE WHEN status = 'late' THEN 1 ELSE 0 END), 0) AS LateCount,
            ISNULL(SUM(CASE WHEN status = 'excused_absence' THEN 1 ELSE 0 END), 0) AS ExcusedCount,
            ISNULL(SUM(CASE WHEN status = 'partial' THEN 1 ELSE 0 END), 0) AS PartialCount
        FROM academics.attendance_records
        WHERE learning_activity_id = @Id;
        """;

    private sealed record ActivityRow(
        Guid Id,
        Guid LearningUnitId,
        string ActivityType,
        string Title,
        string? SectionLabel,
        DateOnly ActivityDate,
        string Status,
        DateTimeOffset CreatedAt);

    private sealed record AggregatedSummaryRow(
        int TotalCount,
        int PresentCount,
        int AbsentCount,
        int LateCount,
        int ExcusedCount,
        int PartialCount);

    /// <summary>
    /// Executes fast attendance roll-call recording with 24-hour grace window enforcement.
    /// </summary>
    /// <param name="id">Learning activity ID.</param>
    /// <param name="request">Attendance entries request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary of recorded counts, or a domain error.</returns>
    public async Task<Result<RecordSessionAttendanceResponse>> HandleAsync(
        Guid id,
        RecordSessionAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Entries is null || request.Entries.Count == 0)
        {
            return Result.Failure<RecordSessionAttendanceResponse>(
                DomainError.Validation(
                    "Academic.EmptyAttendanceEntries",
                    "At least one attendance entry must be provided."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify learning activity exists
        var activity = await connection.QuerySingleOrDefaultAsync<ActivityRow>(new CommandDefinition(
            _selectActivitySql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (activity is null)
        {
            return Result.Failure<RecordSessionAttendanceResponse>(
                DomainError.NotFound(
                    "Academic.LearningActivityNotFound",
                    $"Learning activity with ID '{id}' was not found."));
        }

        // 2. Verify learning activity is not cancelled
        if (string.Equals(activity.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<RecordSessionAttendanceResponse>(
                DomainError.Conflict(
                    "Academic.LearningActivityCancelled",
                    "Cannot record attendance for a cancelled learning activity."));
        }

        // 3. Grace Window Check: evaluates server-side whether activity.activity_date >= today
        //    OR (SYSUTCDATETIME() - activity.created_at <= 24 hours)
        var provider = timeProvider ?? TimeProvider.System;
        var now = provider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var isWithinGraceWindow = AttendanceGraceWindow.IsWithinWindow(activity.ActivityDate, activity.CreatedAt, now);

        if (!isWithinGraceWindow)
        {
            return Result.Failure<RecordSessionAttendanceResponse>(
                DomainError.Conflict(
                    "Academic.AttendanceGraceWindowExpired",
                    "The 24-hour grace window for direct attendance recording has expired. Use the auditable correction endpoint."));
        }

        // 4. Inside a SQL transaction, execute atomic upserts
        var recordedBy = currentUser.Username ?? currentUser.Email ?? "system";
        AggregatedSummaryRow summary;

        await using (var transaction = await connection.BeginTransactionAsync(cancellationToken))
        {
            foreach (var entry in request.Entries)
            {
                var recordId = Guid.NewGuid();
                await connection.ExecuteAsync(new CommandDefinition(
                    _upsertAttendanceRecordSql,
                    new
                    {
                        Id = recordId,
                        LearningActivityId = id,
                        StudentId = entry.StudentId,
                        Status = entry.Status.Trim().ToLowerInvariant(),
                        RecordedBy = recordedBy,
                        RecordedDate = today,
                        Evidence = string.IsNullOrWhiteSpace(entry.Evidence) ? null : entry.Evidence.Trim(),
                        Notes = string.IsNullOrWhiteSpace(entry.Notes) ? null : entry.Notes.Trim(),
                        CreatedAt = now,
                        CreatedBy = recordedBy,
                        UpdatedAt = now,
                        UpdatedBy = recordedBy
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
            }

            summary = await connection.QuerySingleAsync<AggregatedSummaryRow>(new CommandDefinition(
                _selectAggregatedSummarySql,
                new { Id = id },
                transaction: transaction,
                cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
        }

        // 5. Publish post-commit domain event via MediatR IPublisher (ADR-003, TD-08)
        await publisher.Publish(
            new AttendanceRecordedEvent(
                activity.Id,
                today,
                summary.PresentCount,
                summary.AbsentCount,
                summary.LateCount,
                summary.ExcusedCount,
                recordedBy,
                now),
            cancellationToken);

        var response = new RecordSessionAttendanceResponse(
            activity.Id,
            summary.TotalCount,
            summary.PresentCount,
            summary.AbsentCount,
            summary.LateCount,
            summary.ExcusedCount,
            summary.PartialCount,
            today,
            recordedBy);

        return Result.Success(response);
    }
}
