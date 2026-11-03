namespace Elsasa.Sis.Students.Features.Records.Documents.UploadStudentDocument;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Elsasa.Sis.Shared.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;

/// <summary>
/// Handles uploading and registering an official certified student document (ADR-004, ADR-005, P6-S09).
/// </summary>
public sealed class UploadStudentDocumentHandler(
    ISqlConnectionFactory connectionFactory,
    IFileStorageService fileStorageService,
    ICurrentUser currentUser)
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png"
    };

    public async Task<Result<UploadStudentDocumentResponse>> HandleAsync(
        Guid studentId,
        string? documentType,
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        if (studentId == Guid.Empty)
        {
            return DomainError.Validation("Students.InvalidStudentId", "Student ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(documentType))
        {
            return DomainError.Validation("Students.DocumentTypeRequired", "Document type is required.");
        }

        var normalizedDocType = documentType.Trim();
        if (normalizedDocType.Length > 100)
        {
            return DomainError.Validation("Students.DocumentTypeTooLong", "Document type must not exceed 100 characters.");
        }

        if (file is null || file.Length == 0)
        {
            return DomainError.Validation("Students.FileRequired", "Uploaded file cannot be empty.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return DomainError.Validation("Students.FileTooLarge", "The uploaded file exceeds the maximum allowed size of 5 MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !_allowedExtensions.Contains(extension))
        {
            return DomainError.Validation("Students.InvalidFileType", "File type is not supported. Allowed formats are: .pdf, .jpg, .jpeg, .png.");
        }

        await using var connection = (SqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify student exists
        const string checkStudentSql = """
            SELECT id
            FROM students.students
            WHERE id = @StudentId;
            """;

        var student = await connection.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(checkStudentSql, new { StudentId = studentId }, cancellationToken: cancellationToken));

        if (student is null)
        {
            return DomainError.NotFound("Students.StudentNotFound", $"Student with ID '{studentId}' was not found.");
        }

        // 2. Persist file via IFileStorageService under category students/{studentId}
        var category = $"students/{studentId}";
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

        // 3. Insert record into students.student_documents with submission_status = 'submitted'
        var documentId = Guid.NewGuid();
        var actor = currentUser.Username ?? "system";
        var submittedDate = DateOnly.FromDateTime(DateTime.UtcNow);

        const string insertDocSql = """
            INSERT INTO students.student_documents (
                id,
                student_id,
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
                @StudentId,
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
                StudentId = studentId,
                DocumentType = normalizedDocType,
                FileReference = storedFile.FileReference,
                FileName = storedFile.FileName,
                FileSizeBytes = storedFile.SizeBytes,
                ContentType = storedFile.ContentType,
                CreatedBy = actor
            },
            cancellationToken: cancellationToken));

        return new UploadStudentDocumentResponse(
            documentId,
            studentId,
            normalizedDocType,
            "submitted",
            storedFile.FileReference,
            storedFile.FileName,
            storedFile.SizeBytes,
            storedFile.ContentType,
            submittedDate);
    }
}
