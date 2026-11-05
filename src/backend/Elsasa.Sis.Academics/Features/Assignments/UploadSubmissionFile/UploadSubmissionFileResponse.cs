namespace Elsasa.Sis.Academics.Features.Assignments.UploadSubmissionFile;

/// <summary>
/// Response contract containing metadata and relative storage path of an uploaded submission file (TD-07, P5-S11).
/// </summary>
/// <param name="StoragePath">The relative storage path to be stored in submission_reference.</param>
/// <param name="FileName">The unique safe file name on disk.</param>
/// <param name="FileSizeBytes">The file size in bytes.</param>
/// <param name="ContentType">The verified MIME content type.</param>
public sealed record UploadSubmissionFileResponse(
    string StoragePath,
    string FileName,
    long FileSizeBytes,
    string ContentType);
