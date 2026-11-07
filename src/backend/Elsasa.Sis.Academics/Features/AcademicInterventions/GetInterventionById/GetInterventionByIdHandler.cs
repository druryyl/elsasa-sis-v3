namespace Elsasa.Sis.Academics.Features.AcademicInterventions.GetInterventionById;

using Dapper;
using Elsasa.Sis.Academics.Features.AcademicInterventions;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for retrieving detailed information for an academic intervention (UC-ACD-11, P4-S10, TD-04).
/// </summary>
public sealed class GetInterventionByIdHandler(ISqlConnectionFactory connectionFactory)
{
    static GetInterventionByIdHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectInterventionByIdSql = """
        SELECT
            i.id AS Id,
            i.student_id AS StudentId,
            s.student_number AS StudentNumber,
            s.full_name AS StudentName,
            s.email AS StudentEmail,
            lu.level AS GradeLevel,
            i.learning_unit_id AS LearningUnitId,
            lu.code AS LearningUnitCode,
            lu.name AS LearningUnitName,
            i.academic_period_id AS AcademicPeriodId,
            i.intervention_type AS InterventionType,
            i.trigger_type AS TriggerType,
            i.trigger_description AS TriggerDescription,
            i.plan_description AS PlanDescription,
            i.status AS Status,
            i.initiated_by AS InitiatedBy,
            i.initiated_at AS InitiatedAt,
            i.concluded_at AS ConcludedAt,
            i.outcome_notes AS OutcomeNotes,
            i.created_at AS CreatedAt,
            i.created_by AS CreatedBy,
            i.updated_at AS UpdatedAt,
            i.updated_by AS UpdatedBy
        FROM academics.academic_interventions i
        LEFT JOIN students.students s ON s.id = i.student_id
        LEFT JOIN academics.learning_units lu ON lu.id = i.learning_unit_id
        WHERE i.id = @Id;
        """;

    /// <summary>
    /// Executes the detailed intervention lookup.
    /// </summary>
    /// <param name="id">The unique intervention identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed intervention record or not-found error.</returns>
    public async Task<Result<AcademicInterventionDetailDto>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var dto = await connection.QuerySingleOrDefaultAsync<AcademicInterventionDetailDto>(new CommandDefinition(
            _selectInterventionByIdSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (dto is null)
        {
            return Result.Failure<AcademicInterventionDetailDto>(
                DomainError.NotFound(
                    "Academic.InterventionNotFound",
                    $"Academic intervention with ID '{id}' was not found."));
        }

        return Result.Success(dto);
    }
}
