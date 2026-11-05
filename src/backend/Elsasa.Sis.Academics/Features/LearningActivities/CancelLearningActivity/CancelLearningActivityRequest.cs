namespace Elsasa.Sis.Academics.Features.LearningActivities.CancelLearningActivity;

/// <summary>
/// Request payload for cancelling a scheduled learning activity (P3-S07).
/// </summary>
public sealed record CancelLearningActivityRequest(string CancellationReason);
