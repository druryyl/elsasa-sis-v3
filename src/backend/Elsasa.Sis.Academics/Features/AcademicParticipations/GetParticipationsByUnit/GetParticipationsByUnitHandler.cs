namespace Elsasa.Sis.Academics.Features.AcademicParticipations.GetParticipationsByUnit;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying participating students roster for a learning unit (ADR-004, ADR-005, P2-S06, TD-08).
/// </summary>
public sealed class GetParticipationsByUnitHandler(ISqlConnectionFactory connectionFactory)
{
    static GetParticipationsByUnitHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkUnitSql = """
        SELECT COUNT(1)
        FROM academics.learning_units
        WHERE id = @LearningUnitId;
        """;

    private const string _selectParticipationsSql = """
        SELECT
            p.id AS Id,
            p.student_id AS StudentId,
            s.student_number AS StudentNumber,
            s.full_name AS StudentName,
            p.learning_unit_id AS LearningUnitId,
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
        LEFT JOIN students.students s ON s.id = p.student_id
        WHERE p.learning_unit_id = @LearningUnitId
          AND (@AcademicPeriodId IS NULL OR p.academic_period_id = @AcademicPeriodId)
          AND (@SectionLabel IS NULL OR p.section_label = @SectionLabel)
          AND (@Status IS NULL OR p.status = @Status)
        ORDER BY p.section_label ASC, COALESCE(s.full_name, '') ASC, p.attempt_number ASC, p.created_at ASC;
        """;

    /// <summary>
    /// Executes learning unit participations query with optional academic period, section, and status filters.
    /// </summary>
    /// <param name="learningUnitId">The unique identifier of the target learning unit.</param>
    /// <param name="academicPeriodId">Optional academic period filter.</param>
    /// <param name="sectionLabel">Optional cohort section label filter.</param>
    /// <param name="status">Optional participation lifecycle status filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Roster of participating students, or a domain error.</returns>
    public async Task<Result<IReadOnlyList<UnitParticipationItemDto>>> HandleAsync(
        Guid learningUnitId,
        Guid? academicPeriodId,
        string? sectionLabel,
        string? status,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var unitExists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _checkUnitSql,
            new { LearningUnitId = learningUnitId },
            cancellationToken: cancellationToken));

        if (unitExists == 0)
        {
            return Result.Failure<IReadOnlyList<UnitParticipationItemDto>>(
                DomainError.NotFound(
                    "Academics.LearningUnitNotFound",
                    $"Learning unit with ID '{learningUnitId}' was not found."));
        }

        var participations = await connection.QueryAsync<UnitParticipationItemDto>(new CommandDefinition(
            _selectParticipationsSql,
            new
            {
                LearningUnitId = learningUnitId,
                AcademicPeriodId = academicPeriodId,
                SectionLabel = string.IsNullOrWhiteSpace(sectionLabel) ? null : sectionLabel.Trim(),
                Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToLowerInvariant()
            },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<UnitParticipationItemDto>>(participations.AsList());
    }
}
