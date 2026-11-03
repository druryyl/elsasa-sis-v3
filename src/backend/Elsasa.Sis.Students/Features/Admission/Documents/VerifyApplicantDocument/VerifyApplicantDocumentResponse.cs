namespace Elsasa.Sis.Students.Features.Admission.Documents.VerifyApplicantDocument;

/// <summary>
/// Response payload after applicant document verification state update.
/// </summary>
public sealed record VerifyApplicantDocumentResponse(
    Guid Id,
    Guid ApplicationId,
    string DocumentType,
    string SubmissionStatus,
    DateOnly? VerifiedDate,
    string? VerifiedBy,
    string? Notes);
