namespace Elsasa.Sis.Teaching.Features.QualificationEvidence.RejectQualificationEvidence;

/// <summary>
/// Request payload for recording qualification documentary evidence rejection (P3-S10).
/// </summary>
/// <param name="RejectionReason">The mandatory reason explaining why the credential was rejected.</param>
public sealed record RejectQualificationEvidenceRequest(
    string RejectionReason);
