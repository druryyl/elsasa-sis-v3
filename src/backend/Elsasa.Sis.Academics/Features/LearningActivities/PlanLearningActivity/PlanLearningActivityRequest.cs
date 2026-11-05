namespace Elsasa.Sis.Academics.Features.LearningActivities.PlanLearningActivity;

/// <summary>
/// Request payload for planning an instructional learning activity or homeroom session (UC-ACD-02, TD-03).
/// </summary>
public sealed record PlanLearningActivityRequest(
    Guid LearningUnitId,
    string ActivityType,
    string Title,
    string? Description = null,
    string? SectionLabel = null,
    DateOnly ActivityDate = default,
    TimeOnly? StartTime = null,
    TimeOnly? EndTime = null,
    Guid? TeacherAssignmentId = null,
    string? TeacherName = null,
    bool IsAttendanceRequired = true);
