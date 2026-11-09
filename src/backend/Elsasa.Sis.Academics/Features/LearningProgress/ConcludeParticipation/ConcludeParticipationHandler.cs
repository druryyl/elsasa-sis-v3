namespace Elsasa.Sis.Academics.Features.LearningProgress.ConcludeParticipation;

using Dapper;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler executing formal conclusion of academic participation and recording learning progress completion (P2-S04, UC-ACD-12).
/// Verifies active participation, checks operative result passing criteria (KKM), updates participation status to completed,
/// upserts learning progress to completed with assessment_evidence basis, commits atomically, and publishes domain events.
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
        FROM academics.academic_participations WITH (UPDLOCK, ROWLOCK)
        WHERE id = @ParticipationId;
        """;

    private const string _selectFailingOperativeResultsSql = """
        SELECT TOP 1
            a.title AS AssessmentTitle,
            a.passing_score AS PassingScore,
            r.score_numeric AS ScoreNumeric
        FROM academics.academic_results r
        INNER JOIN academics.assessments a ON a.id = r.assessment_id
        WHERE a.learning_unit_id = @LearningUnitId
          AND r.student_id = @StudentId
          AND r.is_final = 1
          AND r.result_status IN ('approved', 'published')
          AND a.status <> 'cancelled'
          AND a.passing_score IS NOT NULL
          AND r.score_numeric IS NOT NULL
          AND r.score_numeric < a.passing_score;
        """;

    private const string _updateParticipationSql = """
        UPDATE academics.academic_participations
        SET
            status = 'completed',
            completion_date = @CompletionDate,
            end_date = @CompletionDate,
            notes = COALESCE(@Notes, notes),
            updated_at = @Now,
            updated_by = @CurrentUser
        WHERE id = @ParticipationId;
        """;

    private const string _upsertLearningProgressSql = """
        MERGE INTO academics.learning_progress WITH (HOLDLOCK) AS target
        USING (
            SELECT
                @StudentId AS student_id,
                @LearningUnitId AS learning_unit_id,
                @AcademicPeriodId AS academic_period_id
        ) AS source
        ON target.student_id = source.student_id
           AND target.learning_unit_id = source.learning_unit_id
           AND target.academic_period_id = source.academic_period_id
        WHEN MATCHED THEN
            UPDATE SET
                status = 'completed',
                basis = 'assessment_evidence',
                completion_date = @CompletionDate,
                notes = COALESCE(@Notes, target.notes),
                updated_at = @Now,
                updated_by = @CurrentUser
        WHEN NOT MATCHED THEN
            INSERT (
                id,
                student_id,
                learning_unit_id,
                academic_period_id,
                status,
                basis,
                completion_date,
                notes,
                created_at,
                created_by
            )
            VALUES (
                @NewProgressId,
                @StudentId,
                @LearningUnitId,
                @AcademicPeriodId,
                'completed',
                'assessment_evidence',
                @CompletionDate,
                @Notes,
                @Now,
                @CurrentUser
            )
        OUTPUT
            inserted.id;
        """;

    private sealed record ParticipationRow(
        Guid Id,
        Guid StudentId,
        Guid LearningUnitId,
        Guid AcademicPeriodId,
        string Status,
        DateOnly StartDate,
        string? Notes);

    private sealed record FailingResultRow(
        string AssessmentTitle,
        decimal PassingScore,
        decimal ScoreNumeric);

    /// <summary>
    /// Executes participation conclusion and progress completion workflow within an atomic transaction.
    /// </summary>
    /// <param name="command">Command payload containing participation identifier and conclusion details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing concluded participation and progress details or domain error.</returns>
    public async Task<Result<ConcludeParticipationResponse>> HandleAsync(
        ConcludeParticipationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // 1. Verify active participation record in academics.academic_participations
        var participation = await connection.QuerySingleOrDefaultAsync<ParticipationRow>(
            new CommandDefinition(
                _selectParticipationSql,
                new { command.ParticipationId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (participation is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<ConcludeParticipationResponse>(
                DomainError.NotFound(
                    "Academic.ParticipationNotFound",
                    $"Academic participation with ID '{command.ParticipationId}' was not found."));
        }

        if (!string.Equals(participation.Status, "in_progress", StringComparison.OrdinalIgnoreCase))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<ConcludeParticipationResponse>(
                DomainError.Conflict(
                    "Academic.ParticipationNotInProgress",
                    $"Only participations in 'in_progress' status can be concluded. Current status: '{participation.Status}'."));
        }

        var effectiveCompletionDate = command.CompletionDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        // Verify completion date does not precede start date
        if (effectiveCompletionDate < participation.StartDate)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<ConcludeParticipationResponse>(
                DomainError.Validation(
                    "Academic.CompletionDateBeforeStartDate",
                    "Completion date cannot precede participation start date."));
        }

        // 2. Check operative result passing criteria (KKM)
        var failingResult = await connection.QueryFirstOrDefaultAsync<FailingResultRow>(
            new CommandDefinition(
                _selectFailingOperativeResultsSql,
                new
                {
                    LearningUnitId = participation.LearningUnitId,
                    StudentId = participation.StudentId
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (failingResult is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<ConcludeParticipationResponse>(
                DomainError.Conflict(
                    "Academic.PassingCriteriaNotMet",
                    $"Cannot conclude participation. Operative result ({failingResult.ScoreNumeric}) is below the required passing threshold (KKM {failingResult.PassingScore}) for assessment '{failingResult.AssessmentTitle}'."));
        }

        var now = DateTimeOffset.UtcNow;
        var currentUserIdentity = currentUser.Username ?? currentUser.Email ?? "system";
        var finalNotes = command.Notes ?? participation.Notes;

        // 3. Update academics.academic_participations.status to 'completed'
        await connection.ExecuteAsync(
            new CommandDefinition(
                _updateParticipationSql,
                new
                {
                    command.ParticipationId,
                    CompletionDate = effectiveCompletionDate,
                    Notes = finalNotes,
                    Now = now,
                    CurrentUser = currentUserIdentity
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        // 4. Insert or update academics.learning_progress to status = 'completed', basis = 'assessment_evidence'
        var newProgressId = Guid.NewGuid();
        var progressId = await connection.ExecuteScalarAsync<Guid>(
            new CommandDefinition(
                _upsertLearningProgressSql,
                new
                {
                    NewProgressId = newProgressId,
                    participation.StudentId,
                    participation.LearningUnitId,
                    participation.AcademicPeriodId,
                    CompletionDate = effectiveCompletionDate,
                    Notes = finalNotes,
                    Now = now,
                    CurrentUser = currentUserIdentity
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        // 5. Commit transaction
        await transaction.CommitAsync(cancellationToken);

        // 6. Post-commit: Publish domain events (ADR-003, UC-ACD-12)
        await publisher.Publish(
            new ParticipationConcludedEvent(
                participation.Id,
                participation.StudentId,
                participation.LearningUnitId,
                effectiveCompletionDate),
            cancellationToken);

        await publisher.Publish(
            new LearningProgressUpdatedEvent(
                progressId,
                participation.StudentId,
                participation.LearningUnitId,
                participation.AcademicPeriodId,
                "completed",
                effectiveCompletionDate),
            cancellationToken);

        return Result.Success(new ConcludeParticipationResponse(
            participation.Id,
            progressId,
            participation.StudentId,
            participation.LearningUnitId,
            participation.AcademicPeriodId,
            "completed",
            "completed",
            effectiveCompletionDate,
            finalNotes,
            now,
            currentUserIdentity));
    }
}
