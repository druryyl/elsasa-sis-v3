namespace Elsasa.Sis.Academics.Features.AcademicParticipations.GetStudentParticipations;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying all unit participations for a student (ADR-004, ADR-005, P2-S06, TD-08).
/// </summary>
public sealed class GetStudentParticipationsHandler(ISqlConnectionFactory connectionFactory)
{
    static GetStudentParticipationsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectStudentParticipationsSql = """
        SELECT
            p.id AS Id,
            p.student_id AS StudentId,
            p.learning_unit_id AS LearningUnitId,
            u.code AS LearningUnitCode,
            u.name AS LearningUnitName,
            u.credit_weight AS CreditWeight,
            p.academic_period_id AS AcademicPeriodId,
            p.academic_period_label AS AcademicPeriodLabel,
            p.participation_type AS ParticipationType,
            p.attempt_number AS AttemptNumber,
            p.source AS Source,
            p.section_label AS SectionLabel,
            p.status AS Status,
            p.start_date AS StartDate,
            p.end_date AS EndDate,
            p.completion_date AS CompletionDate,
            p.source_institution AS SourceInstitution,
            p.external_grade_or_credit AS ExternalGradeOrCredit,
            p.recognition_basis AS RecognitionBasis,
            p.approved_by AS ApprovedBy,
            p.student_learning_plan_id AS StudentLearningPlanId,
            p.notes AS Notes,
            p.created_at AS CreatedAt,
            p.created_by AS CreatedBy,
            p.updated_at AS UpdatedAt,
            p.updated_by AS UpdatedBy
        FROM academics.academic_participations p
        INNER JOIN academics.learning_units u ON u.id = p.learning_unit_id
        WHERE p.student_id = @StudentId
          AND (@AcademicPeriodId IS NULL OR p.academic_period_id = @AcademicPeriodId)
        ORDER BY p.start_date DESC, u.code ASC;
        """;

    /// <summary>
    /// Executes student participations query with optional academic period filter.
    /// </summary>
    /// <param name="studentId">The unique identifier of the target student.</param>
    /// <param name="academicPeriodId">Optional academic period filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of unit participations for the student.</returns>
    public async Task<Result<IReadOnlyList<StudentParticipationItemDto>>> HandleAsync(
        Guid studentId,
        Guid? academicPeriodId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var participations = await connection.QueryAsync<StudentParticipationItemDto>(new CommandDefinition(
            _selectStudentParticipationsSql,
            new
            {
                StudentId = studentId,
                AcademicPeriodId = academicPeriodId
            },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<StudentParticipationItemDto>>(participations.AsList());
    }
}
