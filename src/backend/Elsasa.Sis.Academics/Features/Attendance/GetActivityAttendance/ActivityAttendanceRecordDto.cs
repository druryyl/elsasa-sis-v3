namespace Elsasa.Sis.Academics.Features.Attendance.GetActivityAttendance;

using Elsasa.Sis.Academics.Features.Attendance.Common;

/// <summary>
/// Roster item DTO representing a student's attendance record and full historical correction audit trail (P4-S09, TD-04).
/// </summary>
public sealed record ActivityAttendanceRecordDto(
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
    string? UpdatedBy,
    IReadOnlyList<AttendanceCorrectionDto> Corrections);
