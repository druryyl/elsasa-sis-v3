namespace Elsasa.Sis.Teaching.Features.QualificationEvidence.VerifyQualificationEvidence;

/// <summary>
/// Response payload returned after successfully verifying qualification evidence (P3-S10).
/// </summary>
/// <param name="Id">The unique identifier of the verified qualification evidence.</param>
/// <param name="TeacherProfileId">The unique identifier of the owning teacher profile.</param>
/// <param name="Title">The descriptive title of the credential.</param>
/// <param name="VerificationState">The resulting verification state ('verified').</param>
/// <param name="VerifiedBy">The user identity who verified the credential.</param>
/// <param name="VerifiedDate">The verification date.</param>
/// <param name="VerificationNotes">Optional notes recorded during verification.</param>
public sealed record VerifyQualificationEvidenceResponse(
    Guid Id,
    Guid TeacherProfileId,
    string Title,
    string VerificationState,
    string VerifiedBy,
    DateOnly VerifiedDate,
    string? VerificationNotes);
