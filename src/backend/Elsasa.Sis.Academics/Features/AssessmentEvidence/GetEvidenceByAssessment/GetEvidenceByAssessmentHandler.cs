namespace Elsasa.Sis.Academics.Features.AssessmentEvidence.GetEvidenceByAssessment;

using Dapper;
using Elsasa.Sis.Academics.Features.AssessmentEvidence;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for querying assessment evidence roster (ACD-WF-08, UC-ACD-07, P3-S06).
/// </summary>
public sealed class GetEvidenceByAssessmentHandler(
    ISqlConnectionFactory connectionFactory)
{
    static GetEvidenceByAssessmentHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAssessmentExistsSql = """
        SELECT COUNT(1)
        FROM academics.assessments
        WHERE id = @AssessmentId;
        """;

    private const string _selectEvidenceSql = """
        SELECT
            id AS Id,
            assessment_id AS AssessmentId,
            student_id AS StudentId,
            applicant_id AS ApplicantId,
            submission_id AS SubmissionId,
            evidence_type AS EvidenceType,
            collected_date AS CollectedDate,
            evidence_reference AS EvidenceReference,
            notes AS Notes,
            created_at AS CreatedAt,
            created_by AS CreatedBy,
            updated_at AS UpdatedAt,
            updated_by AS UpdatedBy
        FROM academics.assessment_evidence
        WHERE assessment_id = @AssessmentId
          AND (@StudentId IS NULL OR student_id = @StudentId)
          AND (@ApplicantId IS NULL OR applicant_id = @ApplicantId)
        ORDER BY collected_date DESC, created_at DESC;
        """;

    public async Task<Result<IReadOnlyList<AssessmentEvidenceDto>>> HandleAsync(
        Guid assessmentId,
        Guid? studentId = null,
        Guid? applicantId = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify parent assessment exists
        var assessmentExists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _selectAssessmentExistsSql,
            new { AssessmentId = assessmentId },
            cancellationToken: cancellationToken));

        if (assessmentExists == 0)
        {
            return Result.Failure<IReadOnlyList<AssessmentEvidenceDto>>(
                DomainError.NotFound(
                    "Academic.AssessmentNotFound",
                    $"Assessment with ID '{assessmentId}' was not found."));
        }

        // 2. Query evidence items
        var items = (await connection.QueryAsync<AssessmentEvidenceDto>(new CommandDefinition(
            _selectEvidenceSql,
            new
            {
                AssessmentId = assessmentId,
                StudentId = (studentId.HasValue && studentId.Value != Guid.Empty) ? studentId.Value : (Guid?)null,
                ApplicantId = (applicantId.HasValue && applicantId.Value != Guid.Empty) ? applicantId.Value : (Guid?)null
            },
            cancellationToken: cancellationToken))).AsList();

        return Result.Success<IReadOnlyList<AssessmentEvidenceDto>>(items);
    }
}
