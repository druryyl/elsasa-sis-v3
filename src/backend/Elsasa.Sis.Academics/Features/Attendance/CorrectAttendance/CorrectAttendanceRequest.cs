namespace Elsasa.Sis.Academics.Features.Attendance.CorrectAttendance;

/// <summary>
/// Request contract for auditable attendance corrections (P4-S09, UC-ACD-15, TD-04).
/// </summary>
/// <param name="NewStatus">New attendance status literal ('present', 'absent', 'late', 'excused_absence', 'partial').</param>
/// <param name="Reason">Mandatory justification explaining the historical attendance correction (min 5 characters).</param>
/// <param name="Evidence">Optional URL, document reference, or supporting justification text.</param>
public sealed record CorrectAttendanceRequest(
    string NewStatus,
    string Reason,
    string? Evidence = null);
