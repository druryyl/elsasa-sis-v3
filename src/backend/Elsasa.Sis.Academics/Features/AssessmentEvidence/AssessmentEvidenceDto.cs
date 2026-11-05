namespace Elsasa.Sis.Academics.Features.AssessmentEvidence;

/// <summary>
/// Data transfer object for assessment evidence (ACD-WF-08, ACD-WF-20, UC-ACD-07, GAP-07, P3-S06).
/// </summary>
public sealed record AssessmentEvidenceDto(
    Guid Id,
    Guid AssessmentId,
    Guid? StudentId,
    Guid? ApplicantId,
    Guid? SubmissionId,
    string EvidenceType,
    DateOnly CollectedDate,
    string? EvidenceReference,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
