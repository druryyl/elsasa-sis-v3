namespace Elsasa.Sis.Students.Features.Admission.Documents.VerifyApplicantDocument;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Data.SqlClient;

/// <summary>
/// Handles updating the verification state and audit trail of an applicant document.
/// </summary>
public sealed class VerifyApplicantDocumentHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    public async Task<Result<VerifyApplicantDocumentResponse>> HandleAsync(
        Guid applicationId,
        Guid docId,
        VerifyApplicantDocumentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (applicationId == Guid.Empty || docId == Guid.Empty)
        {
            return DomainError.NotFound("Admission.DocumentNotFound", "Document or application identifier is invalid.");
        }

        var normalizedStatus = request.SubmissionStatus?.Trim().ToLowerInvariant();
        if (normalizedStatus != "verified" && normalizedStatus != "rejected")
        {
            return DomainError.Validation("Admission.InvalidSubmissionStatus", "Submission status must be either 'verified' or 'rejected'.");
        }

        await using var connection = (SqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Check application exists and is not withdrawn
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
            return DomainError.Conflict("Admission.ApplicationWithdrawn", "Cannot verify documents for a withdrawn admission application.");
        }

        // 2. Check document exists
        const string checkDocSql = """
            SELECT id, application_id, document_type, submission_status, file_reference
            FROM students.applicant_documents
            WHERE id = @DocId
              AND application_id = @ApplicationId;
            """;

        var document = await connection.QuerySingleOrDefaultAsync<(Guid Id, Guid ApplicationId, string DocumentType, string SubmissionStatus, string? FileReference)?>(
            new CommandDefinition(checkDocSql, new { DocId = docId, ApplicationId = applicationId }, cancellationToken: cancellationToken));

        if (document is null)
        {
            return DomainError.NotFound("Admission.DocumentNotFound", $"Applicant document with ID '{docId}' for application '{applicationId}' was not found.");
        }

        var verifier = currentUser.Username ?? "system";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        const string updateSql = """
            UPDATE students.applicant_documents
            SET submission_status = @SubmissionStatus,
                verified_by = @VerifiedBy,
                verified_date = CAST(GETUTCDATE() AS DATE),
                notes = @Notes,
                updated_at = SYSUTCDATETIME(),
                updated_by = @UpdatedBy
            WHERE id = @DocId
              AND application_id = @ApplicationId;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            updateSql,
            new
            {
                DocId = docId,
                ApplicationId = applicationId,
                SubmissionStatus = normalizedStatus,
                VerifiedBy = verifier,
                Notes = request.Notes,
                UpdatedBy = verifier
            },
            cancellationToken: cancellationToken));

        return new VerifyApplicantDocumentResponse(
            docId,
            applicationId,
            document.Value.DocumentType,
            normalizedStatus,
            today,
            verifier,
            request.Notes);
    }
}
