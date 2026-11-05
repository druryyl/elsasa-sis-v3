namespace Elsasa.Sis.Academics.Features.AssessmentEvidence.RecordEvidence;

/// <summary>
/// Request payload for recording raw assessment evidence (ACD-WF-08, ACD-WF-20, UC-ACD-07, GAP-07, P3-S06).
/// </summary>
public sealed record RecordEvidenceRequest(
    Guid? StudentId,
    Guid? ApplicantId,
    Guid? SubmissionId,
    string EvidenceType,
    DateOnly CollectedDate,
    string? EvidenceReference,
    string? Notes);
