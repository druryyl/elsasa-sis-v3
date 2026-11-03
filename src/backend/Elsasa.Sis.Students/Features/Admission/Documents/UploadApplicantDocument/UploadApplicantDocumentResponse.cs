namespace Elsasa.Sis.Students.Features.Admission.Documents.UploadApplicantDocument;

/// <summary>
/// Response payload for applicant document upload.
/// </summary>
public sealed record UploadApplicantDocumentResponse(
    Guid Id,
    Guid ApplicationId,
    string DocumentType,
    string SubmissionStatus,
    string FileReference,
    string FileName,
    long FileSizeBytes,
    string ContentType,
    DateOnly? SubmittedDate);
