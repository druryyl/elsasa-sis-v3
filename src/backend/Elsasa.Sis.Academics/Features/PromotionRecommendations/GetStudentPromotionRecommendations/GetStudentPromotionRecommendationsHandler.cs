namespace Elsasa.Sis.Academics.Features.PromotionRecommendations.GetStudentPromotionRecommendations;

using Dapper;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying historical promotion recommendations for a student ordered by academic period (UC-ACD-17, P3-S09).
/// </summary>
public sealed class GetStudentPromotionRecommendationsHandler(ISqlConnectionFactory connectionFactory)
{
    static GetStudentPromotionRecommendationsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectStudentRecommendationsSql = """
        SELECT
            pr.id AS Id,
            pr.student_id AS StudentId,
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
            pr.issued_at AS IssuedAt
        FROM academics.promotion_recommendations pr
        LEFT JOIN operations.operational_periods op ON op.id = pr.academic_period_id
        WHERE pr.student_id = @StudentId
        ORDER BY COALESCE(op.start_date, CAST(pr.issued_at AS DATE)) ASC, pr.issued_at ASC;
        """;

    /// <summary>
    /// Executes query returning historical promotion recommendations for a student.
    /// </summary>
    /// <param name="studentId">The student identifier (required).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of student promotion recommendations ordered by academic period.</returns>
    public async Task<Result<IReadOnlyList<PromotionRecommendationDto>>> HandleAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        if (studentId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<PromotionRecommendationDto>>(
                DomainError.Validation("Academics.StudentIdRequired", "Student ID is required."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var recommendations = await connection.QueryAsync<PromotionRecommendationDto>(new CommandDefinition(
            _selectStudentRecommendationsSql,
            new { StudentId = studentId },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<PromotionRecommendationDto>>(recommendations.AsList());
    }
}
