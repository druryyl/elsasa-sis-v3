namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Strongly-typed contract DTO representing attendance tallies frozen into a report card snapshot.
/// </summary>
/// <param name="PresentDays">Total days marked present.</param>
/// <param name="AbsentDays">Total unexcused absence days.</param>
/// <param name="LateDays">Total days marked late.</param>
/// <param name="ExcusedDays">Total excused absence days.</param>
public sealed record ReportCardAttendanceSummaryDto(
    int PresentDays,
    int AbsentDays,
    int LateDays,
    int ExcusedDays);
