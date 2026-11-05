namespace Elsasa.Sis.Academics.Services;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Helper service interface for persisting and resolving coursework submission files (GAP-006, OQ-004, TD-07, P5-S13).
/// </summary>
public interface ISubmissionStorageService
{
    /// <summary>
    /// Persists an uploaded coursework submission file to physical storage under App_Data/Submissions/{Year}/{StudentId}/.
    /// </summary>
    /// <param name="file">The uploaded file.</param>
    /// <param name="studentId">The student ID associated with the submission.</param>
    /// <param name="safeFileName">The sanitized unique file name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The normalized relative storage path (e.g. App_Data/Submissions/2026/{studentId}/{safeFileName}).</returns>
    Task<string> SaveSubmissionFileAsync(
        IFormFile file,
        Guid studentId,
        string safeFileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the absolute physical file path on disk for a given submission storage reference.
    /// </summary>
    /// <param name="submissionReference">The relative submission storage reference or path.</param>
    /// <returns>The absolute filesystem path if valid and exists; otherwise null.</returns>
    string? ResolvePhysicalPath(string submissionReference);
}
