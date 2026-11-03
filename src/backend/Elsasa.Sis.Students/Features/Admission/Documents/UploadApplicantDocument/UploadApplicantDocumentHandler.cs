namespace Elsasa.Sis.Students.Features.Admission.Documents.UploadApplicantDocument;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Elsasa.Sis.Shared.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;

/// <summary>
/// Handles uploading and registering an applicant credential document.
/// </summary>
public sealed class UploadApplicantDocumentHandler(
    ISqlConnectionFactory connectionFactory,
    IFileStorageService fileStorageService,
    ICurrentUser currentUser)
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png"
    };

    public async Task<Result<UploadApplicantDocumentResponse>> HandleAsync(
        Guid applicationId,
        string? documentType,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (applicationId == Guid.Empty)
        {
            return DomainError.Validation("Admission.InvalidApplicationId", "Application ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(documentType))
        {
            return DomainError.Validation("Admission.DocumentTypeRequired", "Document type is required.");
        }

        var normalizedDocType = documentType.Trim();
        if (normalizedDocType.Length > 100)
        {
            return DomainError.Validation("Admission.DocumentTypeTooLong", "Document type must not exceed 100 characters.");
        }

        if (file is null || file.Length == 0)
        {
            return DomainError.Validation("Admission.FileRequired", "Uploaded file cannot be empty.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return DomainError.Validation("Admission.FileTooLarge", "The uploaded file exceeds the maximum allowed size of 5 MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !_allowedExtensions.Contains(extension))
        {
            return DomainError.Validation("Admission.InvalidFileType", "File type is not supported. Allowed formats are: .pdf, .jpg, .jpeg, .png.");
        }

        await using var connection = (SqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify application exists and is not withdrawn
        const string checkAppSql = """
            SELECT id, status
            FROM students.admission_applications
            WHERE id = @ApplicationId;
            """;

        var application = await connection.QuerySingleOrDefaultAsync<(Guid Id, string Status)?>(
            new CommandDefinition(checkAppSql, new { ApplicationId = applicationId }, cancellationToken: cancellationToken));

        if (application is null)
        {
            return DomainError.NotFound("Admission.ApplicationNotFound", $"Admission application with ID '{applicationId}' was not found.");
        }

        if (string.Equals(application.Value.Status, "withdrawn", StringComparison.OrdinalIgnoreCase))
        {
            return DomainError.Conflict("Admission.ApplicationWithdrawn", "Cannot upload documents for a withdrawn admission application.");
        }

        // 2. Save file to storage
        var category = $"documents/admission/{applicationId}";
        await using var stream = file.OpenReadStream();
        var saveResult = await fileStorageService.SaveFileAsync(
            stream,
            file.FileName,
            file.ContentType,
            category,
            cancellationToken);

        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        var storedFile = saveResult.Value;

        // 3. Check if document record already exists for (application_id, document_type)
        const string checkDocSql = """
            SELECT id, file_reference
            FROM students.applicant_documents
            WHERE application_id = @ApplicationId
              AND document_type = @DocumentType;
            """;

        var existingDoc = await connection.QuerySingleOrDefaultAsync<(Guid Id, string? FileReference)?>(
            new CommandDefinition(checkDocSql, new { ApplicationId = applicationId, DocumentType = normalizedDocType }, cancellationToken: cancellationToken));

        var actor = currentUser.Username ?? "applicant";
        Guid documentId;
        var submittedDate = DateOnly.FromDateTime(DateTime.UtcNow);

        if (existingDoc is not null)
        {
            documentId = existingDoc.Value.Id;
            const string updateDocSql = """
                UPDATE students.applicant_documents
                SET submission_status = 'submitted',
                    file_reference = @FileReference,
                    file_name = @FileName,
                    file_size_bytes = @FileSizeBytes,
                    content_type = @ContentType,
                    submitted_date = CAST(GETUTCDATE() AS DATE),
                    updated_at = SYSUTCDATETIME(),
                    updated_by = @UpdatedBy
                WHERE id = @Id;
                """;

            await connection.ExecuteAsync(new CommandDefinition(
                updateDocSql,
                new
                {
                    Id = documentId,
                    FileReference = storedFile.FileReference,
                    FileName = storedFile.FileName,
                    FileSizeBytes = storedFile.SizeBytes,
                    ContentType = storedFile.ContentType,
                    UpdatedBy = actor
                },
                cancellationToken: cancellationToken));

            // Best-effort cleanup of previous file if different
            if (!string.IsNullOrWhiteSpace(existingDoc.Value.FileReference) &&
                !string.Equals(existingDoc.Value.FileReference, storedFile.FileReference, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await fileStorageService.DeleteFileAsync(existingDoc.Value.FileReference, cancellationToken);
                }
                catch
                {
                    // Non-fatal if previous file cleanup fails
                }
            }
        }
        else
        {
            documentId = Guid.NewGuid();
            const string insertDocSql = """
                INSERT INTO students.applicant_documents (
                    id,
                    application_id,
                    document_type,
                    submission_status,
                    submitted_date,
                    file_reference,
                    file_name,
                    file_size_bytes,
                    content_type,
                    created_at,
                    created_by
                ) VALUES (
                    @Id,
                    @ApplicationId,
                    @DocumentType,
                    'submitted',
                    CAST(GETUTCDATE() AS DATE),
                    @FileReference,
                    @FileName,
                    @FileSizeBytes,
                    @ContentType,
                    SYSUTCDATETIME(),
                    @CreatedBy
                );
                """;

            await connection.ExecuteAsync(new CommandDefinition(
                insertDocSql,
                new
                {
                    Id = documentId,
                    ApplicationId = applicationId,
                    DocumentType = normalizedDocType,
                    FileReference = storedFile.FileReference,
                    FileName = storedFile.FileName,
                    FileSizeBytes = storedFile.SizeBytes,
                    ContentType = storedFile.ContentType,
                    CreatedBy = actor
                },
                cancellationToken: cancellationToken));
        }

        return new UploadApplicantDocumentResponse(
            documentId,
            applicationId,
            normalizedDocType,
            "submitted",
            storedFile.FileReference,
            storedFile.FileName,
            storedFile.SizeBytes,
            storedFile.ContentType,
            submittedDate);
    }
}
