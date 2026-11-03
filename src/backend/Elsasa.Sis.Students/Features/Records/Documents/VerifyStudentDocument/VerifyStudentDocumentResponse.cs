namespace Elsasa.Sis.Students.Features.Records.Documents.VerifyStudentDocument;

/// <summary>
/// Response payload after student document verification state update (ADR-004, ADR-005, P6-S09).
/// </summary>
public sealed record VerifyStudentDocumentResponse(
    Guid Id,
    Guid StudentId,
    string DocumentType,
    string SubmissionStatus,
    DateOnly? VerifiedDate,
    string? VerifiedBy,
    string? Notes);
