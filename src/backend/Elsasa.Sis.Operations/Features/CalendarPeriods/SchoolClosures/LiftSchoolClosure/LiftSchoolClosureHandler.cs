namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolClosures.LiftSchoolClosure;

using Dapper;
using Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolClosures;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing the lifting of an existing school closure (TD-05, UC-OPS-12).
/// </summary>
public sealed class LiftSchoolClosureHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectClosureSql = """
        SELECT
            id AS Id,
            academic_year_id AS AcademicYearId,
            closure_number AS ClosureNumber,
            reason_category AS ReasonCategory,
            reason_details AS ReasonDetails,
            start_date AS StartDate,
            end_date AS EndDate,
            affected_campus AS AffectedCampus,
            affected_population AS AffectedPopulation,
            status AS Status,
            authorized_by AS AuthorizedBy,
            authorized_date AS AuthorizedDate,
            communication_status AS CommunicationStatus,
            recovery_plan_notes AS RecoveryPlanNotes,
            created_at AS CreatedAt,
            created_by AS CreatedBy,
            updated_at AS UpdatedAt,
            updated_by AS UpdatedBy
        FROM operations.school_closures
        WHERE id = @Id;
        """;

    private const string _updateClosureSql = """
        UPDATE operations.school_closures
        SET status = 'lifted',
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private const string _updateCalendarEntriesSql = """
        UPDATE operations.calendar_entries
        SET name = CASE 
                WHEN name LIKE '%(Lifted)%' THEN name 
                ELSE name + ' (Lifted)' 
            END,
            description = CASE 
                WHEN description IS NULL OR description = '' THEN 'Closure lifted'
                WHEN description LIKE '%(Closure lifted)%' THEN description
                ELSE description + ' (Closure lifted)'
            END
        WHERE closure_id = @ClosureId;
        """;

    /// <summary>
    /// Lifts an active school closure, updating its status to 'lifted' and annotating linked calendar entries (TD-05, UC-OPS-12).
    /// </summary>
    public async Task<Result<SchoolClosureResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var existing = await connection.QuerySingleOrDefaultAsync<SchoolClosureResponse>(new CommandDefinition(
            _selectClosureSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<SchoolClosureResponse>(
                DomainError.NotFound(
                    "Operations.SchoolClosureNotFound",
                    $"School closure with ID '{id}' was not found."));
        }

        if (string.Equals(existing.Status, "lifted", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<SchoolClosureResponse>(
                DomainError.Conflict(
                    "Operations.SchoolClosureAlreadyLifted",
                    $"School closure '{existing.ClosureNumber}' has already been lifted."));
        }

        var updatedAt = DateTimeOffset.UtcNow;
        var updatedBy = currentUser.Username ?? "system";

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // 1. Update closure record
        await connection.ExecuteAsync(new CommandDefinition(
            _updateClosureSql,
            new
            {
                Id = id,
                UpdatedAt = updatedAt,
                UpdatedBy = updatedBy,
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 2. Update linked calendar entries
        await connection.ExecuteAsync(new CommandDefinition(
            _updateCalendarEntriesSql,
            new { ClosureId = id },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        var updated = existing with
        {
            Status = "lifted",
            UpdatedAt = updatedAt,
            UpdatedBy = updatedBy,
        };

        return Result.Success(updated);
    }
}
