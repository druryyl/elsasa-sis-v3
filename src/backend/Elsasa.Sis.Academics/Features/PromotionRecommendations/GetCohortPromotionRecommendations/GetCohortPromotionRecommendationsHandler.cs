namespace Elsasa.Sis.Academics.Features.PromotionRecommendations.GetCohortPromotionRecommendations;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying committed academic progression recommendations for a cohort in an academic period and optional grade level (TD-03, UC-ACD-17, P3-S09, SCR-ACD-09).
/// </summary>
public sealed class GetCohortPromotionRecommendationsHandler(ISqlConnectionFactory connectionFactory)
{
    static GetCohortPromotionRecommendationsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectCohortPromotionRecommendationsSql = """
        SELECT
            pr.id AS Id,
            pr.student_id AS StudentId,
            COALESCE(s.student_number, '') AS StudentNumber,
            COALESCE(s.full_name, 'Student ' + CAST(pr.student_id AS NVARCHAR(36))) AS StudentName,
            pr.academic_period_id AS AcademicPeriodId,
            pr.recommendation AS Recommendation,
            pr.from_level AS FromLevel,
            pr.recommended_to_level AS RecommendedToLevel,
            pr.conditions AS Conditions,
            pr.supporting_evidence AS SupportingEvidence,
            pr.academic_standing_id AS AcademicStandingId,
            pr.is_exception AS IsException,
            pr.exception_reason AS ExceptionReason,
            pr.committee_reference AS CommitteeReference,
            pr.authorized_by AS AuthorizedBy,
            pr.issued_at AS IssuedAt,
            pr.created_at AS CreatedAt,
            pr.updated_at AS UpdatedAt
        FROM academics.promotion_recommendations pr
        LEFT JOIN students.students s ON s.id = pr.student_id
        LEFT JOIN students.administrative_placements p ON p.student_id = pr.student_id AND p.is_current = 1
        WHERE pr.academic_period_id = @AcademicPeriodId
          AND (
              @GradeLevel IS NULL
              OR RTRIM(LTRIM(@GradeLevel)) = ''
              OR pr.from_level = RTRIM(LTRIM(@GradeLevel))
              OR p.grade = RTRIM(LTRIM(@GradeLevel))
              OR p.cohort = RTRIM(LTRIM(@GradeLevel))
              OR p.school_level = RTRIM(LTRIM(@GradeLevel))
              OR EXISTS (
                  SELECT 1 FROM academics.academic_participations ap
                  LEFT JOIN academics.learning_units lu ON lu.id = ap.learning_unit_id
                  WHERE ap.student_id = pr.student_id
                    AND ap.academic_period_id = @AcademicPeriodId
                    AND (ap.section_label = RTRIM(LTRIM(@GradeLevel)) OR lu.level = RTRIM(LTRIM(@GradeLevel)))
              )
          )
        ORDER BY StudentName ASC;
        """;

    /// <summary>
    /// Executes query returning committed progression recommendations for a cohort.
    /// </summary>
    /// <param name="academicPeriodId">The academic period identifier (required).</param>
    /// <param name="gradeLevel">The grade level filter (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of cohort progression recommendations with student details and exception metadata.</returns>
    public async Task<Result<IReadOnlyList<CohortPromotionRecommendationItemDto>>> HandleAsync(
        Guid academicPeriodId,
        string? gradeLevel = null,
        CancellationToken cancellationToken = default)
    {
        if (academicPeriodId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<CohortPromotionRecommendationItemDto>>(
                DomainError.Validation("Academics.AcademicPeriodRequired", "Academic period ID is required."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var recommendations = await connection.QueryAsync<CohortPromotionRecommendationItemDto>(new CommandDefinition(
            _selectCohortPromotionRecommendationsSql,
            new
            {
                AcademicPeriodId = academicPeriodId,
                GradeLevel = string.IsNullOrWhiteSpace(gradeLevel) ? null : gradeLevel.Trim()
            },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<CohortPromotionRecommendationItemDto>>(recommendations.AsList());
    }
}
