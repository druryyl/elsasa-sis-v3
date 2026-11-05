namespace Elsasa.Sis.Academics.Features.Attendance.RecordSessionAttendance;

/// <summary>
/// Domain logic evaluator for the teacher session attendance 24-hour grace window (GAP-005, OQ-003, TD-04, UC-ACD-03).
/// Direct attendance recording and same-day modifications are permitted when either:
/// 1. The activity date is today or in the future (<c>ActivityDate &gt;= today</c>), OR
/// 2. The activity was created within the last 24 hours (<c>now - CreatedAt &lt;= 24 hours</c>).
/// Modifications after the 24-hour grace window require the auditable correction workflow.
/// </summary>
public static class AttendanceGraceWindow
{
    /// <summary>
    /// Default grace duration for direct roll-call recording (24 hours).
    /// </summary>
    public static readonly TimeSpan GraceDuration = TimeSpan.FromHours(24);

    /// <summary>
    /// Evaluates whether direct attendance recording is permitted for an activity.
    /// </summary>
    /// <param name="activityDate">Scheduled date of the learning activity.</param>
    /// <param name="createdAt">Activity entity creation timestamp (UTC).</param>
    /// <param name="now">Current evaluation timestamp (UTC).</param>
    /// <returns><c>true</c> if within the permitted grace window; otherwise <c>false</c>.</returns>
    public static bool IsWithinWindow(DateOnly activityDate, DateTimeOffset createdAt, DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(now.DateTime);
        return activityDate >= today || (now - createdAt) <= GraceDuration;
    }
}
