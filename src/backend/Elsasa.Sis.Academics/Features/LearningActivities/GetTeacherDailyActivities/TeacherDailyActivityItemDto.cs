namespace Elsasa.Sis.Academics.Features.LearningActivities.GetTeacherDailyActivities;

/// <summary>
/// DTO representing an activity in the teacher's daily schedule (SCR-ACD-01, P3-S07).
/// </summary>
public sealed record TeacherDailyActivityItemDto(
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
    string? DeliveryNotes);
