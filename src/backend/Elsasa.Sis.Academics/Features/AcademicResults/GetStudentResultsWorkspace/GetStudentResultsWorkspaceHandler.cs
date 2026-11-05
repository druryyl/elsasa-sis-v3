namespace Elsasa.Sis.Academics.Features.AcademicResults.GetStudentResultsWorkspace;

using Dapper;
using Elsasa.Sis.Academics.Features.AcademicResults;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for student self-service learning results workspace (SCR-SELF-03, P4-S10).
/// Strictly queries published, operative (is_final = 1) results and excludes provisional summative scores.
/// </summary>
public sealed class GetStudentResultsWorkspaceHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static GetStudentResultsWorkspaceHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private static readonly HashSet<string> _privilegedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Teacher",
        "AcademicCoordinator",
        "Academic Coordinator",
        "AcademicAuthority",
        "Academic Authority",
        "Admin",
        "Administrator"
    };

    private const string _selectStudentPublishedResultsSql = """
        SELECT
            r.id AS ResultId,
            r.assessment_id AS AssessmentId,
            a.title AS AssessmentTitle,
            a.assessment_type AS AssessmentType,
            u.id AS LearningUnitId,
            u.code AS LearningUnitCode,
            u.name AS LearningUnitTitle,
            r.attempt_number AS AttemptNumber,
            r.score_numeric AS ScoreNumeric,
            r.score_max AS ScoreMax,
            r.grade_letter AS GradeLetter,
            r.grade_label AS GradeLabel,
            r.narrative_feedback AS NarrativeFeedback,
            r.evaluation_model AS EvaluationModel,
            r.evaluated_date AS EvaluatedDate,
            r.published_date AS PublishedDate
        FROM academics.academic_results r
        INNER JOIN academics.assessments a ON a.id = r.assessment_id
        INNER JOIN academics.learning_units u ON u.id = a.learning_unit_id
        WHERE r.student_id = @StudentId
          AND r.is_final = 1
          AND r.result_status = 'published'
          AND EXISTS (
              SELECT 1
              FROM academics.academic_participations p
              WHERE p.learning_unit_id = a.learning_unit_id
                AND p.student_id = r.student_id
                AND p.academic_period_id = @AcademicPeriodId
          )
        ORDER BY u.name ASC, a.title ASC, r.attempt_number ASC;
        """;

    public async Task<Result<IReadOnlyList<StudentPublishedResultDto>>> HandleAsync(
        Guid academicPeriodId,
        Guid? studentIdOverride = null,
        CancellationToken cancellationToken = default)
    {
        if (academicPeriodId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<StudentPublishedResultDto>>(
                DomainError.Validation(
                    "Academic.AcademicPeriodIdRequired",
                    "Academic period ID is required."));
        }

        var isPrivileged = currentUser.Roles.Any(r => _privilegedRoles.Contains(r));
        var isParent = currentUser.Roles.Any(r => r.Equals("Parent", StringComparison.OrdinalIgnoreCase));

        Guid effectiveStudentId;
        if (currentUser.StudentId.HasValue)
        {
            if (studentIdOverride.HasValue && studentIdOverride.Value != currentUser.StudentId.Value && !isPrivileged)
            {
                return Result.Failure<IReadOnlyList<StudentPublishedResultDto>>(
                    DomainError.Forbidden(
                        "Academic.UnauthorizedStudentWorkspace",
                        "Students are only authorized to access their own results workspace."));
            }

            effectiveStudentId = currentUser.StudentId.Value;
        }
        else if (isPrivileged || isParent)
        {
            if (!studentIdOverride.HasValue || studentIdOverride.Value == Guid.Empty)
            {
                return Result.Failure<IReadOnlyList<StudentPublishedResultDto>>(
                    DomainError.Validation(
                        "Academic.StudentIdRequired",
                        "Student ID must be specified when querying student results workspace as a staff member or parent."));
            }

            effectiveStudentId = studentIdOverride.Value;
        }
        else
        {
            return Result.Failure<IReadOnlyList<StudentPublishedResultDto>>(
                DomainError.Forbidden(
                    "Academic.UnauthorizedStudentWorkspace",
                    "The current user is not associated with an enrolled student."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var results = await connection.QueryAsync<StudentPublishedResultDto>(new CommandDefinition(
            _selectStudentPublishedResultsSql,
            new
            {
                StudentId = effectiveStudentId,
                AcademicPeriodId = academicPeriodId
            },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<StudentPublishedResultDto>>(results.AsList());
    }
}
