namespace Elsasa.Sis.Academics.Features.LearningProgress.GetStudentLearningProgress;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying student learning progress ledger across enrolled units for an academic period (P2-S05, UC-ACD-12, SCR-ACD-07).
/// Joins academics.academic_participations, academics.learning_units, and academics.learning_progress using fast parameterized Dapper query.
/// </summary>
public sealed class GetStudentLearningProgressHandler(ISqlConnectionFactory connectionFactory)
{
    static GetStudentLearningProgressHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectStudentLearningProgressSql = """
        WITH LatestParticipations AS (
            SELECT
                p.*,
                ROW_NUMBER() OVER (
                    PARTITION BY p.student_id, p.learning_unit_id, p.academic_period_id
                    ORDER BY
                        CASE WHEN p.status = 'in_progress' THEN 1
                             WHEN p.status = 'completed' THEN 2
                             WHEN p.status = 'paused' THEN 3
                             ELSE 4 END ASC,
                        p.attempt_number DESC,
                        p.created_at DESC
                ) AS rn
            FROM academics.academic_participations p
            WHERE p.student_id = @StudentId
              AND p.academic_period_id = @AcademicPeriodId
        )
        SELECT
            lp.id AS ProgressId,
            p.id AS ParticipationId,
            COALESCE(p.student_id, lp.student_id) AS StudentId,
            COALESCE(p.learning_unit_id, lp.learning_unit_id) AS LearningUnitId,
            u.code AS LearningUnitCode,
            u.name AS LearningUnitName,
            u.credit_weight AS CreditWeight,
            COALESCE(p.academic_period_id, lp.academic_period_id) AS AcademicPeriodId,
            p.section_label AS SectionLabel,
            COALESCE(p.status, 'enrolled') AS ParticipationStatus,
            COALESCE(lp.status, 'not_started') AS ProgressStatus,
            lp.basis AS Basis,
            COALESCE(lp.completion_date, p.completion_date) AS CompletionDate,
            COALESCE(lp.notes, p.notes) AS Notes,
            lp.created_at AS CreatedAt,
            lp.created_by AS CreatedBy,
            lp.updated_at AS UpdatedAt,
            lp.updated_by AS UpdatedBy
        FROM (SELECT * FROM LatestParticipations WHERE rn = 1) p
        FULL OUTER JOIN (
            SELECT *
            FROM academics.learning_progress
            WHERE student_id = @StudentId
              AND academic_period_id = @AcademicPeriodId
        ) lp
            ON lp.student_id = p.student_id
           AND lp.learning_unit_id = p.learning_unit_id
           AND lp.academic_period_id = p.academic_period_id
        INNER JOIN academics.learning_units u
            ON u.id = COALESCE(p.learning_unit_id, lp.learning_unit_id)
        ORDER BY u.code ASC, u.name ASC;
        """;

    /// <summary>
    /// Executes query returning learning progress items for a student across enrolled units within an academic period.
    /// </summary>
    /// <param name="studentId">The unique identifier of the target student.</param>
    /// <param name="academicPeriodId">The academic period identifier (required).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of student learning progress items or domain validation error.</returns>
    public async Task<Result<IReadOnlyList<StudentLearningProgressItemDto>>> HandleAsync(
        Guid studentId,
        Guid? academicPeriodId,
        CancellationToken cancellationToken = default)
    {
        if (studentId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<StudentLearningProgressItemDto>>(
                DomainError.Validation("Academics.StudentIdRequired", "Student ID is required."));
        }

        if (!academicPeriodId.HasValue || academicPeriodId.Value == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<StudentLearningProgressItemDto>>(
                DomainError.Validation("Academics.AcademicPeriodRequired", "Academic period ID is required."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var items = await connection.QueryAsync<StudentLearningProgressItemDto>(new CommandDefinition(
            _selectStudentLearningProgressSql,
            new
            {
                StudentId = studentId,
                AcademicPeriodId = academicPeriodId.Value
            },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<StudentLearningProgressItemDto>>(items.AsList());
    }
}
