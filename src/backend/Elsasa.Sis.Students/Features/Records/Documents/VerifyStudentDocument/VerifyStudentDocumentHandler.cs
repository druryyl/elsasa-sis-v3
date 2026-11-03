namespace Elsasa.Sis.Students.Features.Records.Documents.VerifyStudentDocument;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Data.SqlClient;

/// <summary>
/// Handles updating the verification state and audit trail of an official student document (ADR-004, ADR-005, P6-S09).
/// </summary>
public sealed class VerifyStudentDocumentHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    public async Task<Result<VerifyStudentDocumentResponse>> HandleAsync(
        Guid studentId,
        Guid docId,
        VerifyStudentDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (studentId == Guid.Empty || docId == Guid.Empty)
        {
            return DomainError.NotFound("Students.DocumentNotFound", "Document or student identifier is invalid.");
        }

        var normalizedStatus = request.SubmissionStatus?.Trim().ToLowerInvariant();
        if (normalizedStatus != "verified" && normalizedStatus != "rejected")
        {
            return DomainError.Validation("Students.InvalidSubmissionStatus", "Submission status must be either 'verified' or 'rejected'.");
        }

        await using var connection = (SqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify student document exists
        const string checkDocSql = """
            SELECT id, student_id, document_type, submission_status
            FROM students.student_documents
            WHERE id = @DocId
              AND student_id = @StudentId;
            """;

        var document = await connection.QuerySingleOrDefaultAsync<(Guid Id, Guid StudentId, string DocumentType, string SubmissionStatus)?>(
            new CommandDefinition(checkDocSql, new { DocId = docId, StudentId = studentId }, cancellationToken: cancellationToken));

        if (document is null)
        {
            return DomainError.NotFound("Students.DocumentNotFound", $"Student document with ID '{docId}' for student '{studentId}' was not found.");
        }

        var verifier = currentUser.Username ?? "system";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 2. Update verification status, verifier identity, and verified date
        const string updateSql = """
            UPDATE students.student_documents
            SET submission_status = @SubmissionStatus,
                verified_by = @VerifiedBy,
                verified_date = CAST(SYSUTCDATETIME() AS DATE),
                notes = @Notes,
                updated_at = SYSUTCDATETIME(),
                updated_by = @UpdatedBy
            WHERE id = @DocId
              AND student_id = @StudentId;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            updateSql,
            new
            {
                DocId = docId,
                StudentId = studentId,
                SubmissionStatus = normalizedStatus,
                VerifiedBy = verifier,
                Notes = request.Notes,
                UpdatedBy = verifier
            },
            cancellationToken: cancellationToken));

        return new VerifyStudentDocumentResponse(
            docId,
            studentId,
            document.Value.DocumentType,
            normalizedStatus,
            today,
            verifier,
            request.Notes);
    }
}
