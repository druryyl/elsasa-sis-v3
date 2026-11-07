namespace Elsasa.Sis.Academics.Features.AcademicStandings.GetStudentStandings;

using Dapper;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying historical academic standing determinations for a student ordered by academic period (UC-ACD-17, P3-S09).
/// </summary>
public sealed class GetStudentStandingsHandler(ISqlConnectionFactory connectionFactory)
{
    static GetStudentStandingsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectStudentStandingsSql = """
        SELECT
            ast.id AS Id,
            ast.student_id AS StudentId,
            ast.academic_period_id AS AcademicPeriodId,
            ast.standing AS Standing,
            ast.policy_reference AS PolicyReference,
            ast.conditions AS Conditions,
            ast.is_exception AS IsException,
            ast.exception_reason AS ExceptionReason,
            ast.committee_reference AS CommitteeReference,
            ast.authorized_by AS AuthorizedBy,
            ast.determined_at AS DeterminedAt
        FROM academics.academic_standings ast
        LEFT JOIN operations.operational_periods op ON op.id = ast.academic_period_id
        WHERE ast.student_id = @StudentId
        ORDER BY COALESCE(op.start_date, CAST(ast.determined_at AS DATE)) ASC, ast.determined_at ASC;
        """;

    /// <summary>
    /// Executes query returning historical standing determinations for a student.
    /// </summary>
    /// <param name="studentId">The student identifier (required).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of student academic standing determinations ordered by academic period.</returns>
    public async Task<Result<IReadOnlyList<StudentAcademicStandingDto>>> HandleAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        if (studentId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<StudentAcademicStandingDto>>(
                DomainError.Validation("Academics.StudentIdRequired", "Student ID is required."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var standings = await connection.QueryAsync<StudentAcademicStandingDto>(new CommandDefinition(
            _selectStudentStandingsSql,
            new { StudentId = studentId },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<StudentAcademicStandingDto>>(standings.AsList());
    }
}
