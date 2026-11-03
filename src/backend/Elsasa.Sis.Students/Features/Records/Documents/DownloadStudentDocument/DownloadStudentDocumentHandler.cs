namespace Elsasa.Sis.Students.Features.Records.Documents.DownloadStudentDocument;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Elsasa.Sis.Shared.Storage;
using Microsoft.Data.SqlClient;

/// <summary>
/// Handles retrieving and opening a readable stream for a stored student document (ADR-004, ADR-005, P6-S09).
/// </summary>
public sealed class DownloadStudentDocumentHandler(
    ISqlConnectionFactory connectionFactory,
    IFileStorageService fileStorageService)
{
    public async Task<Result<FileStreamResult>> HandleAsync(
        Guid studentId,
        Guid docId,
        CancellationToken cancellationToken = default)
    {
        if (studentId == Guid.Empty || docId == Guid.Empty)
        {
            return DomainError.NotFound("Students.DocumentNotFound", "Document or student identifier is invalid.");
        }

        await using var connection = (SqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                id AS Id,
                student_id AS StudentId,
                document_type AS DocumentType,
                file_reference AS FileReference,
                file_name AS FileName,
                content_type AS ContentType
            FROM students.student_documents
            WHERE id = @DocId
              AND student_id = @StudentId;
            """;

        var doc = await connection.QuerySingleOrDefaultAsync<DocumentRecord>(
            new CommandDefinition(sql, new { DocId = docId, StudentId = studentId }, cancellationToken: cancellationToken));

        if (doc is null)
        {
            return DomainError.NotFound("Students.DocumentNotFound", $"Student document with ID '{docId}' for student '{studentId}' was not found.");
        }

        if (string.IsNullOrWhiteSpace(doc.FileReference))
        {
            return DomainError.NotFound("Students.FileNotFound", "No document file has been uploaded for this document record.");
        }

        return await fileStorageService.OpenReadStreamAsync(doc.FileReference, cancellationToken);
    }

    private sealed record DocumentRecord(
        Guid Id,
        Guid StudentId,
        string DocumentType,
        string? FileReference,
        string? FileName,
        string? ContentType);
}
