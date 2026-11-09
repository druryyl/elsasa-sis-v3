namespace Elsasa.Sis.Academics.Features.AcademicResults.GetResultsByAssessment;

using Dapper;
using Elsasa.Sis.Academics.Features.AcademicResults;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for querying assessment results roster with attempt lineage and scoring (SCR-ACD-02, P4-S10).
/// </summary>
public sealed class GetResultsByAssessmentHandler(
    ISqlConnectionFactory connectionFactory)
{
    static GetResultsByAssessmentHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAssessmentExistsSql = """
        SELECT COUNT(1)
        FROM academics.assessments
        WHERE id = @AssessmentId;
        """;

    private const string _selectResultsSql = """
        SELECT
            r.id AS Id,
            r.assessment_id AS AssessmentId,
            r.student_id AS StudentId,
            r.applicant_id AS ApplicantId,
            std.student_number AS StudentNumber,
            COALESCE(std.full_name, LTRIM(RTRIM(CONCAT(app.first_name, ' ', app.last_name)))) AS CandidateName,
            r.attempt_number AS AttemptNumber,
            r.replaces_result_id AS ReplacesResultId,
            r.is_final AS IsFinal,
            r.score_numeric AS ScoreNumeric,
            r.score_max AS ScoreMax,
            r.grade_letter AS GradeLetter,
            r.grade_label AS GradeLabel,
            r.narrative_feedback AS NarrativeFeedback,
            r.evaluation_model AS EvaluationModel,
            r.evaluated_by AS EvaluatedBy,
            r.evaluated_date AS EvaluatedDate,
            r.result_status AS ResultStatus,
            r.approved_by AS ApprovedBy,
            r.approved_date AS ApprovedDate,
            r.published_date AS PublishedDate,
            r.notes AS Notes,
            r.created_at AS CreatedAt,
            r.created_by AS CreatedBy,
            r.updated_at AS UpdatedAt,
            r.updated_by AS UpdatedBy
        FROM academics.academic_results r
        LEFT JOIN students.students std ON std.id = r.student_id
        LEFT JOIN students.applicants app ON app.id = r.applicant_id
        WHERE r.assessment_id = @AssessmentId
          AND (@OnlyFinal = 0 OR r.is_final = 1)
          AND (@Status IS NULL OR r.result_status = @Status)
        ORDER BY
            COALESCE(std.full_name, LTRIM(RTRIM(CONCAT(app.first_name, ' ', app.last_name)))) ASC,
            r.attempt_number ASC;
        """;

    public async Task<Result<IReadOnlyList<AcademicResultDetailDto>>> HandleAsync(
        Guid assessmentId,
        string? status = null,
        bool onlyFinal = true,
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
            return Result.Failure<IReadOnlyList<AcademicResultDetailDto>>(
                DomainError.NotFound(
                    "Academic.AssessmentNotFound",
                    $"Assessment with ID '{assessmentId}' was not found."));
        }

        // 2. Query results with candidate joins
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToLowerInvariant();

        var results = await connection.QueryAsync<AcademicResultDetailDto>(new CommandDefinition(
            _selectResultsSql,
            new
            {
                AssessmentId = assessmentId,
                OnlyFinal = onlyFinal ? 1 : 0,
                Status = normalizedStatus
            },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<AcademicResultDetailDto>>(results.AsList());
    }
}
