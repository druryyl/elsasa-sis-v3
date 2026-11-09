namespace Elsasa.Sis.Academics.Features.AcademicInterventions.InitiateAcademicIntervention;

using Dapper;
using Elsasa.Sis.Academics.Features.AcademicInterventions;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler executing initiation of a new academic intervention (UC-ACD-11, P4-S10, TD-04).
/// Inserts plan in 'planned' status, records initiator identity, and emits AcademicInterventionInitiatedEvent post-commit.
/// </summary>
public sealed class InitiateAcademicInterventionHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static InitiateAcademicInterventionHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkLearningUnitSql = """
        SELECT code, name
        FROM academics.learning_units
        WHERE id = @LearningUnitId;
        """;

    private const string _insertInterventionSql = """
        INSERT INTO academics.academic_interventions (
            id,
            student_id,
            learning_unit_id,
            academic_period_id,
            intervention_type,
            trigger_type,
            trigger_description,
            plan_description,
            status,
            initiated_by,
            initiated_at,
            concluded_at,
            outcome_notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @StudentId,
            @LearningUnitId,
            @AcademicPeriodId,
            @InterventionType,
            @TriggerType,
            @TriggerDescription,
            @PlanDescription,
            'planned',
            @InitiatedBy,
            @InitiatedAt,
            NULL,
            NULL,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private const string _selectInterventionDtoSql = """
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
        WHERE i.id = @Id;
        """;

    private sealed record LearningUnitRow(string Code, string Name);

    /// <summary>
    /// Executes the academic intervention initiation workflow.
    /// </summary>
    /// <param name="command">The intervention command details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The initiated academic intervention DTO.</returns>
    public async Task<Result<AcademicInterventionDto>> HandleAsync(
        InitiateAcademicInterventionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify target learning unit exists
        var learningUnit = await connection.QuerySingleOrDefaultAsync<LearningUnitRow>(new CommandDefinition(
            _checkLearningUnitSql,
            new { command.LearningUnitId },
            cancellationToken: cancellationToken));

        if (learningUnit is null)
        {
            return Result.Failure<AcademicInterventionDto>(
                DomainError.NotFound(
                    "Academic.LearningUnitNotFound",
                    $"Target learning unit with ID '{command.LearningUnitId}' was not found."));
        }

        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var initiatedBy = currentUser.Username ?? currentUser.Email ?? "system";

        // 2. Insert intervention record
        await connection.ExecuteAsync(new CommandDefinition(
            _insertInterventionSql,
            new
            {
                Id = id,
                command.StudentId,
                command.LearningUnitId,
                command.AcademicPeriodId,
                command.InterventionType,
                command.TriggerType,
                command.TriggerDescription,
                command.PlanDescription,
                InitiatedBy = initiatedBy,
                InitiatedAt = now,
                CreatedAt = now,
                CreatedBy = initiatedBy
            },
            cancellationToken: cancellationToken));

        // 3. Publish post-commit in-memory domain event (ADR-003, UC-ACD-11)
        await publisher.Publish(
            new AcademicInterventionInitiatedEvent(
                id,
                command.StudentId,
                command.LearningUnitId,
                command.InterventionType,
                command.TriggerType,
                now),
            cancellationToken);

        // 4. Return complete DTO
        var dto = await connection.QuerySingleAsync<AcademicInterventionDto>(new CommandDefinition(
            _selectInterventionDtoSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        return Result.Success(dto);
    }
}
