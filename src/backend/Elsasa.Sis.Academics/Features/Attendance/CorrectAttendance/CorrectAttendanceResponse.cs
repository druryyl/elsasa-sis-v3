namespace Elsasa.Sis.Academics.Features.Attendance.CorrectAttendance;

using Elsasa.Sis.Academics.Features.Attendance.Common;

/// <summary>
/// Response returned upon successfully correcting an attendance record, containing updated details and audit entry (P4-S09, UC-ACD-15).
/// </summary>
public sealed record CorrectAttendanceResponse(
    Guid Id,
    Guid LearningActivityId,
    Guid StudentId,
    string Status,
    string RecordedBy,
    DateOnly RecordedDate,
    string? Evidence,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy,
    AttendanceCorrectionDto LatestCorrection);
