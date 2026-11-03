namespace Elsasa.Sis.Students.Features.Admission.AssessmentEvidence.AddAssessmentEvidence;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing manual diagnostic assessment evidence intake (ADR-004, ADR-005).
/// </summary>
public sealed class AddAssessmentEvidenceHandler(
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

    private const string _insertEvidenceSql = """
        INSERT INTO students.entrance_assessment_evidence (
            id,
            application_id,
            assessment_type,
            score,
            source,
            assessment_date,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @ApplicationId,
            @AssessmentType,
            @Score,
            @Source,
            @AssessmentDate,
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private const string _defaultSource = "External Assessment Center";

    /// <summary>
    /// Executes the addition of manual assessment evidence for an admission application.
    /// </summary>
    /// <param name="applicationId">The admission application ID.</param>
    /// <param name="request">The assessment evidence request details.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A result containing the created assessment evidence response.</returns>
    public async Task<Result<AddAssessmentEvidenceResponse>> HandleAsync(
        Guid applicationId,
        AddAssessmentEvidenceRequest request,
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
            return Result.Failure<AddAssessmentEvidenceResponse>(
                DomainError.NotFound(
                    "Admission.ApplicationNotFound",
                    $"Admission application with ID '{applicationId}' was not found."));
        }

        if (string.Equals(application.Status, "decided", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<AddAssessmentEvidenceResponse>(
                DomainError.Conflict(
                    "Admission.ApplicationAlreadyDecided",
                    "Cannot add assessment evidence to an admission application that has already been decided."));
        }

        if (string.Equals(application.Status, "withdrawn", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<AddAssessmentEvidenceResponse>(
                DomainError.Conflict(
                    "Admission.ApplicationAlreadyWithdrawn",
                    "Cannot add assessment evidence to an admission application that has been withdrawn."));
        }

        var evidenceId = Guid.NewGuid();
        var source = string.IsNullOrWhiteSpace(request.Source) ? _defaultSource : request.Source.Trim();
        var assessmentType = request.AssessmentType.Trim();
        var score = string.IsNullOrWhiteSpace(request.Score) ? null : request.Score.Trim();
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        var auditUser = currentUser.Username ?? "system";
        var now = DateTimeOffset.UtcNow;

        await connection.ExecuteAsync(new CommandDefinition(
            _insertEvidenceSql,
            new
            {
                Id = evidenceId,
                ApplicationId = applicationId,
                AssessmentType = assessmentType,
                Score = score,
                Source = source,
                AssessmentDate = request.AssessmentDate,
                Notes = notes,
                CreatedAt = now,
                CreatedBy = auditUser,
            },
            cancellationToken: cancellationToken));

        var response = new AddAssessmentEvidenceResponse(
            evidenceId,
            applicationId,
            assessmentType,
            score,
            source,
            request.AssessmentDate,
            notes,
            now);

        return Result.Success(response);
    }

    private sealed record ApplicationStatusRow(Guid Id, string Status);
}
