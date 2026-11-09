namespace Elsasa.Sis.Academics.Features.AcademicStandings.GetCohortStandings;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying determined academic standings for a cohort with student details and exception metadata (TD-03, UC-ACD-17, P3-S09, SCR-ACD-09).
/// </summary>
public sealed class GetCohortStandingsHandler(ISqlConnectionFactory connectionFactory)
{
    static GetCohortStandingsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectCohortStandingsSql = """
        SELECT
            ast.id AS Id,
            ast.student_id AS StudentId,
            COALESCE(s.student_number, '') AS StudentNumber,
            COALESCE(s.full_name, 'Student ' + CAST(ast.student_id AS NVARCHAR(36))) AS StudentName,
            COALESCE(p.grade, pr.from_level, @GradeLevel, '') AS GradeLevel,
            ast.academic_period_id AS AcademicPeriodId,
            ast.standing AS Standing,
            ast.policy_reference AS PolicyReference,
            ast.conditions AS Conditions,
            ast.is_exception AS IsException,
            ast.exception_reason AS ExceptionReason,
            ast.committee_reference AS CommitteeReference,
            ast.authorized_by AS AuthorizedBy,
            ast.determined_at AS DeterminedAt,
            ast.created_at AS CreatedAt,
            ast.updated_at AS UpdatedAt
        FROM academics.academic_standings ast
        LEFT JOIN students.students s ON s.id = ast.student_id
        LEFT JOIN students.administrative_placements p ON p.student_id = ast.student_id AND p.is_current = 1
        LEFT JOIN academics.promotion_recommendations pr ON pr.student_id = ast.student_id AND pr.academic_period_id = ast.academic_period_id
        WHERE ast.academic_period_id = @AcademicPeriodId
          AND (
              @GradeLevel IS NULL
              OR RTRIM(LTRIM(@GradeLevel)) = ''
              OR p.grade = RTRIM(LTRIM(@GradeLevel))
              OR p.cohort = RTRIM(LTRIM(@GradeLevel))
              OR p.school_level = RTRIM(LTRIM(@GradeLevel))
              OR pr.from_level = RTRIM(LTRIM(@GradeLevel))
              OR EXISTS (
                  SELECT 1 FROM academics.academic_participations ap
                  LEFT JOIN academics.learning_units lu ON lu.id = ap.learning_unit_id
                  WHERE ap.student_id = ast.student_id
                    AND ap.academic_period_id = @AcademicPeriodId
                    AND (ap.section_label = RTRIM(LTRIM(@GradeLevel)) OR lu.level = RTRIM(LTRIM(@GradeLevel)))
              )
          )
        ORDER BY StudentName ASC;
        """;

    /// <summary>
    /// Executes query returning cohort standing determinations for a period and optional grade level.
    /// </summary>
    /// <param name="academicPeriodId">The academic period identifier (required).</param>
    /// <param name="gradeLevel">The grade level filter (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of cohort standing items with student details and exception metadata.</returns>
    public async Task<Result<IReadOnlyList<CohortStandingItemDto>>> HandleAsync(
        Guid academicPeriodId,
        string? gradeLevel = null,
        CancellationToken cancellationToken = default)
    {
        if (academicPeriodId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<CohortStandingItemDto>>(
                DomainError.Validation("Academics.AcademicPeriodRequired", "Academic period ID is required."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var standings = await connection.QueryAsync<CohortStandingItemDto>(new CommandDefinition(
            _selectCohortStandingsSql,
            new
            {
                AcademicPeriodId = academicPeriodId,
                GradeLevel = string.IsNullOrWhiteSpace(gradeLevel) ? null : gradeLevel.Trim()
            },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<CohortStandingItemDto>>(standings.AsList());
    }
}
