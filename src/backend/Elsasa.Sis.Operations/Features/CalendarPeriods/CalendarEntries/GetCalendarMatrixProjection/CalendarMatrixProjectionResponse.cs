namespace Elsasa.Sis.Operations.Features.CalendarPeriods.CalendarEntries.GetCalendarMatrixProjection;

/// <summary>
/// Response payload containing the synthesized calendar matrix projection across a date range (ADR-005, TD-04).
/// </summary>
public sealed record CalendarMatrixProjectionResponse(
    Guid CalendarId,
    string CalendarName,
    string Version,
    string PublicationStatus,
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalDays,
    int InstructionalDaysCount,
    int NonInstructionalDaysCount,
    IReadOnlyList<CalendarMatrixDayProjection> Days);
