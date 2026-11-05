namespace Elsasa.Sis.Academics.Features.Attendance.GetStudentAttendanceSummary;

/// <summary>
/// Response payload containing aggregate attendance counts and calculated percentage for a student (P4-S09, TD-04).
/// </summary>
public sealed record GetStudentAttendanceSummaryResponse(
    Guid StudentId,
    Guid? AcademicPeriodId,
    int TotalSessions,
    int TotalPresent,
    int TotalAbsent,
    int TotalLate,
    int TotalExcused,
    int TotalPartial,
    decimal AttendanceRatePercentage);
