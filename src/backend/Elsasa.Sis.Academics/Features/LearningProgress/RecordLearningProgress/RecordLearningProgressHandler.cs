namespace Elsasa.Sis.Academics.Features.LearningProgress.RecordLearningProgress;

using Dapper;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler executing recording or updating of student learning progress (P2-S04, UC-ACD-12).
/// Supports explicit completion date stamping for out-of-period completion and authorized transfer recognition.
/// </summary>
public sealed class RecordLearningProgressHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static RecordLearningProgressHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectUnitSql = """
        SELECT id
        FROM academics.learning_units
        WHERE id = @LearningUnitId;
        """;

    private const string _upsertProgressSql = """
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
                status = @Status,
                basis = @Basis,
                completion_date = @CompletionDate,
                notes = @Notes,
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
                @NewId,
                @StudentId,
                @LearningUnitId,
                @AcademicPeriodId,
                @Status,
                @Basis,
                @CompletionDate,
                @Notes,
                @Now,
                @CurrentUser
            )
        OUTPUT
            inserted.id AS Id,
            inserted.student_id AS StudentId,
            inserted.learning_unit_id AS LearningUnitId,
            inserted.academic_period_id AS AcademicPeriodId,
            inserted.status AS Status,
            inserted.basis AS Basis,
            inserted.completion_date AS CompletionDate,
            inserted.notes AS Notes,
            inserted.created_at AS CreatedAt,
            inserted.created_by AS CreatedBy,
            inserted.updated_at AS UpdatedAt,
            inserted.updated_by AS UpdatedBy;
        """;

    private sealed record LearningProgressRow(
        Guid Id,
        Guid StudentId,
        Guid LearningUnitId,
        Guid AcademicPeriodId,
        string Status,
        string Basis,
        DateOnly? CompletionDate,
        string? Notes,
        DateTimeOffset CreatedAt,
        string CreatedBy,
        DateTimeOffset? UpdatedAt,
        string? UpdatedBy);

    /// <summary>
    /// Executes learning progress recording workflow.
    /// </summary>
    /// <param name="command">Command payload containing progress attributes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing recorded progress details or domain error.</returns>
    public async Task<Result<RecordLearningProgressResponse>> HandleAsync(
        RecordLearningProgressCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var normalizedStatus = command.Status.Trim().ToLowerInvariant();
        var normalizedBasis = command.Basis.Trim().ToLowerInvariant();

        // Business rules:
        // If status = 'completed', requires an explicit completion_date (defaults to current date if omitted).
        // If status != 'completed', completion_date must be set to NULL.
        DateOnly? effectiveCompletionDate = normalizedStatus == "completed"
            ? (command.CompletionDate ?? DateOnly.FromDateTime(DateTime.UtcNow))
            : null;

        // If basis = 'authorized_decision', explanatory Notes is mandatory.
        if (normalizedBasis == "authorized_decision" && string.IsNullOrWhiteSpace(command.Notes))
        {
            return Result.Failure<RecordLearningProgressResponse>(
                DomainError.Validation(
                    "Academic.ProgressNotesRequired",
                    "Explanatory notes are required when basis is 'authorized_decision'."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // Verify learning unit exists
        var unitExists = await connection.ExecuteScalarAsync<Guid?>(
            new CommandDefinition(
                _selectUnitSql,
                new { command.LearningUnitId },
                cancellationToken: cancellationToken));

        if (unitExists is null)
        {
            return Result.Failure<RecordLearningProgressResponse>(
                DomainError.NotFound(
                    "Academic.LearningUnitNotFound",
                    $"Learning unit with ID '{command.LearningUnitId}' was not found."));
        }

        var now = DateTimeOffset.UtcNow;
        var currentUserIdentity = currentUser.Username ?? currentUser.Email ?? "system";
        var newId = Guid.NewGuid();

        var row = await connection.QuerySingleAsync<LearningProgressRow>(
            new CommandDefinition(
                _upsertProgressSql,
                new
                {
                    NewId = newId,
                    command.StudentId,
                    command.LearningUnitId,
                    command.AcademicPeriodId,
                    Status = normalizedStatus,
                    Basis = normalizedBasis,
                    CompletionDate = effectiveCompletionDate,
                    command.Notes,
                    Now = now,
                    CurrentUser = currentUserIdentity
                },
                cancellationToken: cancellationToken));

        // Post-commit: Publish LearningProgressUpdatedEvent
        await publisher.Publish(
            new LearningProgressUpdatedEvent(
                row.Id,
                row.StudentId,
                row.LearningUnitId,
                row.AcademicPeriodId,
                row.Status,
                row.CompletionDate),
            cancellationToken);

        return Result.Success(new RecordLearningProgressResponse(
            row.Id,
            row.StudentId,
            row.LearningUnitId,
            row.AcademicPeriodId,
            row.Status,
            row.Basis,
            row.CompletionDate,
            row.Notes,
            row.CreatedAt,
            row.CreatedBy,
            row.UpdatedAt,
            row.UpdatedBy));
    }
}
