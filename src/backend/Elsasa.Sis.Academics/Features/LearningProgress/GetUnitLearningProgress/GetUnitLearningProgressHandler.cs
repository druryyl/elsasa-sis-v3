namespace Elsasa.Sis.Academics.Features.LearningProgress.GetUnitLearningProgress;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying participant progress rosters for a specific learning unit (P2-S05, UC-ACD-12, SCR-ACD-07).
/// Returns all students enrolled in the unit with participation status, learning progress state, operative grade, and completion date.
/// </summary>
public sealed class GetUnitLearningProgressHandler(ISqlConnectionFactory connectionFactory)
{
    static GetUnitLearningProgressHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkUnitSql = """
        SELECT COUNT(1)
        FROM academics.learning_units
        WHERE id = @UnitId;
        """;

    private const string _selectUnitLearningProgressSql = """
        WITH DistinctParticipations AS (
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
            WHERE p.learning_unit_id = @UnitId
              AND p.academic_period_id = @AcademicPeriodId
        ),
        UnitEnrolledStudents AS (
            SELECT
                p.id AS ParticipationId,
                COALESCE(p.student_id, lp.student_id) AS StudentId,
                COALESCE(p.learning_unit_id, lp.learning_unit_id) AS LearningUnitId,
                COALESCE(p.academic_period_id, lp.academic_period_id) AS AcademicPeriodId,
                p.section_label AS SectionLabel,
                COALESCE(p.status, 'enrolled') AS ParticipationStatus,
                lp.id AS ProgressId,
                COALESCE(lp.status, 'not_started') AS ProgressStatus,
                lp.basis AS Basis,
                COALESCE(lp.completion_date, p.completion_date) AS CompletionDate,
                COALESCE(lp.notes, p.notes) AS Notes,
                lp.updated_at AS ProgressUpdatedAt
            FROM (SELECT * FROM DistinctParticipations WHERE rn = 1) p
            FULL OUTER JOIN (
                SELECT *
                FROM academics.learning_progress
                WHERE learning_unit_id = @UnitId
                  AND academic_period_id = @AcademicPeriodId
            ) lp
                ON lp.student_id = p.student_id
               AND lp.learning_unit_id = p.learning_unit_id
               AND lp.academic_period_id = p.academic_period_id
        )
        SELECT
            ues.StudentId,
            s.student_number AS StudentNumber,
            s.full_name AS StudentName,
            ues.LearningUnitId,
            ues.AcademicPeriodId,
            ues.ParticipationId,
            ues.ParticipationStatus,
            ues.SectionLabel,
            ues.ProgressId,
            ues.ProgressStatus,
            ues.Basis,
            res.OperativeGrade,
            res.ScoreNumeric AS OperativeScore,
            res.GradeLetter,
            res.GradeLabel,
            ues.CompletionDate,
            ues.Notes,
            ues.ProgressUpdatedAt AS UpdatedAt
        FROM UnitEnrolledStudents ues
        LEFT JOIN students.students s ON s.id = ues.StudentId
        OUTER APPLY (
            SELECT TOP 1
                ar.score_numeric AS ScoreNumeric,
                ar.grade_letter AS GradeLetter,
                ar.grade_label AS GradeLabel,
                COALESCE(ar.grade_letter, ar.grade_label, CAST(ar.score_numeric AS NVARCHAR(20))) AS OperativeGrade
            FROM academics.academic_results ar
            INNER JOIN academics.assessments a ON a.id = ar.assessment_id
            WHERE a.learning_unit_id = @UnitId
              AND ar.student_id = ues.StudentId
              AND ar.is_final = 1
              AND ar.result_status IN ('approved', 'published')
              AND a.status <> 'cancelled'
            ORDER BY
                CASE WHEN a.assessment_type = 'summative' THEN 1 ELSE 2 END ASC,
                ar.evaluated_date DESC,
                ar.created_at DESC
        ) res
        WHERE (@Status IS NULL OR LOWER(ues.ProgressStatus) = LOWER(@Status))
        ORDER BY ues.SectionLabel ASC, COALESCE(s.full_name, '') ASC, s.student_number ASC;
        """;

    /// <summary>
    /// Executes query returning student progress roster for a learning unit within an academic period.
    /// </summary>
    /// <param name="unitId">The unique identifier of the target learning unit.</param>
    /// <param name="academicPeriodId">The academic period identifier (required).</param>
    /// <param name="status">Optional progress status filter (e.g., 'not_started', 'in_progress', 'completed', 'needs_remediation', 'exempted').</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Roster of students with progress state, operative grade, and completion date, or domain error.</returns>
    public async Task<Result<IReadOnlyList<UnitLearningProgressItemDto>>> HandleAsync(
        Guid unitId,
        Guid? academicPeriodId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        if (unitId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<UnitLearningProgressItemDto>>(
                DomainError.Validation("Academics.LearningUnitIdRequired", "Learning unit ID is required."));
        }

        if (!academicPeriodId.HasValue || academicPeriodId.Value == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<UnitLearningProgressItemDto>>(
                DomainError.Validation("Academics.AcademicPeriodRequired", "Academic period ID is required."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var unitExists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _checkUnitSql,
            new { UnitId = unitId },
            cancellationToken: cancellationToken));

        if (unitExists == 0)
        {
            return Result.Failure<IReadOnlyList<UnitLearningProgressItemDto>>(
                DomainError.NotFound(
                    "Academics.LearningUnitNotFound",
                    $"Learning unit with ID '{unitId}' was not found."));
        }

        var items = await connection.QueryAsync<UnitLearningProgressItemDto>(new CommandDefinition(
            _selectUnitLearningProgressSql,
            new
            {
                UnitId = unitId,
                AcademicPeriodId = academicPeriodId.Value,
                Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim()
            },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<UnitLearningProgressItemDto>>(items.AsList());
    }
}
