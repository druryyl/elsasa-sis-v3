namespace Elsasa.Sis.Students.Features.Records.Documents.GetStudentDocuments;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Elsasa.Sis.Students.Features.Records.GetStudentById;

/// <summary>
/// Feature handler querying official student documents by student ID (ADR-004, ADR-005, P4-S06).
/// </summary>
public sealed class GetStudentDocumentsHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _checkStudentSql = """
        SELECT COUNT(1)
        FROM students.students
        WHERE id = @StudentId;
        """;

    private const string _selectDocumentsSql = """
        SELECT
            doc.id AS Id,
            doc.document_type AS DocumentType,
            doc.submission_status AS SubmissionStatus,
            doc.submitted_date AS SubmittedDate,
            doc.verified_date AS VerifiedDate,
            doc.verified_by AS VerifiedBy,
            doc.file_name AS FileName,
            doc.file_size_bytes AS FileSizeBytes,
            doc.content_type AS ContentType,
            doc.file_reference AS FileReference
        FROM students.student_documents doc
        WHERE doc.student_id = @StudentId
        ORDER BY doc.created_at ASC;
        """;

    /// <summary>
    /// Retrieves the list of official student documents for a given student ID.
    /// </summary>
    public async Task<Result<IReadOnlyList<StudentDocumentDto>>> HandleAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var exists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _checkStudentSql,
            new { StudentId = studentId },
            cancellationToken: cancellationToken));

        if (exists == 0)
        {
            return Result.Failure<IReadOnlyList<StudentDocumentDto>>(
                DomainError.NotFound(
                    "Students.Records.StudentNotFound",
                    $"Student with ID '{studentId}' was not found."));
        }

        var documents = await connection.QueryAsync<StudentDocumentDto>(new CommandDefinition(
            _selectDocumentsSql,
            new { StudentId = studentId },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<StudentDocumentDto>>(documents.AsList());
    }
}
