namespace Elsasa.Sis.Operations.Features.CalendarPeriods.CalendarEntries.GetCalendarMatrixProjection;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler synthesizing the dynamic calendar matrix projection (ADR-004, ADR-005, TD-04).
/// Projects standard Mon–Fri instructional baselines overlaid with explicit entries and closures.
/// </summary>
public sealed class GetCalendarMatrixProjectionHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _selectCalendarContextSql = """
        SELECT
            c.id AS CalendarId,
            c.name AS CalendarName,
            c.version AS Version,
            c.publication_status AS PublicationStatus,
            ay.start_date AS AcademicYearStartDate,
            ay.end_date AS AcademicYearEndDate
        FROM operations.school_calendars c
        INNER JOIN operations.academic_years ay ON c.academic_year_id = ay.id
        WHERE c.id = @CalendarId;
        """;

    private const string _selectCalendarEntriesSql = """
        SELECT
            id AS Id,
            calendar_id AS CalendarId,
            entry_date AS EntryDate,
            end_date AS EndDate,
            designation AS Designation,
            name AS Name,
            description AS Description,
            is_instructional AS IsInstructional,
            closure_id AS ClosureId,
            applies_to AS AppliesTo
        FROM operations.calendar_entries
        WHERE calendar_id = @CalendarId
          AND (
              (end_date IS NULL AND entry_date >= @StartDate AND entry_date <= @EndDate)
              OR
              (end_date IS NOT NULL AND entry_date <= @EndDate AND end_date >= @StartDate)
          )
        ORDER BY entry_date ASC;
        """;

    /// <summary>
    /// Generates a continuous matrix projection for the requested date range.
    /// </summary>
    public async Task<Result<CalendarMatrixProjectionResponse>> HandleAsync(
        Guid calendarId,
        DateOnly? startDate,
        DateOnly? endDate,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var calendarContext = await connection.QuerySingleOrDefaultAsync<CalendarContextRow>(new CommandDefinition(
            _selectCalendarContextSql,
            new { CalendarId = calendarId },
            cancellationToken: cancellationToken));

        if (calendarContext is null)
        {
            return Result.Failure<CalendarMatrixProjectionResponse>(
                DomainError.NotFound(
                    "Operations.SchoolCalendarNotFound",
                    $"School calendar with ID '{calendarId}' was not found."));
        }

        var effectiveStartDate = startDate ?? calendarContext.AcademicYearStartDate;
        var effectiveEndDate = endDate ?? calendarContext.AcademicYearEndDate;

        if (effectiveStartDate > effectiveEndDate)
        {
            return Result.Failure<CalendarMatrixProjectionResponse>(
                DomainError.Validation(
                    "Operations.InvalidDateRange",
                    $"Start date '{effectiveStartDate:yyyy-MM-dd}' cannot be after end date '{effectiveEndDate:yyyy-MM-dd}'."));
        }

        var totalDays = effectiveEndDate.DayNumber - effectiveStartDate.DayNumber + 1;
        if (totalDays > 730)
        {
            return Result.Failure<CalendarMatrixProjectionResponse>(
                DomainError.Validation(
                    "Operations.DateRangeTooLarge",
                    "The projection date range cannot exceed 730 days."));
        }

        var rawEntries = await connection.QueryAsync<CalendarEntryRow>(new CommandDefinition(
            _selectCalendarEntriesSql,
            new
            {
                CalendarId = calendarId,
                StartDate = effectiveStartDate,
                EndDate = effectiveEndDate,
            },
            cancellationToken: cancellationToken));

        var entriesByDate = new Dictionary<DateOnly, CalendarEntryRow>();
        foreach (var entry in rawEntries)
        {
            var spanStart = entry.EntryDate < effectiveStartDate ? effectiveStartDate : entry.EntryDate;
            var spanEnd = (entry.EndDate ?? entry.EntryDate) > effectiveEndDate
                ? effectiveEndDate
                : (entry.EndDate ?? entry.EntryDate);

            for (var d = spanStart; d <= spanEnd; d = d.AddDays(1))
            {
                if (!entriesByDate.TryGetValue(d, out var existing))
                {
                    entriesByDate[d] = entry;
                }
                else
                {
                    // Non-instructional entries (holidays, closures) take precedence over instructional entries
                    if (existing.IsInstructional && !entry.IsInstructional)
                    {
                        entriesByDate[d] = entry;
                    }
                    else if (string.Equals(entry.Designation, "school_closure", StringComparison.OrdinalIgnoreCase))
                    {
                        entriesByDate[d] = entry;
                    }
                }
            }
        }

        var projectedDays = new List<CalendarMatrixDayProjection>(totalDays);
        var instructionalCount = 0;
        var nonInstructionalCount = 0;

        for (var date = effectiveStartDate; date <= effectiveEndDate; date = date.AddDays(1))
        {
            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var dayOfWeekName = date.DayOfWeek.ToString();

            if (entriesByDate.TryGetValue(date, out var overrideEntry))
            {
                projectedDays.Add(new CalendarMatrixDayProjection(
                    Date: date,
                    DayOfWeek: dayOfWeekName,
                    IsInstructional: overrideEntry.IsInstructional,
                    Designation: overrideEntry.Designation,
                    EntryName: overrideEntry.Name,
                    EntryDescription: overrideEntry.Description,
                    EntryId: overrideEntry.Id));

                if (overrideEntry.IsInstructional)
                {
                    instructionalCount++;
                }
                else
                {
                    nonInstructionalCount++;
                }
            }
            else
            {
                var isInstructional = !isWeekend;
                var designation = isWeekend ? "weekend" : "instructional_day";

                projectedDays.Add(new CalendarMatrixDayProjection(
                    Date: date,
                    DayOfWeek: dayOfWeekName,
                    IsInstructional: isInstructional,
                    Designation: designation,
                    EntryName: null,
                    EntryDescription: null,
                    EntryId: null));

                if (isInstructional)
                {
                    instructionalCount++;
                }
                else
                {
                    nonInstructionalCount++;
                }
            }
        }

        var response = new CalendarMatrixProjectionResponse(
            calendarId,
            calendarContext.CalendarName,
            calendarContext.Version,
            calendarContext.PublicationStatus,
            effectiveStartDate,
            effectiveEndDate,
            totalDays,
            instructionalCount,
            nonInstructionalCount,
            projectedDays);

        return Result.Success(response);
    }

    private sealed record CalendarContextRow(
        Guid CalendarId,
        string CalendarName,
        string Version,
        string PublicationStatus,
        DateOnly AcademicYearStartDate,
        DateOnly AcademicYearEndDate);

    private sealed record CalendarEntryRow(
        Guid Id,
        Guid CalendarId,
        DateOnly EntryDate,
        DateOnly? EndDate,
        string Designation,
        string Name,
        string? Description,
        bool IsInstructional,
        Guid? ClosureId,
        string AppliesTo);
}
