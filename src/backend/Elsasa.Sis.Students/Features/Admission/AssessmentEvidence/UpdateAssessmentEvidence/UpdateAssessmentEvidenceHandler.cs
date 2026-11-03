namespace Elsasa.Sis.Students.Features.Admission.AssessmentEvidence.UpdateAssessmentEvidence;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing manual diagnostic assessment evidence update (ADR-004, ADR-005).
/// </summary>
public sealed class UpdateAssessmentEvidenceHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
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

    private const string _updateEvidenceSql = """
        UPDATE students.entrance_assessment_evidence
        SET
            assessment_type = @AssessmentType,
            score = @Score,
            source = @Source,
            assessment_date = @AssessmentDate,
            notes = @Notes,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @EvidenceId AND application_id = @ApplicationId;
        """;

    private const string _defaultSource = "External Assessment Center";

    /// <summary>
    /// Executes the update of manual assessment evidence for an admission application.
    /// </summary>
    /// <param name="applicationId">The admission application ID.</param>
    /// <param name="evidenceId">The assessment evidence ID.</param>
    /// <param name="request">The updated assessment evidence request details.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A result containing the updated assessment evidence response.</returns>
    public async Task<Result<UpdateAssessmentEvidenceResponse>> HandleAsync(
        Guid applicationId,
        Guid evidenceId,
        UpdateAssessmentEvidenceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var application = await connection.QuerySingleOrDefaultAsync<ApplicationStatusRow>(new CommandDefinition(
            _selectApplicationStatusSql,
            new { Id = applicationId },
            cancellationToken: cancellationToken));

        if (application is null)
        {
            return Result.Failure<UpdateAssessmentEvidenceResponse>(
                DomainError.NotFound(
                    "Admission.ApplicationNotFound",
                    $"Admission application with ID '{applicationId}' was not found."));
        }

        if (string.Equals(application.Status, "decided", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<UpdateAssessmentEvidenceResponse>(
                DomainError.Conflict(
                    "Admission.ApplicationAlreadyDecided",
                    "Cannot update assessment evidence for an admission application that has already been decided."));
        }

        if (string.Equals(application.Status, "withdrawn", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<UpdateAssessmentEvidenceResponse>(
                DomainError.Conflict(
                    "Admission.ApplicationAlreadyWithdrawn",
                    "Cannot update assessment evidence for an admission application that has been withdrawn."));
        }

        var evidence = await connection.QuerySingleOrDefaultAsync<EvidenceRow>(new CommandDefinition(
            _selectEvidenceSql,
            new { EvidenceId = evidenceId, ApplicationId = applicationId },
            cancellationToken: cancellationToken));

        if (evidence is null)
        {
            return Result.Failure<UpdateAssessmentEvidenceResponse>(
                DomainError.NotFound(
                    "Admission.AssessmentEvidenceNotFound",
                    $"Assessment evidence with ID '{evidenceId}' was not found for application '{applicationId}'."));
        }

        var source = string.IsNullOrWhiteSpace(request.Source) ? _defaultSource : request.Source.Trim();
        var assessmentType = request.AssessmentType.Trim();
        var score = string.IsNullOrWhiteSpace(request.Score) ? null : request.Score.Trim();
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        var auditUser = currentUser.Username ?? "system";
        var now = DateTimeOffset.UtcNow;

        await connection.ExecuteAsync(new CommandDefinition(
            _updateEvidenceSql,
            new
            {
                EvidenceId = evidenceId,
                ApplicationId = applicationId,
                AssessmentType = assessmentType,
                Score = score,
                Source = source,
                AssessmentDate = request.AssessmentDate,
                Notes = notes,
                UpdatedAt = now,
                UpdatedBy = auditUser,
            },
            cancellationToken: cancellationToken));

        var response = new UpdateAssessmentEvidenceResponse(
            evidenceId,
            applicationId,
            assessmentType,
            score,
            source,
            request.AssessmentDate,
            notes,
            now,
            auditUser);

        return Result.Success(response);
    }

    private sealed record ApplicationStatusRow(Guid Id, string Status);

    private sealed record EvidenceRow(Guid Id);
}
