namespace Elsasa.Sis.Operations.Features.CalendarPeriods.CalendarEntries.GetCalendarMatrixProjection;

/// <summary>
/// Daily calendar projection item with day classification and synthesized instructional status (ADR-005, TD-04).
/// </summary>
/// <param name="Date">Calendar date.</param>
/// <param name="DayOfWeek">Day of week name (e.g. "Monday", "Saturday").</param>
/// <param name="IsInstructional">Whether standard teaching/instruction occurs on this date.</param>
/// <param name="Designation">Day type classification (e.g. "instructional_day", "weekend", "holiday", "school_closure", "staff_only_day", "exam_day").</param>
/// <param name="EntryName">Optional explicit entry title if overridden.</param>
/// <param name="EntryDescription">Optional explicit entry description if overridden.</param>
/// <param name="EntryId">Optional identifier of the overriding calendar entry.</param>
public sealed record CalendarMatrixDayProjection(
    DateOnly Date,
    string DayOfWeek,
    bool IsInstructional,
    string Designation,
    string? EntryName,
    string? EntryDescription,
    Guid? EntryId = null);
