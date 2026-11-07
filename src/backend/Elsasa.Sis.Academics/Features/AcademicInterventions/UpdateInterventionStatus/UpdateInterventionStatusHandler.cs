namespace Elsasa.Sis.Academics.Features.AcademicInterventions.UpdateInterventionStatus;

using Dapper;
using Elsasa.Sis.Academics.Features.AcademicInterventions;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler executing status progression for an academic intervention (UC-ACD-11, P4-S10, TD-04).
/// Updates lifecycle status, stamps concluded_at timestamp when terminal, and emits AcademicInterventionConcludedEvent post-commit.
/// </summary>
public sealed class UpdateInterventionStatusHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static UpdateInterventionStatusHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectExistingSql = """
        SELECT
            id AS Id,
            student_id AS StudentId,
            status AS Status,
            outcome_notes AS OutcomeNotes
        FROM academics.academic_interventions
        WHERE id = @Id;
        """;

    private const string _updateInterventionSql = """
        UPDATE academics.academic_interventions
        SET
            status = @Status,
            concluded_at = @ConcludedAt,
            outcome_notes = COALESCE(@OutcomeNotes, outcome_notes),
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
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

    private sealed record ExistingInterventionRow(
        Guid Id,
        Guid StudentId,
        string Status,
        string? OutcomeNotes);

    /// <summary>
    /// Executes the intervention status transition workflow.
    /// </summary>
    /// <param name="command">Status update parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated academic intervention DTO or domain error.</returns>
    public async Task<Result<AcademicInterventionDto>> HandleAsync(
        UpdateInterventionStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify existing intervention
        var existing = await connection.QuerySingleOrDefaultAsync<ExistingInterventionRow>(new CommandDefinition(
            _selectExistingSql,
            new { command.Id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<AcademicInterventionDto>(
                DomainError.NotFound(
                    "Academic.InterventionNotFound",
                    $"Academic intervention with ID '{command.Id}' was not found."));
        }

        var isTerminal = string.Equals(command.Status, "completed", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(command.Status, "cancelled", StringComparison.OrdinalIgnoreCase);

        var now = DateTimeOffset.UtcNow;
        DateTimeOffset? concludedAt = isTerminal ? now : null;
        var updatedBy = currentUser.Username ?? currentUser.Email ?? "system";

        // 2. Execute update
        await connection.ExecuteAsync(new CommandDefinition(
            _updateInterventionSql,
            new
            {
                command.Id,
                Status = command.Status.ToLowerInvariant(),
                ConcludedAt = concludedAt,
                command.OutcomeNotes,
                UpdatedAt = now,
                UpdatedBy = updatedBy
            },
            cancellationToken: cancellationToken));

        // 3. Publish post-commit domain event if completed or cancelled (ADR-003, UC-ACD-11)
        if (isTerminal)
        {
            var notesToPublish = command.OutcomeNotes ?? existing.OutcomeNotes ?? string.Empty;
            await publisher.Publish(
                new AcademicInterventionConcludedEvent(
                    existing.Id,
                    existing.StudentId,
                    notesToPublish,
                    now),
                cancellationToken);
        }

        // 4. Return updated DTO
        var dto = await connection.QuerySingleAsync<AcademicInterventionDto>(new CommandDefinition(
            _selectInterventionDtoSql,
            new { command.Id },
            cancellationToken: cancellationToken));

        return Result.Success(dto);
    }
}
