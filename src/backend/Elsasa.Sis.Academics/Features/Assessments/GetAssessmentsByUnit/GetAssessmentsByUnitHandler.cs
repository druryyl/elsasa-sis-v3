namespace Elsasa.Sis.Academics.Features.Assessments.GetAssessmentsByUnit;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying assessments for a learning unit with status/date filters and counts (P2-S05).
/// </summary>
public sealed class GetAssessmentsByUnitHandler(ISqlConnectionFactory connectionFactory)
{
    static GetAssessmentsByUnitHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkUnitSql = """
        SELECT COUNT(1)
        FROM academics.learning_units
        WHERE id = @LearningUnitId;
        """;

    private const string _selectAssessmentsSql = """
        SELECT
            a.id AS Id,
            a.learning_unit_id AS LearningUnitId,
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
            a.created_at AS CreatedAt,
            a.created_by AS CreatedBy,
            a.updated_at AS UpdatedAt,
            a.updated_by AS UpdatedBy
        FROM academics.assessments a
        LEFT JOIN academics.learning_activities la ON a.learning_activity_id = la.id
        WHERE a.learning_unit_id = @LearningUnitId
          AND (@Status IS NULL OR a.status = @Status)
          AND (@AssessmentType IS NULL OR a.assessment_type = @AssessmentType)
          AND (@FromDate IS NULL OR a.assessment_date >= @FromDate)
          AND (@ToDate IS NULL OR a.assessment_date <= @ToDate)
        ORDER BY a.assessment_date DESC, a.created_at DESC;
        """;

    public async Task<Result<IReadOnlyList<AssessmentSummaryDto>>> HandleAsync(
        Guid unitId,
        string? status,
        string? assessmentType,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var unitExists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _checkUnitSql,
            new { LearningUnitId = unitId },
            cancellationToken: cancellationToken));

        if (unitExists == 0)
        {
            return Result.Failure<IReadOnlyList<AssessmentSummaryDto>>(
                DomainError.NotFound(
                    "Academic.LearningUnitNotFound",
                    $"Learning unit with ID '{unitId}' was not found."));
        }

        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToLowerInvariant();
        var normalizedType = string.IsNullOrWhiteSpace(assessmentType) ? null : assessmentType.Trim().ToLowerInvariant();

        var assessments = await connection.QueryAsync<AssessmentSummaryDto>(new CommandDefinition(
            _selectAssessmentsSql,
            new
            {
                LearningUnitId = unitId,
                Status = normalizedStatus,
                AssessmentType = normalizedType,
                FromDate = fromDate,
                ToDate = toDate
            },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<AssessmentSummaryDto>>(assessments.AsList());
    }
}
