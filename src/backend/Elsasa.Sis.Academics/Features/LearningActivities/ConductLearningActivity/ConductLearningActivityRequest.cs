namespace Elsasa.Sis.Academics.Features.LearningActivities.ConductLearningActivity;

/// <summary>
/// Request payload for conducting a scheduled learning activity and recording curriculum delivery notes (UC-ACD-02, TD-03, P3-S07).
/// </summary>
public sealed record ConductLearningActivityRequest(
    string? DeliveryNotes = null,
    string? ActualTeacherName = null);
