namespace Elsasa.Sis.Students.Features.Admission.AssessmentEvidence.UpdateAssessmentEvidence;

/// <summary>
/// Request payload for updating entrance assessment evidence (ADR-004, ADR-005).
/// </summary>
/// <param name="AssessmentType">Category or name of assessment.</param>
/// <param name="Score">Diagnostic score or outcome string (max 50 chars).</param>
/// <param name="Source">Issuing or conducting center.</param>
/// <param name="AssessmentDate">Optional date the assessment was administered.</param>
/// <param name="Notes">Optional evaluative or diagnostic notes.</param>
public sealed record UpdateAssessmentEvidenceRequest(
    string AssessmentType,
    string? Score,
    string? Source,
    DateOnly? AssessmentDate,
    string? Notes);
