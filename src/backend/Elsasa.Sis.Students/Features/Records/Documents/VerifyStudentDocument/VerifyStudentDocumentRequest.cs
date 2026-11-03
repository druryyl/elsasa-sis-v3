namespace Elsasa.Sis.Students.Features.Records.Documents.VerifyStudentDocument;

/// <summary>
/// Request payload for verifying or rejecting an official student document (ADR-004, ADR-005, P6-S09).
/// </summary>
public sealed record VerifyStudentDocumentRequest(
    string SubmissionStatus,
    string? Notes);
