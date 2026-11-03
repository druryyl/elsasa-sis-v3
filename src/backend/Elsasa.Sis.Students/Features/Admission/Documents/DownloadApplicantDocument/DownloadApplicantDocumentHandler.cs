namespace Elsasa.Sis.Students.Features.Admission.Documents.DownloadApplicantDocument;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Elsasa.Sis.Shared.Storage;
using Microsoft.Data.SqlClient;

/// <summary>
/// Handles retrieving and opening a readable stream for a stored applicant document.
/// </summary>
public sealed class DownloadApplicantDocumentHandler(
    ISqlConnectionFactory connectionFactory,
    IFileStorageService fileStorageService)
{
    public async Task<Result<FileStreamResult>> HandleAsync(
        Guid applicationId,
        Guid docId,
        CancellationToken cancellationToken)
    {
        if (applicationId == Guid.Empty || docId == Guid.Empty)
        {
            return DomainError.NotFound("Admission.DocumentNotFound", "Document or application identifier is invalid.");
        }

        await using var connection = (SqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                id AS Id,
                application_id AS ApplicationId,
                document_type AS DocumentType,
                file_reference AS FileReference,
                file_name AS FileName,
                content_type AS ContentType
            FROM students.applicant_documents
            WHERE id = @DocId
              AND application_id = @ApplicationId;
            """;

        var doc = await connection.QuerySingleOrDefaultAsync<DocumentRecord>(
            new CommandDefinition(sql, new { DocId = docId, ApplicationId = applicationId }, cancellationToken: cancellationToken));

        if (doc is null)
        {
            return DomainError.NotFound("Admission.DocumentNotFound", $"Applicant document with ID '{docId}' for application '{applicationId}' was not found.");
        }

        if (string.IsNullOrWhiteSpace(doc.FileReference))
        {
            return DomainError.NotFound("Admission.FileNotFound", "No document file has been uploaded for this document record.");
        }

        return await fileStorageService.OpenReadStreamAsync(doc.FileReference, cancellationToken);
    }

    private sealed record DocumentRecord(
        Guid Id,
        Guid ApplicationId,
        string DocumentType,
        string? FileReference,
        string? FileName,
        string? ContentType);
}
