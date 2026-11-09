namespace Elsasa.Sis.Academics.Features.AcademicInterventions.GetActiveInterventions;

using Dapper;
using Elsasa.Sis.Academics.Features.AcademicInterventions;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for retrieving active and historical academic interventions (UC-ACD-11, P4-S10, TD-04).
/// </summary>
public sealed class GetActiveInterventionsHandler(ISqlConnectionFactory connectionFactory)
{
    static GetActiveInterventionsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectInterventionsSql = """
        SELECT
            i.id AS Id,
            i.student_id AS StudentId,
            s.student_number AS StudentNumber,
            s.full_name AS StudentName,
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
        WHERE (@AcademicPeriodId IS NULL OR i.academic_period_id = @AcademicPeriodId)
          AND (@Status IS NULL OR i.status = @Status)
          AND (@StudentId IS NULL OR i.student_id = @StudentId)
        ORDER BY i.initiated_at DESC, i.created_at DESC;
        """;

    /// <summary>
    /// Executes the interventions query with optional period, status, and student filters.
    /// </summary>
    /// <param name="academicPeriodId">Optional academic period identifier.</param>
    /// <param name="status">Optional status filter ('planned', 'in_progress', 'completed', 'cancelled').</param>
    /// <param name="studentId">Optional student identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of academic intervention DTOs.</returns>
    public async Task<Result<IReadOnlyList<AcademicInterventionDto>>> HandleAsync(
        Guid? academicPeriodId,
        string? status,
        Guid? studentId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<AcademicInterventionDto>(new CommandDefinition(
            _selectInterventionsSql,
            new
            {
                AcademicPeriodId = academicPeriodId,
                Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
                StudentId = studentId
            },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<AcademicInterventionDto>>(rows.AsList());
    }
}
