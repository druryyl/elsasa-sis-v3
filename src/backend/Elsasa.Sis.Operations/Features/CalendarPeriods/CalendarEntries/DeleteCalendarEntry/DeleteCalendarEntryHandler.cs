namespace Elsasa.Sis.Operations.Features.CalendarPeriods.CalendarEntries.DeleteCalendarEntry;

using System.Text.Json;
using Dapper;
using Elsasa.Sis.Operations.Features.CalendarPeriods.Common;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing the removal of a calendar entry with automated amendment tracking (ADR-004, ADR-005, TD-05).
/// </summary>
public sealed class DeleteCalendarEntryHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectEntrySql = """
        SELECT
            e.id AS Id,
            e.calendar_id AS CalendarId,
            e.entry_date AS EntryDate,
            e.end_date AS EndDate,
            e.designation AS Designation,
            e.name AS Name,
            e.description AS Description,
            e.is_instructional AS IsInstructional,
            e.closure_id AS ClosureId,
            e.applies_to AS AppliesTo,
            c.version AS CalendarVersion,
            c.publication_status AS PublicationStatus
        FROM operations.calendar_entries e
        INNER JOIN operations.school_calendars c ON e.calendar_id = c.id
        WHERE e.id = @EntryId AND e.calendar_id = @CalendarId;
        """;

    private const string _clearAmendmentFkSql = """
        UPDATE operations.calendar_amendments
        SET affected_entry_id = NULL
        WHERE affected_entry_id = @EntryId;
        """;

    private const string _insertAmendmentSql = """
        INSERT INTO operations.calendar_amendments
        (
            id,
            calendar_id,
            amendment_number,
            from_version,
            to_version,
            amendment_type,
            affected_entry_id,
            change_summary,
            previous_value,
            new_value,
            reason,
            effective_date,
            authorized_by,
            authorized_date,
            downstream_impact_reviewed,
            created_at,
            created_by
        )
        VALUES
        (
            @Id,
            @CalendarId,
            @AmendmentNumber,
            @FromVersion,
            @ToVersion,
            @AmendmentType,
            @AffectedEntryId,
            @ChangeSummary,
            @PreviousValue,
            @NewValue,
            @Reason,
            @EffectiveDate,
            @AuthorizedBy,
            @AuthorizedDate,
            @DownstreamImpactReviewed,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private const string _updateCalendarVersionSql = """
        UPDATE operations.school_calendars
        SET version = @ToVersion,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @CalendarId;
        """;

    private const string _deleteEntrySql = """
        DELETE FROM operations.calendar_entries
        WHERE id = @EntryId;
        """;

    /// <summary>
    /// Deletes a calendar entry, recording an amendment and incrementing calendar minor version if published.
    /// </summary>
    public async Task<Result> HandleAsync(
        Guid calendarId,
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var existing = await connection.QuerySingleOrDefaultAsync<CalendarEntryDetailsRow>(new CommandDefinition(
            _selectEntrySql,
            new { CalendarId = calendarId, EntryId = entryId },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure(
                DomainError.NotFound(
                    "Operations.CalendarEntryNotFound",
                    $"Calendar entry with ID '{entryId}' was not found in school calendar '{calendarId}'."));
        }

        if (existing.ClosureId.HasValue)
        {
            return Result.Failure(
                DomainError.Validation(
                    "Operations.ClosureEntryCannotBeDirectlyDeleted",
                    "This calendar entry was created by a school closure. Please lift or manage the closure directly."));
        }

        var isPublished = string.Equals(existing.PublicationStatus, "published", StringComparison.OrdinalIgnoreCase);
        var now = DateTimeOffset.UtcNow;
        var username = currentUser.Username ?? "system";

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        if (isPublished)
        {
            // Nullify any existing amendment references to avoid foreign key violations on delete
            await connection.ExecuteAsync(new CommandDefinition(
                _clearAmendmentFkSql,
                new { EntryId = entryId },
                transaction: transaction,
                cancellationToken: cancellationToken));

            var fromVersion = existing.CalendarVersion;
            var toVersion = CalendarAmendmentHelper.BumpMinorVersion(fromVersion);
            var amendmentYear = DateTime.UtcNow.Year;
            var amendmentNumber = await CalendarAmendmentHelper.GenerateAmendmentNumberAsync(
                connection,
                transaction,
                amendmentYear,
                cancellationToken);

            var previousValueJson = JsonSerializer.Serialize(new
            {
                existing.EntryDate,
                existing.EndDate,
                existing.Designation,
                existing.Name,
                existing.Description,
                existing.IsInstructional,
                existing.AppliesTo,
            });

            var changeSummary = $"Removed calendar entry '{existing.Name}' ({existing.Designation}) on {existing.EntryDate:yyyy-MM-dd}.";

            await connection.ExecuteAsync(new CommandDefinition(
                _insertAmendmentSql,
                new
                {
                    Id = Guid.NewGuid(),
                    CalendarId = calendarId,
                    AmendmentNumber = amendmentNumber,
                    FromVersion = fromVersion,
                    ToVersion = toVersion,
                    AmendmentType = "remove_entry",
                    AffectedEntryId = (Guid?)null,
                    ChangeSummary = changeSummary,
                    PreviousValue = previousValueJson,
                    NewValue = (string?)null,
                    Reason = "Operational calendar entry removed post-publication.",
                    EffectiveDate = existing.EntryDate,
                    AuthorizedBy = username,
                    AuthorizedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    DownstreamImpactReviewed = false,
                    CreatedAt = now,
                    CreatedBy = username,
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                _updateCalendarVersionSql,
                new
                {
                    CalendarId = calendarId,
                    ToVersion = toVersion,
                    UpdatedAt = now,
                    UpdatedBy = username,
                },
                transaction: transaction,
                cancellationToken: cancellationToken));
        }

        await connection.ExecuteAsync(new CommandDefinition(
            _deleteEntrySql,
            new { EntryId = entryId },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }

    private sealed record CalendarEntryDetailsRow(
        Guid Id,
        Guid CalendarId,
        DateOnly EntryDate,
        DateOnly? EndDate,
        string Designation,
        string Name,
        string? Description,
        bool IsInstructional,
        Guid? ClosureId,
        string AppliesTo,
        string CalendarVersion,
        string PublicationStatus);
}
