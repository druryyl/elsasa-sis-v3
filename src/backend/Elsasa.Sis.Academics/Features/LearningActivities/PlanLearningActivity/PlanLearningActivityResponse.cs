namespace Elsasa.Sis.Academics.Features.LearningActivities.PlanLearningActivity;

/// <summary>
/// Response returned upon successfully planning a learning activity (P3-S07).
/// </summary>
public sealed record PlanLearningActivityResponse(
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
    string CreatedBy);
