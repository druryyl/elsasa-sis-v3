namespace Elsasa.Sis.Academics.Features.LearningActivities.GetActivitiesByUnit;

/// <summary>
/// DTO representing a learning activity item in a unit activity list (P3-S07).
/// </summary>
public sealed record LearningActivityItemDto(
    Guid Id,
    Guid LearningUnitId,
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
    string? UpdatedBy);
