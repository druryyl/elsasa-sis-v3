namespace Elsasa.Sis.Students.Features.Admission.AssessmentEvidence.AddAssessmentEvidence;

/// <summary>
/// Request payload for adding external diagnostic entrance assessment evidence (ADR-004, ADR-005).
/// </summary>
/// <param name="AssessmentType">Category or name of assessment (e.g. reading_readiness, math_readiness).</param>
/// <param name="Score">Diagnostic score or outcome string (max 50 chars).</param>
/// <param name="Source">Issuing or conducting center (default "External Assessment Center").</param>
/// <param name="AssessmentDate">Optional date the assessment was administered.</param>
/// <param name="Notes">Optional evaluative or diagnostic notes.</param>
public sealed record AddAssessmentEvidenceRequest(
    string AssessmentType,
    string? Score,
    string? Source,
    DateOnly? AssessmentDate,
    string? Notes);
