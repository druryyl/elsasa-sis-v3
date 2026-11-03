namespace Elsasa.Sis.Students.Features.Admission.AssessmentEvidence.DeleteAssessmentEvidence;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing deletion of entrance assessment evidence (ADR-004, ADR-005).
/// </summary>
public sealed class DeleteAssessmentEvidenceHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _selectApplicationStatusSql = """
        SELECT
            id AS Id,
            status AS Status
        FROM students.admission_applications
        WHERE id = @Id;
        """;

    private const string _selectEvidenceSql = """
        SELECT
            id AS Id
        FROM students.entrance_assessment_evidence
        WHERE id = @EvidenceId AND application_id = @ApplicationId;
        """;

    private const string _deleteEvidenceSql = """
        DELETE FROM students.entrance_assessment_evidence
        WHERE id = @EvidenceId AND application_id = @ApplicationId;
        """;

    /// <summary>
    /// Executes the removal of manual assessment evidence for an admission application.
    /// </summary>
    /// <param name="applicationId">The admission application ID.</param>
    /// <param name="evidenceId">The assessment evidence ID.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> HandleAsync(
        Guid applicationId,
        Guid evidenceId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var application = await connection.QuerySingleOrDefaultAsync<ApplicationStatusRow>(new CommandDefinition(
            _selectApplicationStatusSql,
            new { Id = applicationId },
            cancellationToken: cancellationToken));

        if (application is null)
        {
            return Result.Failure(
                DomainError.NotFound(
                    "Admission.ApplicationNotFound",
                    $"Admission application with ID '{applicationId}' was not found."));
        }

        if (string.Equals(application.Status, "decided", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(
                DomainError.Conflict(
                    "Admission.ApplicationAlreadyDecided",
                    "Cannot delete assessment evidence from an admission application that has already been decided."));
        }

        if (string.Equals(application.Status, "withdrawn", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(
                DomainError.Conflict(
                    "Admission.ApplicationAlreadyWithdrawn",
                    "Cannot delete assessment evidence from an admission application that has been withdrawn."));
        }

        var evidence = await connection.QuerySingleOrDefaultAsync<EvidenceRow>(new CommandDefinition(
            _selectEvidenceSql,
            new { EvidenceId = evidenceId, ApplicationId = applicationId },
            cancellationToken: cancellationToken));

        if (evidence is null)
        {
            return Result.Failure(
                DomainError.NotFound(
                    "Admission.AssessmentEvidenceNotFound",
                    $"Assessment evidence with ID '{evidenceId}' was not found for application '{applicationId}'."));
        }

        await connection.ExecuteAsync(new CommandDefinition(
            _deleteEvidenceSql,
            new { EvidenceId = evidenceId, ApplicationId = applicationId },
            cancellationToken: cancellationToken));

        return Result.Success();
    }

    private sealed record ApplicationStatusRow(Guid Id, string Status);

    private sealed record EvidenceRow(Guid Id);
}
