namespace Elsasa.Sis.Students.Features.Admission.AssessmentEvidence.UpdateAssessmentEvidence;

/// <summary>
/// Response payload returned after updating entrance assessment evidence (ADR-004, ADR-005).
/// </summary>
/// <param name="Id">The unique identifier of the evidence record.</param>
/// <param name="ApplicationId">The parent admission application identifier.</param>
/// <param name="AssessmentType">Category or name of assessment.</param>
/// <param name="Score">Diagnostic score or outcome string.</param>
/// <param name="Source">Issuing or conducting center.</param>
/// <param name="AssessmentDate">Optional date the assessment was administered.</param>
/// <param name="Notes">Optional evaluative or diagnostic notes.</param>
/// <param name="UpdatedAt">Audit update timestamp in UTC.</param>
/// <param name="UpdatedBy">Username of the user who performed the update.</param>
public sealed record UpdateAssessmentEvidenceResponse(
    Guid Id,
    Guid ApplicationId,
    string AssessmentType,
    string? Score,
    string Source,
    DateOnly? AssessmentDate,
    string? Notes,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
