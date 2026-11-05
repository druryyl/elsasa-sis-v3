namespace Elsasa.Sis.Academics.Features.Attendance.GetActivityAttendance;

using Dapper;
using Elsasa.Sis.Academics.Features.Attendance.Common;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying session student attendance roster with current status and full audit correction history (P4-S09, TD-04, UC-ACD-15).
/// </summary>
public sealed class GetActivityAttendanceHandler(ISqlConnectionFactory connectionFactory)
{
    static GetActivityAttendanceHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkActivitySql = """
        SELECT COUNT(1)
        FROM academics.learning_activities
        WHERE id = @Id;
        """;

    private const string _selectAttendanceRecordsSql = """
        SELECT
            r.id AS Id,
            r.learning_activity_id AS LearningActivityId,
            r.student_id AS StudentId,
            s.student_number AS StudentNumber,
            s.full_name AS StudentName,
            r.status AS Status,
            r.recorded_by AS RecordedBy,
            r.recorded_date AS RecordedDate,
            r.evidence AS Evidence,
            r.notes AS Notes,
            r.created_at AS CreatedAt,
            r.created_by AS CreatedBy,
            r.updated_at AS UpdatedAt,
            r.updated_by AS UpdatedBy
        FROM academics.attendance_records r
        LEFT JOIN students.students s ON s.id = r.student_id
        WHERE r.learning_activity_id = @Id
        ORDER BY COALESCE(s.full_name, '') ASC, r.created_at ASC;
        """;

    private const string _selectCorrectionsSql = """
        SELECT
            c.id AS Id,
            c.attendance_record_id AS AttendanceRecordId,
            c.previous_status AS PreviousStatus,
            c.new_status AS NewStatus,
            c.reason AS Reason,
            c.corrected_by AS CorrectedBy,
            c.corrected_date AS CorrectedDate,
            c.evidence AS Evidence,
            c.created_at AS CreatedAt
        FROM academics.attendance_corrections c
        INNER JOIN academics.attendance_records r ON r.id = c.attendance_record_id
        WHERE r.learning_activity_id = @Id
        ORDER BY c.created_at ASC;
        """;

    private sealed record RawAttendanceRecordRow(
        Guid Id,
        Guid LearningActivityId,
        Guid StudentId,
        string? StudentNumber,
        string? StudentName,
        string Status,
        string RecordedBy,
        DateOnly RecordedDate,
        string? Evidence,
        string? Notes,
        DateTimeOffset CreatedAt,
        string CreatedBy,
        DateTimeOffset? UpdatedAt,
        string? UpdatedBy);

    /// <summary>
    /// Executes the query for session attendance records and correction histories.
    /// </summary>
    /// <param name="id">Learning activity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session attendance roster with full correction histories, or a domain error.</returns>
    public async Task<Result<IReadOnlyList<ActivityAttendanceRecordDto>>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var activityExists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _checkActivitySql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (activityExists == 0)
        {
            return Result.Failure<IReadOnlyList<ActivityAttendanceRecordDto>>(
                DomainError.NotFound(
                    "Academic.LearningActivityNotFound",
                    $"Learning activity with ID '{id}' was not found."));
        }

        var records = (await connection.QueryAsync<RawAttendanceRecordRow>(new CommandDefinition(
            _selectAttendanceRecordsSql,
            new { Id = id },
            cancellationToken: cancellationToken))).AsList();

        if (records.Count == 0)
        {
            return Result.Success<IReadOnlyList<ActivityAttendanceRecordDto>>([]);
        }

        var corrections = (await connection.QueryAsync<AttendanceCorrectionDto>(new CommandDefinition(
            _selectCorrectionsSql,
            new { Id = id },
            cancellationToken: cancellationToken))).AsList();

        var correctionsByRecordId = corrections
            .GroupBy(c => c.AttendanceRecordId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<AttendanceCorrectionDto>)g.ToList());

        var result = records.Select(r => new ActivityAttendanceRecordDto(
            r.Id,
            r.LearningActivityId,
            r.StudentId,
            r.StudentNumber,
            r.StudentName,
            r.Status,
            r.RecordedBy,
            r.RecordedDate,
            r.Evidence,
            r.Notes,
            r.CreatedAt,
            r.CreatedBy,
            r.UpdatedAt,
            r.UpdatedBy,
            correctionsByRecordId.TryGetValue(r.Id, out var corrList) ? corrList : []
        )).ToList();

        return Result.Success<IReadOnlyList<ActivityAttendanceRecordDto>>(result);
    }
}
