namespace Elsasa.Sis.Academics.Features.Assessments.GetAssessmentById;

using Dapper;
using Elsasa.Sis.Academics.Common;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying detailed assessment specifications, joins, and participant counts (P2-S05).
/// </summary>
public sealed class GetAssessmentByIdHandler(ISqlConnectionFactory connectionFactory)
{
    static GetAssessmentByIdHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAssessmentByIdSql = """
        SELECT
            a.id AS Id,
            a.learning_unit_id AS LearningUnitId,
            u.name AS LearningUnitTitle,
            u.code AS LearningUnitCode,
            a.learning_activity_id AS LearningActivityId,
            la.title AS LearningActivityTitle,
            a.title AS Title,
            a.assessment_type AS AssessmentType,
            a.assessment_method AS AssessmentMethod,
            a.max_score AS MaxScore,
            a.passing_score AS PassingScore,
            a.reassessment_policy AS ReassessmentPolicy,
            a.criteria AS Criteria,
            a.assessment_date AS AssessmentDate,
            a.assessor_reference AS AssessorReference,
            a.status AS Status,
            a.notes AS Notes,
            (SELECT COUNT(1) FROM academics.assessment_evidence e WHERE e.assessment_id = a.id) AS EvidenceCount,
            (SELECT COUNT(1) FROM academics.academic_results r WHERE r.assessment_id = a.id) AS ResultCount,
            (
                SELECT COUNT(DISTINCT p.ParticipantId)
                FROM (
                    SELECT COALESCE(r.student_id, r.applicant_id) AS ParticipantId
                    FROM academics.academic_results r
                    WHERE r.assessment_id = a.id
                    UNION
                    SELECT COALESCE(e.student_id, e.applicant_id) AS ParticipantId
                    FROM academics.assessment_evidence e
                    WHERE e.assessment_id = a.id
                ) p
                WHERE p.ParticipantId IS NOT NULL
            ) AS ParticipantCount,
            a.created_at AS CreatedAt,
            a.created_by AS CreatedBy,
            a.updated_at AS UpdatedAt,
            a.updated_by AS UpdatedBy
        FROM academics.assessments a
        INNER JOIN academics.learning_units u ON a.learning_unit_id = u.id
        LEFT JOIN academics.learning_activities la ON a.learning_activity_id = la.id
        WHERE a.id = @Id;
        """;

    public async Task<Result<AssessmentDetailDto>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var assessment = await connection.QuerySingleOrDefaultAsync<AssessmentDetailDto>(new CommandDefinition(
            _selectAssessmentByIdSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (assessment is null)
        {
            return Result.Failure<AssessmentDetailDto>(
                DomainErrors.Academics.Assessments.NotFound);
        }

        return Result.Success(assessment);
    }
}
