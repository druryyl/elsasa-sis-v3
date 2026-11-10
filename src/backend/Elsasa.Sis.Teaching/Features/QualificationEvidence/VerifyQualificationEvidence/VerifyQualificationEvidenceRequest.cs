namespace Elsasa.Sis.Teaching.Features.QualificationEvidence.VerifyQualificationEvidence;

/// <summary>
/// Request payload for verifying qualification documentary evidence (P3-S10).
/// </summary>
/// <param name="VerificationNotes">Optional verification notes or comments from the verifier.</param>
public sealed record VerifyQualificationEvidenceRequest(
    string? VerificationNotes = null);
