namespace Elsasa.Sis.Academics.Features.AcademicAchievements.GetStudentAchievements;

using Dapper;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying academic honors and achievements conferred on a student (UC-ACD-21, P4-S11).
/// </summary>
public sealed class GetStudentAchievementsHandler(ISqlConnectionFactory connectionFactory)
{
    static GetStudentAchievementsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectStudentAchievementsSql = """
        SELECT
            id AS Id,
            student_id AS StudentId,
            learning_unit_id AS LearningUnitId,
            academic_period_id AS AcademicPeriodId,
            achievement_type AS AchievementType,
            title AS Title,
            description AS Description,
            criteria_met AS CriteriaMet,
            supporting_evidence AS SupportingEvidence,
            recognized_by AS RecognizedBy,
            recognized_at AS RecognizedAt
        FROM academics.academic_achievements
        WHERE student_id = @StudentId
          AND (@AcademicPeriodId IS NULL OR academic_period_id = @AcademicPeriodId)
        ORDER BY recognized_at DESC, created_at DESC;
        """;

    /// <summary>
    /// Executes query returning achievements earned by the student, optionally filtered by academic period.
    /// </summary>
    public async Task<Result<IReadOnlyList<AcademicAchievementDto>>> HandleAsync(
        Guid studentId,
        Guid? academicPeriodId = null,
        CancellationToken cancellationToken = default)
    {
        if (studentId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<AcademicAchievementDto>>(
                DomainError.Validation("Academic.EmptyStudentId", "StudentId cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var achievements = await connection.QueryAsync<AcademicAchievementDto>(
            new CommandDefinition(
                _selectStudentAchievementsSql,
                new { StudentId = studentId, AcademicPeriodId = academicPeriodId },
                cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<AcademicAchievementDto>>(achievements.AsList());
    }
}
