namespace Elsasa.Sis.Shared.Storage;

using Elsasa.Sis.Shared.Common;

/// <summary>
/// Defines file storage operations for saving, reading, and deleting physical credential documents.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves a file stream into physical disk storage under the specified category.
    /// </summary>
    /// <param name="stream">The readable file stream to save.</param>
    /// <param name="fileName">The original or sanitized file name with extension.</param>
    /// <param name="contentType">The MIME content type of the file.</param>
    /// <param name="category">The relative logical folder or categorization path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing metadata of the stored file, or a domain error.</returns>
    Task<Result<StoredFileResult>> SaveFileAsync(
        Stream stream,
        string fileName,
        string contentType,
        string category,
        CancellationToken ct = default);

    /// <summary>
    /// Opens a safe read-only stream for a file identified by its storage reference.
    /// </summary>
    /// <param name="fileReference">The storage reference or relative key of the file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the file stream and content metadata, or a domain error.</returns>
    Task<Result<FileStreamResult>> OpenReadStreamAsync(
        string fileReference,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a file identified by its storage reference from physical storage.
    /// </summary>
    /// <param name="fileReference">The storage reference or relative key of the file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> DeleteFileAsync(
        string fileReference,
        CancellationToken ct = default);
}

/// <summary>
/// Represents metadata of a successfully uploaded and persisted file.
/// </summary>
/// <param name="FileReference">The unique relative path or reference key of the stored file.</param>
/// <param name="FileName">The sanitized original file name.</param>
/// <param name="SizeBytes">The actual size of the stored file in bytes.</param>
/// <param name="ContentType">The verified MIME content type.</param>
public sealed record StoredFileResult(
    string FileReference,
    string FileName,
    long SizeBytes,
    string ContentType);

/// <summary>
/// Represents an opened file stream with MIME content type and file name for download/streaming.
/// </summary>
/// <param name="Stream">The readable file stream.</param>
/// <param name="ContentType">The MIME content type.</param>
/// <param name="FileName">The file name on disk.</param>
public sealed record FileStreamResult(
    Stream Stream,
    string ContentType,
    string FileName);
