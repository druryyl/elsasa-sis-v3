namespace Elsasa.Sis.Teaching.Features.QualificationEvidence.RejectQualificationEvidence;

/// <summary>
/// Response payload returned after successfully recording qualification evidence rejection (P3-S10).
/// </summary>
/// <param name="Id">The unique identifier of the qualification evidence.</param>
/// <param name="TeacherProfileId">The unique identifier of the owning teacher profile.</param>
/// <param name="Title">The descriptive title of the credential.</param>
/// <param name="VerificationState">The resulting verification state ('rejected').</param>
/// <param name="VerifiedBy">The user identity who recorded the rejection.</param>
/// <param name="VerifiedDate">The date when rejection was recorded.</param>
/// <param name="RejectionReason">The documented rejection reason.</param>
public sealed record RejectQualificationEvidenceResponse(
    Guid Id,
    Guid TeacherProfileId,
    string Title,
    string VerificationState,
    string VerifiedBy,
    DateOnly VerifiedDate,
    string RejectionReason);
