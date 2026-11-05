namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Strongly-typed contract DTO representing an aggregate attendance summary for a student within an academic period.
/// </summary>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="AcademicPeriodId">The unique identifier of the academic period.</param>
/// <param name="TotalSessions">Total number of scheduled or conducted sessions for which attendance was recorded.</param>
/// <param name="TotalPresent">Total count of sessions recorded as present.</param>
/// <param name="TotalAbsent">Total count of sessions recorded as absent.</param>
/// <param name="TotalLate">Total count of sessions recorded as late.</param>
/// <param name="TotalExcused">Total count of sessions recorded as excused absence.</param>
/// <param name="AttendanceRatePercentage">Computed attendance rate percentage.</param>
public sealed record StudentAttendanceSummaryDto(
    Guid StudentId,
    Guid AcademicPeriodId,
    int TotalSessions,
    int TotalPresent,
    int TotalAbsent,
    int TotalLate,
    int TotalExcused,
    decimal AttendanceRatePercentage);
