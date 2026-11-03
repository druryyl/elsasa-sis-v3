namespace Elsasa.Sis.Students.Features.Admission.Documents.VerifyApplicantDocument;

/// <summary>
/// Request payload for verifying or rejecting an applicant document.
/// </summary>
public sealed record VerifyApplicantDocumentRequest(
    string SubmissionStatus,
    string? Notes);
