namespace Elsasa.Sis.Academics.Features.AcademicAchievements.GetPeriodAchievements;

using Dapper;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying all academic honors and achievements conferred across all students for a period (UC-ACD-21, P4-S11).
/// </summary>
public sealed class GetPeriodAchievementsHandler(ISqlConnectionFactory connectionFactory)
{
    static GetPeriodAchievementsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectPeriodAchievementsSql = """
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
        WHERE academic_period_id = @AcademicPeriodId
        ORDER BY recognized_at DESC, created_at DESC;
        """;

    /// <summary>
    /// Executes query returning achievements conferred for the academic period.
    /// </summary>
    public async Task<Result<IReadOnlyList<AcademicAchievementDto>>> HandleAsync(
        Guid academicPeriodId,
        CancellationToken cancellationToken = default)
    {
        if (academicPeriodId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<AcademicAchievementDto>>(
                DomainError.Validation("Academic.EmptyPeriodId", "AcademicPeriodId cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var achievements = await connection.QueryAsync<AcademicAchievementDto>(
            new CommandDefinition(
                _selectPeriodAchievementsSql,
                new { AcademicPeriodId = academicPeriodId },
                cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<AcademicAchievementDto>>(achievements.AsList());
    }
}
