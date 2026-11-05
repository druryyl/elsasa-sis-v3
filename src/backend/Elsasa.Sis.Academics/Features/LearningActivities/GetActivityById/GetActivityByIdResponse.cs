namespace Elsasa.Sis.Academics.Features.LearningActivities.GetActivityById;

/// <summary>
/// Summary counts of attendance records for a learning activity (P3-S07).
/// </summary>
public sealed record ActivityAttendanceSummary(
    int TotalCount,
    int PresentCount,
    int AbsentCount,
    int LateCount,
    int ExcusedCount,
    int PartialCount);

/// <summary>
/// Coursework assignment issued for a learning activity (P3-S07).
/// </summary>
public sealed record ActivityAssignmentItem(
    Guid Id,
    string Title,
    string? Description,
    DateOnly ReleaseDate,
    DateOnly? DueDate,
    decimal? MaxScore,
    decimal? WeightPercentage,
    string Status,
    DateTimeOffset CreatedAt);

/// <summary>
/// Detailed response for a single learning activity including teacher context, attendance summary, and issued assignments (P3-S07).
/// </summary>
public sealed record GetActivityByIdResponse(
    Guid Id,
    Guid LearningUnitId,
    string LearningUnitCode,
    string LearningUnitName,
    string ActivityType,
    string Title,
    string? Description,
    string? SectionLabel,
    DateOnly ActivityDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    Guid? TeacherAssignmentId,
    string? TeacherName,
    string Status,
    bool IsAttendanceRequired,
    string? DeliveryNotes,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy,
    ActivityAttendanceSummary AttendanceSummary,
    IReadOnlyList<ActivityAssignmentItem> Assignments);
