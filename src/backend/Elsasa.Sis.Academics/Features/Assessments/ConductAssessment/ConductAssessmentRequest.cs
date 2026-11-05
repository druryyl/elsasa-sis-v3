namespace Elsasa.Sis.Academics.Features.Assessments.ConductAssessment;

/// <summary>
/// Optional request payload for conducting an assessment (UC-ACD-05, P2-S04).
/// </summary>
public sealed record ConductAssessmentRequest(
    DateOnly? AssessmentDate = null,
    string? Notes = null);
