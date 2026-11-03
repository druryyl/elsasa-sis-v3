namespace Elsasa.Sis.Students.Features.Admission.AssessmentEvidence.AddAssessmentEvidence;

/// <summary>
/// Response payload returned after creating entrance assessment evidence (ADR-004, ADR-005).
/// </summary>
/// <param name="Id">The unique identifier of the created evidence record.</param>
/// <param name="ApplicationId">The parent admission application identifier.</param>
/// <param name="AssessmentType">Category or name of assessment.</param>
/// <param name="Score">Diagnostic score or outcome string.</param>
/// <param name="Source">Issuing or conducting center.</param>
/// <param name="AssessmentDate">Optional date the assessment was administered.</param>
/// <param name="Notes">Optional evaluative or diagnostic notes.</param>
/// <param name="CreatedAt">Audit creation timestamp in UTC.</param>
public sealed record AddAssessmentEvidenceResponse(
    Guid Id,
    Guid ApplicationId,
    string AssessmentType,
    string? Score,
    string Source,
    DateOnly? AssessmentDate,
    string? Notes,
    DateTimeOffset CreatedAt);
