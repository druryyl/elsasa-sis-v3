namespace Elsasa.Sis.Academics.Features.AcademicParticipations.UpdateParticipationStatus;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler executing participation lifecycle status transitions (P2-S05, GAP-08, UC-ACD-11).
/// Rejects updates if already completed, automatically stamps end date when transitioning to withdrawn,
/// and enforces active participation uniqueness.
/// </summary>
public sealed class UpdateParticipationStatusHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static UpdateParticipationStatusHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectParticipationSql = """
        SELECT
            id AS Id,
            student_id AS StudentId,
            learning_unit_id AS LearningUnitId,
            academic_period_id AS AcademicPeriodId,
            academic_period_label AS AcademicPeriodLabel,
            participation_type AS ParticipationType,
            attempt_number AS AttemptNumber,
            section_label AS SectionLabel,
            status AS Status,
            start_date AS StartDate,
            end_date AS EndDate,
            completion_date AS CompletionDate,
            notes AS Notes
        FROM academics.academic_participations
        WHERE id = @Id;
        """;

    private const string _checkActiveConflictSql = """
        SELECT COUNT(1)
        FROM academics.academic_participations
        WHERE student_id = @StudentId
          AND learning_unit_id = @LearningUnitId
          AND academic_period_id = @AcademicPeriodId
          AND status = 'in_progress'
          AND id != @Id;
        """;

    private const string _updateParticipationSql = """
        UPDATE academics.academic_participations
        SET
            status = @NewStatus,
            end_date = @EndDate,
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
        string AcademicPeriodLabel,
        string ParticipationType,
        int AttemptNumber,
        string? SectionLabel,
        string Status,
        DateOnly StartDate,
        DateOnly? EndDate,
        DateOnly? CompletionDate,
        string? Notes);

    /// <summary>
    /// Executes participation status update.
    /// </summary>
    /// <param name="id">Participation identifier.</param>
    /// <param name="request">Status transition request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated participation response or domain error.</returns>
    public async Task<Result<UpdateParticipationStatusResponse>> HandleAsync(
        Guid id,
        UpdateParticipationStatusRequest request,
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
            return Result.Failure<UpdateParticipationStatusResponse>(
                DomainError.NotFound(
                    "Academic.ParticipationNotFound",
                    $"Academic participation with ID '{id}' was not found."));
        }

        // 2. Reject transition if participation is already completed
        if (string.Equals(existing.Status, "completed", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<UpdateParticipationStatusResponse>(
                DomainError.Conflict(
                    "Academic.ParticipationAlreadyCompleted",
                    "Cannot update status of an already completed academic participation."));
        }

        var normalizedNewStatus = request.NewStatus.Trim().ToLowerInvariant();

        // 3. Prevent duplicate active participation if resuming to in_progress
        if (string.Equals(normalizedNewStatus, "in_progress", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(existing.Status, "in_progress", StringComparison.OrdinalIgnoreCase))
        {
            var activeCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                _checkActiveConflictSql,
                new
                {
                    existing.StudentId,
                    existing.LearningUnitId,
                    existing.AcademicPeriodId,
                    Id = id
                },
                cancellationToken: cancellationToken));

            if (activeCount > 0)
            {
                return Result.Failure<UpdateParticipationStatusResponse>(
                    DomainError.Conflict(
                        "Academic.DuplicateActiveParticipation",
                        "Student already has an active participation in this learning unit for this period."));
            }
        }

        // 4. If transitioning to withdrawn, sets end_date = CURRENT_TIMESTAMP / current UTC date
        DateOnly? finalEndDate = existing.EndDate;
        if (string.Equals(normalizedNewStatus, "withdrawn", StringComparison.OrdinalIgnoreCase))
        {
            finalEndDate = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        var finalNotes = request.Notes ?? existing.Notes;
        var updatedAt = DateTimeOffset.UtcNow;
        var updatedBy = currentUser.Username ?? currentUser.Email ?? "system";

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                _updateParticipationSql,
                new
                {
                    Id = id,
                    NewStatus = normalizedNewStatus,
                    EndDate = finalEndDate,
                    Notes = finalNotes,
                    UpdatedAt = updatedAt,
                    UpdatedBy = updatedBy
                },
                cancellationToken: cancellationToken));
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            return Result.Failure<UpdateParticipationStatusResponse>(
                DomainError.Conflict(
                    "Academic.DuplicateActiveParticipation",
                    "Student already has an active participation in this learning unit for this period."));
        }

        return Result.Success(new UpdateParticipationStatusResponse(
            existing.Id,
            existing.StudentId,
            existing.LearningUnitId,
            existing.AcademicPeriodId,
            existing.AcademicPeriodLabel,
            existing.ParticipationType,
            existing.AttemptNumber,
            existing.SectionLabel,
            normalizedNewStatus,
            existing.StartDate,
            finalEndDate,
            existing.CompletionDate,
            finalNotes,
            updatedAt,
            updatedBy));
    }
}
