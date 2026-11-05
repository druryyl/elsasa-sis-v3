namespace Elsasa.Sis.Academics.Features.LearningActivities.ConductLearningActivity;

/// <summary>
/// Response returned upon successfully conducting a learning activity (UC-ACD-02, P3-S07).
/// </summary>
public sealed record ConductLearningActivityResponse(
    Guid Id,
    Guid LearningUnitId,
    string ActivityType,
    string Title,
    string? SectionLabel,
    DateOnly ActivityDate,
    string Status,
    string? DeliveryNotes,
    string? TeacherName,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
