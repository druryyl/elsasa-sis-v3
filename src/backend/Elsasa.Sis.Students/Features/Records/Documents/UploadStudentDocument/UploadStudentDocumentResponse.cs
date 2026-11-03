namespace Elsasa.Sis.Students.Features.Records.Documents.UploadStudentDocument;

/// <summary>
/// Response payload after successfully uploading an official student document (ADR-004, ADR-005, P6-S09).
/// </summary>
public sealed record UploadStudentDocumentResponse(
    Guid Id,
    Guid StudentId,
    string DocumentType,
    string SubmissionStatus,
    string? FileReference,
    string? FileName,
    long? FileSizeBytes,
    string? ContentType,
    DateOnly? SubmittedDate)
{
    /// <summary>
    /// Explicit alias for document identifier.
    /// </summary>
    public Guid DocumentId => Id;
}
