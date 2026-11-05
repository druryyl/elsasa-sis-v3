namespace Elsasa.Sis.Academics.Features.Attendance.GetStudentAttendanceSummary;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler aggregating student session attendance totals and percentage across an academic period or overall (P4-S09, TD-04).
/// </summary>
public sealed class GetStudentAttendanceSummaryHandler(ISqlConnectionFactory connectionFactory)
{
    static GetStudentAttendanceSummaryHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAttendanceSummarySql = """
        SELECT
            COUNT(1) AS TotalSessions,
            ISNULL(SUM(CASE WHEN r.status = 'present' THEN 1 ELSE 0 END), 0) AS TotalPresent,
            ISNULL(SUM(CASE WHEN r.status = 'absent' THEN 1 ELSE 0 END), 0) AS TotalAbsent,
            ISNULL(SUM(CASE WHEN r.status = 'late' THEN 1 ELSE 0 END), 0) AS TotalLate,
            ISNULL(SUM(CASE WHEN r.status = 'excused_absence' THEN 1 ELSE 0 END), 0) AS TotalExcused,
            ISNULL(SUM(CASE WHEN r.status = 'partial' THEN 1 ELSE 0 END), 0) AS TotalPartial
        FROM academics.attendance_records r
        INNER JOIN academics.learning_activities a ON a.id = r.learning_activity_id
        INNER JOIN academics.learning_units u ON u.id = a.learning_unit_id
        INNER JOIN academics.learning_structures ls ON ls.id = u.learning_structure_id
        WHERE r.student_id = @StudentId
          AND (@AcademicPeriodId IS NULL OR ls.academic_period_id = @AcademicPeriodId OR EXISTS (
              SELECT 1 FROM academics.academic_participations p
              WHERE p.student_id = r.student_id
                AND p.learning_unit_id = a.learning_unit_id
                AND p.academic_period_id = @AcademicPeriodId
          ));
        """;

    private sealed record SummaryRow(
        int TotalSessions,
        int TotalPresent,
        int TotalAbsent,
        int TotalLate,
        int TotalExcused,
        int TotalPartial);

    /// <summary>
    /// Executes the aggregation query for student attendance totals.
    /// </summary>
    /// <param name="studentId">The student ID.</param>
    /// <param name="academicPeriodId">Optional academic period filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated student attendance summary.</returns>
    public async Task<Result<GetStudentAttendanceSummaryResponse>> HandleAsync(
        Guid studentId,
        Guid? academicPeriodId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<SummaryRow>(new CommandDefinition(
            _selectAttendanceSummarySql,
            new
            {
                StudentId = studentId,
                AcademicPeriodId = academicPeriodId
            },
            cancellationToken: cancellationToken));

        var totalSessions = row?.TotalSessions ?? 0;
        var totalPresent = row?.TotalPresent ?? 0;
        var totalAbsent = row?.TotalAbsent ?? 0;
        var totalLate = row?.TotalLate ?? 0;
        var totalExcused = row?.TotalExcused ?? 0;
        var totalPartial = row?.TotalPartial ?? 0;

        var attendanceRate = totalSessions > 0
            ? Math.Round((decimal)(totalPresent + totalLate) * 100m / totalSessions, 2)
            : 0.00m;

        var response = new GetStudentAttendanceSummaryResponse(
            studentId,
            academicPeriodId,
            totalSessions,
            totalPresent,
            totalAbsent,
            totalLate,
            totalExcused,
            totalPartial,
            attendanceRate);

        return Result.Success(response);
    }
}
