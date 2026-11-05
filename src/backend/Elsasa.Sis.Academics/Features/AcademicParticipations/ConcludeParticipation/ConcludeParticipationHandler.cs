namespace Elsasa.Sis.Academics.Features.AcademicParticipations.ConcludeParticipation;

using Dapper;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler executing formal conclusion of active academic participation (P2-S05, GAP-08, UC-ACD-12).
/// Verifies 'in_progress' status, updates status to 'completed', records completion and end dates,
/// and publishes ParticipationConcludedEvent post-commit.
/// </summary>
public sealed class ConcludeParticipationHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static ConcludeParticipationHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectParticipationSql = """
        SELECT
            id AS Id,
            student_id AS StudentId,
            learning_unit_id AS LearningUnitId,
            academic_period_id AS AcademicPeriodId,
            status AS Status,
            start_date AS StartDate,
            notes AS Notes
        FROM academics.academic_participations
        WHERE id = @Id;
        """;

    private const string _updateParticipationSql = """
        UPDATE academics.academic_participations
        SET
            status = 'completed',
            completion_date = @CompletionDate,
            end_date = @CompletionDate,
            notes = @Notes,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private sealed record ParticipationRow(
        Guid Id,
        Guid StudentId,
        Guid LearningUnitId,
        Guid AcademicPeriodId,
        string Status,
        DateOnly StartDate,
        string? Notes);

    /// <summary>
    /// Executes participation conclusion workflow.
    /// </summary>
    /// <param name="id">Participation identifier.</param>
    /// <param name="request">Conclusion details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Concluded participation details or domain error.</returns>
    public async Task<Result<ConcludeParticipationResponse>> HandleAsync(
        Guid id,
        ConcludeParticipationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify participation exists
        var existing = await connection.QuerySingleOrDefaultAsync<ParticipationRow>(new CommandDefinition(
            _selectParticipationSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<ConcludeParticipationResponse>(
                DomainError.NotFound(
                    "Academic.ParticipationNotFound",
                    $"Academic participation with ID '{id}' was not found."));
        }

        // 2. Verify participation is in 'in_progress' status
        if (!string.Equals(existing.Status, "in_progress", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ConcludeParticipationResponse>(
                DomainError.Conflict(
                    "Academic.ParticipationNotInProgress",
                    $"Only participations in 'in_progress' status can be concluded. Current status: '{existing.Status}'."));
        }

        // 3. Verify completion date does not precede start date
        if (request.CompletionDate < existing.StartDate)
        {
            return Result.Failure<ConcludeParticipationResponse>(
                DomainError.Validation(
                    "Academic.CompletionDateBeforeStartDate",
                    "Completion date cannot precede participation start date."));
        }

        var finalNotes = request.Notes ?? existing.Notes;
        var updatedAt = DateTimeOffset.UtcNow;
        var updatedBy = currentUser.Username ?? currentUser.Email ?? "system";

        // 4. Update status and completion timestamps
        await connection.ExecuteAsync(new CommandDefinition(
            _updateParticipationSql,
            new
            {
                Id = id,
                request.CompletionDate,
                Notes = finalNotes,
                UpdatedAt = updatedAt,
                UpdatedBy = updatedBy
            },
            cancellationToken: cancellationToken));

        // 5. Publish post-commit domain event (ADR-003, TD-08, UC-ACD-12)
        await publisher.Publish(
            new ParticipationConcludedEvent(
                existing.Id,
                existing.StudentId,
                existing.LearningUnitId,
                request.CompletionDate),
            cancellationToken);

        return Result.Success(new ConcludeParticipationResponse(
            existing.Id,
            existing.StudentId,
            existing.LearningUnitId,
            existing.AcademicPeriodId,
            "completed",
            existing.StartDate,
            request.CompletionDate,
            request.CompletionDate,
            finalNotes,
            updatedAt,
            updatedBy));
    }
}
