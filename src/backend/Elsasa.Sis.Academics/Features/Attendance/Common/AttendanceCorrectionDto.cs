namespace Elsasa.Sis.Academics.Features.Attendance.Common;

/// <summary>
/// Immutable audit log entry representing a historical attendance status correction (P4-S09, UC-ACD-15, TD-04).
/// </summary>
public sealed record AttendanceCorrectionDto(
    Guid Id,
    Guid AttendanceRecordId,
    string PreviousStatus,
    string NewStatus,
    string Reason,
    string CorrectedBy,
    DateOnly CorrectedDate,
    string? Evidence,
    DateTimeOffset CreatedAt);
