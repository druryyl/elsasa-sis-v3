namespace Elsasa.Sis.Academics.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

/// <summary>
/// Physical filesystem implementation of <see cref="ISubmissionStorageService"/> for coursework uploads (GAP-006, OQ-004, TD-07, P5-S13).
/// </summary>
public sealed class SubmissionStorageService(IOptions<SubmissionStorageOptions>? options = null) : ISubmissionStorageService
{
    private readonly SubmissionStorageOptions _options = options?.Value ?? new SubmissionStorageOptions();

    /// <inheritdoc />
    public async Task<string> SaveSubmissionFileAsync(
        IFormFile file,
        Guid studentId,
        string safeFileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(safeFileName);

        var year = DateTime.UtcNow.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var configuredRoot = string.IsNullOrWhiteSpace(_options.StorageRootPath)
            ? "App_Data/Submissions"
            : _options.StorageRootPath.Trim().Replace('\\', '/').TrimEnd('/');

        var relativeDir = $"{configuredRoot}/{year}/{studentId}";

        var baseDir = Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(AppContext.BaseDirectory, configuredRoot);

        var fullTargetDir = Path.GetFullPath(Path.Combine(baseDir, year, studentId.ToString()));
        Directory.CreateDirectory(fullTargetDir);

        var destinationPath = Path.Combine(fullTargetDir, safeFileName);

        await using (var fileStream = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true))
        {
            await file.CopyToAsync(fileStream, cancellationToken);
        }

        return $"{relativeDir}/{safeFileName}".Replace('\\', '/');
    }

    /// <inheritdoc />
    public string? ResolvePhysicalPath(string submissionReference)
    {
        if (string.IsNullOrWhiteSpace(submissionReference))
        {
            return null;
        }

        var normalized = submissionReference.Replace('\\', '/').TrimStart('/');
        if (normalized.Contains("..", StringComparison.Ordinal))
        {
            return null;
        }

        var fullPath = Path.IsPathRooted(normalized)
            ? Path.GetFullPath(normalized)
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, normalized));

        return File.Exists(fullPath) ? fullPath : null;
    }
}
