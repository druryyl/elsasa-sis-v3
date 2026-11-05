namespace Elsasa.Sis.Academics.Features.Assessments.CancelAssessment;

/// <summary>
/// Request payload for cancelling a planned assessment (UC-ACD-05, P2-S04).
/// </summary>
public sealed record CancelAssessmentRequest(string Reason);
