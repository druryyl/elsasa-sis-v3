namespace Elsasa.Sis.Shared.Storage;

using Elsasa.Sis.Shared.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>
/// Physical local filesystem implementation of <see cref="IFileStorageService"/>
/// providing path traversal protection, MIME/extension verification, and file size enforcement.
/// </summary>
public sealed class PhysicalFileStorageService : IFileStorageService
{
    /// <summary>
    /// Maximum allowed file upload size: 5 MB (5,242,880 bytes).
    /// </summary>
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string[]> _allowedExtensionsAndMimes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = ["application/pdf"],
        [".jpg"] = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".png"] = ["image/png"]
    };

    private readonly string _rootPath;
    private readonly string _normalizedRootWithSeparator;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicalFileStorageService"/> class using injected options.
    /// </summary>
    /// <param name="options">Configuration options specifying the local root path.</param>
    [ActivatorUtilitiesConstructor]
    public PhysicalFileStorageService(IOptions<FileStorageOptions> options)
        : this(options?.Value?.LocalRootPath)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicalFileStorageService"/> class using an explicit root path.
    /// </summary>
    /// <param name="localRootPath">The directory path on the local filesystem to use as the storage root.</param>
    public PhysicalFileStorageService(string? localRootPath = null)
    {
        var rawPath = string.IsNullOrWhiteSpace(localRootPath) ? "App_Data/storage" : localRootPath;
        _rootPath = Path.IsPathRooted(rawPath)
            ? Path.GetFullPath(rawPath)
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, rawPath));

        _normalizedRootWithSeparator = _rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        try
        {
            Directory.CreateDirectory(_rootPath);
        }
        catch
        {
            // Best effort directory creation on startup; actual I/O will fail predictably if inaccessible.
        }
    }

    /// <inheritdoc />
    public async Task<Result<StoredFileResult>> SaveFileAsync(
        Stream stream,
        string fileName,
        string contentType,
        string category,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return DomainError.Validation("Storage.InvalidFileName", "File name cannot be empty.");
        }

        // 1. Sanitize file name and reject path traversal
        var cleanFileName = Path.GetFileName(fileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(cleanFileName) || cleanFileName.Contains("..", StringComparison.Ordinal))
        {
            return DomainError.Validation("Storage.InvalidFileName", "File name contains invalid path traversal sequences.");
        }

        // 2. Validate category path traversal
        var normalizedCategory = (category ?? string.Empty).Replace('\\', '/').Trim('/', ' ');
        if (normalizedCategory.Split('/', StringSplitOptions.RemoveEmptyEntries).Any(segment => segment == ".."))
        {
            return DomainError.Validation("Storage.InvalidCategory", "Category path contains invalid traversal sequences.");
        }

        if (normalizedCategory.StartsWith("storage/", StringComparison.OrdinalIgnoreCase))
        {
            normalizedCategory = normalizedCategory[8..];
        }

        // 3. Whitelist validation: extensions and MIME types
        var extension = Path.GetExtension(cleanFileName);
        if (string.IsNullOrEmpty(extension) || !_allowedExtensionsAndMimes.TryGetValue(extension, out var allowedMimes))
        {
            return DomainError.Validation(
                "Storage.InvalidContentType",
                "File format or MIME type is not supported. Allowed extensions: .pdf, .jpg, .jpeg, .png.");
        }

        var mediaType = string.IsNullOrWhiteSpace(contentType) ? string.Empty : contentType.Split(';')[0].Trim();
        if (!allowedMimes.Any(mime => string.Equals(mime, mediaType, StringComparison.OrdinalIgnoreCase)))
        {
            return DomainError.Validation(
                "Storage.InvalidContentType",
                "File format or MIME type is not supported. Allowed formats: PDF (.pdf), JPEG (.jpg, .jpeg), PNG (.png).");
        }

        // 4. Validate file size (seekable fast-check)
        if (stream.CanSeek && stream.Length > MaxFileSizeBytes)
        {
            return DomainError.Validation("Storage.FileTooLarge", "File size exceeds the 5 MB limit.");
        }

        // 5. Construct target directory and verify root boundary
        var targetDir = string.IsNullOrEmpty(normalizedCategory)
            ? _rootPath
            : Path.GetFullPath(Path.Combine(_rootPath, normalizedCategory));

        if (!IsSubpath(targetDir))
        {
            return DomainError.Validation("Storage.InvalidCategory", "Category path resolves outside the storage root.");
        }

        Directory.CreateDirectory(targetDir);

        // 6. Generate unique stored file name
        var baseName = Path.GetFileNameWithoutExtension(cleanFileName);
        var uniqueFileName = $"{baseName}_{Guid.NewGuid()}{extension}";
        var destinationPath = Path.Combine(targetDir, uniqueFileName);

        // 7. Write stream to physical file while enforcing size limit
        long totalBytesWritten = 0;
        try
        {
            await using (var destinationStream = new FileStream(
                destinationPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                var buffer = new byte[81920];
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false)) > 0)
                {
                    totalBytesWritten += bytesRead;
                    if (totalBytesWritten > MaxFileSizeBytes)
                    {
                        await destinationStream.DisposeAsync().ConfigureAwait(false);
                        TryDeleteFile(destinationPath);
                        return DomainError.Validation("Storage.FileTooLarge", "File size exceeds the 5 MB limit.");
                    }

                    await destinationStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct).ConfigureAwait(false);
                }

                await destinationStream.FlushAsync(ct).ConfigureAwait(false);
            }

            var fileReference = string.IsNullOrEmpty(normalizedCategory)
                ? $"storage/{uniqueFileName}"
                : $"storage/{normalizedCategory}/{uniqueFileName}";

            return Result.Success(new StoredFileResult(
                fileReference,
                cleanFileName,
                totalBytesWritten,
                mediaType));
        }
        catch (OperationCanceledException)
        {
            TryDeleteFile(destinationPath);
            throw;
        }
        catch (Exception ex)
        {
            TryDeleteFile(destinationPath);
            return DomainError.Failure("Storage.SaveFailed", $"Failed to save physical file: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Task<Result<FileStreamResult>> OpenReadStreamAsync(
        string fileReference,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileReference))
        {
            return Task.FromResult<Result<FileStreamResult>>(
                DomainError.Validation("Storage.InvalidFileReference", "File reference cannot be empty."));
        }

        var fullPath = ResolveSafePath(fileReference);
        if (fullPath is null)
        {
            return Task.FromResult<Result<FileStreamResult>>(
                DomainError.Validation("Storage.InvalidFileReference", "Invalid file reference or directory traversal sequence detected."));
        }

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Result<FileStreamResult>>(
                DomainError.NotFound("Storage.FileNotFound", $"The file '{Path.GetFileName(fileReference)}' was not found."));
        }

        try
        {
            var fileStream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            var extension = Path.GetExtension(fullPath);
            var contentType = _allowedExtensionsAndMimes.TryGetValue(extension, out var mimes)
                ? mimes[0]
                : "application/octet-stream";

            var fileName = Path.GetFileName(fullPath);

            return Task.FromResult(Result.Success(new FileStreamResult(fileStream, contentType, fileName)));
        }
        catch (Exception ex)
        {
            return Task.FromResult<Result<FileStreamResult>>(
                DomainError.Failure("Storage.ReadFailed", $"Failed to open file stream: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<Result> DeleteFileAsync(
        string fileReference,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileReference))
        {
            return Task.FromResult<Result>(
                DomainError.Validation("Storage.InvalidFileReference", "File reference cannot be empty."));
        }

        var fullPath = ResolveSafePath(fileReference);
        if (fullPath is null)
        {
            return Task.FromResult<Result>(
                DomainError.Validation("Storage.InvalidFileReference", "Invalid file reference or directory traversal sequence detected."));
        }

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Result>(
                DomainError.NotFound("Storage.FileNotFound", $"The file '{Path.GetFileName(fileReference)}' was not found."));
        }

        try
        {
            File.Delete(fullPath);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult<Result>(
                DomainError.Failure("Storage.DeleteFailed", $"Failed to delete physical file: {ex.Message}"));
        }
    }

    private string? ResolveSafePath(string fileReference)
    {
        var normalized = fileReference.Replace('\\', '/').TrimStart('/');
        if (normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).Any(segment => segment == ".."))
        {
            return null;
        }

        if (normalized.StartsWith("storage/", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[8..];
        }

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, normalized));
        return IsSubpath(fullPath) ? fullPath : null;
    }

    private bool IsSubpath(string path)
    {
        var normalizedPath = Path.GetFullPath(path);
        return normalizedPath.StartsWith(_normalizedRootWithSeparator, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalizedPath, _rootPath, StringComparison.OrdinalIgnoreCase);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }
}
