namespace Elsasa.Sis.Teaching.Features.QualificationEvidence.SubmitQualificationEvidence;

/// <summary>
/// Response record returned after successfully submitting qualification evidence (P3-S09).
/// </summary>
/// <param name="Id">The unique identifier of the created qualification evidence record.</param>
/// <param name="TeacherProfileId">The unique identifier of the owning teacher profile.</param>
/// <param name="QualificationTypeId">The recognized qualification type identifier.</param>
/// <param name="Title">The title of the submitted credential.</param>
/// <param name="VerificationState">The initial verification lifecycle state ('submitted').</param>
public sealed record SubmitQualificationEvidenceResponse(
    Guid Id,
    Guid TeacherProfileId,
    Guid QualificationTypeId,
    string Title,
    string VerificationState);
