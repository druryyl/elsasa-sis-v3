namespace Elsasa.Sis.Teaching.Features.TeacherEligibility.GetCandidateEvaluations;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying candidate eligibility evaluation records with optional filters (TD-03, P2-S05).
/// </summary>
public sealed class GetCandidateEvaluationsHandler(ISqlConnectionFactory connectionFactory)
{
    static GetCandidateEvaluationsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _queryEvaluationsSql = """
        SELECT
            e.id AS Id,
            e.teaching_demand_id AS TeachingDemandId,
            d.demand_code AS DemandCode,
            e.teacher_profile_id AS TeacherProfileId,
            tp.full_name AS TeacherName,
            tp.teacher_number AS TeacherNumber,
            e.evaluation_date AS EvaluationDate,
            e.evaluated_by AS EvaluatedBy,
            e.is_eligible AS IsEligible,
            e.eligibility_status AS EligibilityStatus,
            e.profile_active_check AS ProfileActiveCheck,
            e.authorization_check_passed AS AuthorizationCheckPassed,
            e.qualification_check_passed AS QualificationCheckPassed,
            e.availability_check_passed AS AvailabilityCheckPassed,
            e.capacity_check_passed AS CapacityCheckPassed,
            e.explanation AS Explanation,
            e.notes AS Notes,
            e.created_at AS CreatedAt
        FROM teaching.teacher_eligibility_evaluations e
        INNER JOIN teaching.teaching_demands d ON d.id = e.teaching_demand_id
        INNER JOIN teaching.teacher_profiles tp ON tp.id = e.teacher_profile_id
        WHERE (@TeachingDemandId IS NULL OR e.teaching_demand_id = @TeachingDemandId)
          AND (@TeacherProfileId IS NULL OR e.teacher_profile_id = @TeacherProfileId)
        ORDER BY e.created_at DESC;
        """;

    public async Task<Result<IReadOnlyList<CandidateEvaluationSummaryDto>>> HandleAsync(
        Guid? teachingDemandId,
        Guid? teacherProfileId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var evaluations = (await connection.QueryAsync<CandidateEvaluationSummaryDto>(new CommandDefinition(
            _queryEvaluationsSql,
            new
            {
                TeachingDemandId = teachingDemandId,
                TeacherProfileId = teacherProfileId
            },
            cancellationToken: cancellationToken))).ToList();

        return Result.Success<IReadOnlyList<CandidateEvaluationSummaryDto>>(evaluations);
    }
}
