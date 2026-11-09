namespace Elsasa.Sis.Academics.Features.Assessments.UpdateAssessment;

/// <summary>
/// Request payload for updating a planned assessment (UC-ACD-05, P2-S04).
/// </summary>
public sealed record UpdateAssessmentRequest(
    string Title,
    string? Criteria,
    decimal? MaxScore,
    decimal? PassingScore,
    string? ReassessmentPolicy,
    DateOnly? AssessmentDate,
    string? AssessorReference,
    string? Notes);
