namespace Elsasa.Sis.Academics.Features.Assignments.UploadSubmissionFile;

using Elsasa.Sis.Academics.Services;
using Elsasa.Sis.Shared.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

/// <summary>
/// Feature handler for validating and storing local coursework submission files (GAP-006, OQ-004, TD-07, P5-S11, P5-S13).
/// </summary>
public sealed class UploadSubmissionFileHandler
{
    public const long DefaultMaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

    private readonly ICurrentUser _currentUser;
    private readonly ISubmissionStorageService _storageService;
    private readonly SubmissionStorageOptions _options;

    public UploadSubmissionFileHandler(
        ICurrentUser currentUser,
        ISubmissionStorageService? storageService = null,
        IOptions<SubmissionStorageOptions>? options = null)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _options = options?.Value ?? new SubmissionStorageOptions();
        _storageService = storageService ?? new SubmissionStorageService(options);
    }

    private static readonly HashSet<string> _privilegedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Teacher",
        "AcademicCoordinator",
        "Academic Coordinator",
        "AcademicAuthority",
        "Academic Authority",
        "Admin",
        "Administrator"
    };

    private static readonly Dictionary<string, string[]> _allowedExtensionsAndMimes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = ["application/pdf"],
        [".jpg"] = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".png"] = ["image/png"],
        [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"]
    };

    public async Task<Result<UploadSubmissionFileResponse>> HandleAsync(
        IFormFile? file,
        Guid? studentId = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate file presence
        if (file is null || file.Length == 0)
        {
            return Result.Failure<UploadSubmissionFileResponse>(
                DomainError.Validation(
                    "Academic.SubmissionFileRequired",
                    "A valid non-empty file must be uploaded."));
        }

        // 2. Validate file size (max 25 MB or configured limit)
        var maxSizeBytes = _options.MaxFileSizeBytes > 0 ? _options.MaxFileSizeBytes : DefaultMaxFileSizeBytes;
        if (file.Length > maxSizeBytes)
        {
            return Result.Failure<UploadSubmissionFileResponse>(
                DomainError.Validation(
                    "Academic.SubmissionFileTooLarge",
                    $"The uploaded file exceeds the maximum allowed size of {maxSizeBytes / (1024 * 1024)} MB."));
        }

        // 3. Validate extension and MIME content type
        var rawFileName = file.FileName ?? string.Empty;
        var cleanFileName = Path.GetFileName(rawFileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(cleanFileName) || cleanFileName.Contains("..", StringComparison.Ordinal))
        {
            return Result.Failure<UploadSubmissionFileResponse>(
                DomainError.Validation(
                    "Academic.InvalidFileName",
                    "File name contains invalid path traversal sequences."));
        }

        var extension = Path.GetExtension(cleanFileName);
        if (string.IsNullOrEmpty(extension) || !_allowedExtensionsAndMimes.TryGetValue(extension, out var allowedMimes))
        {
            return Result.Failure<UploadSubmissionFileResponse>(
                DomainError.Validation(
                    "Academic.InvalidSubmissionFileType",
                    "File format is not supported. Allowed extensions: .pdf, .jpg, .jpeg, .png, .docx."));
        }

        var mediaType = string.IsNullOrWhiteSpace(file.ContentType)
            ? string.Empty
            : file.ContentType.Split(';')[0].Trim();

        if (!allowedMimes.Any(mime => string.Equals(mime, mediaType, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure<UploadSubmissionFileResponse>(
                DomainError.Validation(
                    "Academic.InvalidSubmissionFileType",
                    "MIME content type is not supported. Allowed formats: PDF, JPEG, PNG, DOCX."));
        }

        // 4. Resolve and authorize student identifier
        var effectiveStudentId = studentId ?? _currentUser.StudentId ?? _currentUser.Id;
        if (!effectiveStudentId.HasValue || effectiveStudentId.Value == Guid.Empty)
        {
            return Result.Failure<UploadSubmissionFileResponse>(
                DomainError.Validation(
                    "Academic.StudentIdRequired",
                    "Student ID could not be determined for the submission file."));
        }

        var isPrivileged = _currentUser.Roles.Any(r => _privilegedRoles.Contains(r));
        if (!isPrivileged && _currentUser.StudentId.HasValue && _currentUser.StudentId.Value != effectiveStudentId.Value)
        {
            return Result.Failure<UploadSubmissionFileResponse>(
                DomainError.Forbidden(
                    "Academic.UnauthorizedStudentUpload",
                    "Students can only upload coursework files on their own behalf."));
        }

        // 5. Generate sanitized safe unique filename
        var baseName = Path.GetFileNameWithoutExtension(cleanFileName);
        var safeChars = baseName.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray();
        var safeBase = safeChars.Length > 0 ? new string(safeChars) : "submission";
        if (safeBase.Length > 50)
        {
            safeBase = safeBase[..50];
        }

        var safeFileName = $"{safeBase}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

        // 6. Save via submission storage helper service (App_Data/Submissions/{Year}/{StudentId}/)
        var storagePath = await _storageService.SaveSubmissionFileAsync(
            file,
            effectiveStudentId.Value,
            safeFileName,
            cancellationToken);

        var response = new UploadSubmissionFileResponse(
            storagePath,
            safeFileName,
            file.Length,
            mediaType);

        return Result.Success(response);
    }
}
