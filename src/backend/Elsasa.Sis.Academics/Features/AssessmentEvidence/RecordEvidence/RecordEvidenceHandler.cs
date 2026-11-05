namespace Elsasa.Sis.Academics.Features.AssessmentEvidence.RecordEvidence;

using Dapper;
using Elsasa.Sis.Academics.Features.AssessmentEvidence;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for recording assessment evidence (ACD-WF-08, ACD-WF-20, UC-ACD-07, GAP-07, P3-S06).
/// </summary>
public sealed class RecordEvidenceHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static RecordEvidenceHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAssessmentSql = """
        SELECT
            id AS Id,
            status AS Status
        FROM academics.assessments
        WHERE id = @AssessmentId;
        """;

    private const string _selectSubmissionExistsSql = """
        SELECT COUNT(1)
        FROM academics.submissions
        WHERE id = @SubmissionId;
        """;

    private const string _insertEvidenceSql = """
        INSERT INTO academics.assessment_evidence (
            id,
            assessment_id,
            student_id,
            applicant_id,
            submission_id,
            evidence_type,
            collected_date,
            evidence_reference,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @AssessmentId,
            @StudentId,
            @ApplicantId,
            @SubmissionId,
            @EvidenceType,
            @CollectedDate,
            @EvidenceReference,
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private sealed record AssessmentRow(Guid Id, string Status);

    public async Task<Result<AssessmentEvidenceDto>> HandleAsync(
        Guid assessmentId,
        RecordEvidenceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify parent assessment exists
        var assessment = await connection.QuerySingleOrDefaultAsync<AssessmentRow>(new CommandDefinition(
            _selectAssessmentSql,
            new { AssessmentId = assessmentId },
            cancellationToken: cancellationToken));

        if (assessment is null)
        {
            return Result.Failure<AssessmentEvidenceDto>(
                DomainError.NotFound(
                    "Academic.AssessmentNotFound",
                    $"Assessment with ID '{assessmentId}' was not found."));
        }

        if (string.Equals(assessment.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<AssessmentEvidenceDto>(
                DomainError.Conflict(
                    "Academic.AssessmentCancelled",
                    $"Cannot record evidence for cancelled assessment '{assessmentId}'."));
        }

        // 2. If submission_id provided, verify submission exists
        if (request.SubmissionId.HasValue && request.SubmissionId.Value != Guid.Empty)
        {
            var submissionCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                _selectSubmissionExistsSql,
                new { SubmissionId = request.SubmissionId.Value },
                cancellationToken: cancellationToken));

            if (submissionCount == 0)
            {
                return Result.Failure<AssessmentEvidenceDto>(
                    DomainError.NotFound(
                        "Academic.SubmissionNotFound",
                        $"Coursework submission with ID '{request.SubmissionId.Value}' was not found."));
            }
        }

        // 3. Insert assessment evidence record
        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var createdBy = currentUser.Username ?? "system";
        var normalizedType = request.EvidenceType.Trim().ToLowerInvariant();
        var evidenceReference = string.IsNullOrWhiteSpace(request.EvidenceReference) ? null : request.EvidenceReference.Trim();
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        var submissionId = (request.SubmissionId.HasValue && request.SubmissionId.Value != Guid.Empty)
            ? request.SubmissionId.Value
            : (Guid?)null;

        await connection.ExecuteAsync(new CommandDefinition(
            _insertEvidenceSql,
            new
            {
                Id = id,
                AssessmentId = assessmentId,
                StudentId = request.StudentId,
                ApplicantId = request.ApplicantId,
                SubmissionId = submissionId,
                EvidenceType = normalizedType,
                request.CollectedDate,
                EvidenceReference = evidenceReference,
                Notes = notes,
                CreatedAt = createdAt,
                CreatedBy = createdBy
            },
            cancellationToken: cancellationToken));

        var response = new AssessmentEvidenceDto(
            id,
            assessmentId,
            request.StudentId,
            request.ApplicantId,
            submissionId,
            normalizedType,
            request.CollectedDate,
            evidenceReference,
            notes,
            createdAt,
            createdBy,
            null,
            null);

        return Result.Success(response);
    }
}
