namespace Elsasa.Sis.Operations.Features.CalendarPeriods.CalendarAmendments.GetCalendarAmendmentsList;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying the amendment audit timeline for a school calendar (ADR-004, ADR-005, TD-05).
/// </summary>
public sealed class GetCalendarAmendmentsListHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _selectCalendarExistsSql = """
        SELECT 1
        FROM operations.school_calendars
        WHERE id = @CalendarId;
        """;

    private const string _selectAmendmentsSql = """
        SELECT
            id AS Id,
            calendar_id AS CalendarId,
            amendment_number AS AmendmentNumber,
            from_version AS FromVersion,
            to_version AS ToVersion,
            amendment_type AS AmendmentType,
            affected_entry_id AS AffectedEntryId,
            change_summary AS ChangeSummary,
            previous_value AS PreviousValue,
            new_value AS NewValue,
            reason AS Reason,
            effective_date AS EffectiveDate,
            authorized_by AS AuthorizedBy,
            authorized_date AS AuthorizedDate,
            downstream_impact_reviewed AS DownstreamImpactReviewed,
            created_at AS CreatedAt
        FROM operations.calendar_amendments
        WHERE calendar_id = @CalendarId
        ORDER BY created_at DESC;
        """;

    /// <summary>
    /// Retrieves all calendar amendments recorded for the specified school calendar.
    /// </summary>
    public async Task<Result<IReadOnlyList<CalendarAmendmentResponse>>> HandleAsync(
        Guid calendarId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var calendarExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            _selectCalendarExistsSql,
            new { CalendarId = calendarId },
            cancellationToken: cancellationToken));

        if (!calendarExists)
        {
            return Result.Failure<IReadOnlyList<CalendarAmendmentResponse>>(
                DomainError.NotFound(
                    "Operations.SchoolCalendarNotFound",
                    $"School calendar with ID '{calendarId}' was not found."));
        }

        var amendments = await connection.QueryAsync<CalendarAmendmentResponse>(new CommandDefinition(
            _selectAmendmentsSql,
            new { CalendarId = calendarId },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<CalendarAmendmentResponse>>(amendments.AsList());
    }
}
