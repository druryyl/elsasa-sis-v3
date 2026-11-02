namespace Elsasa.Sis.Operations.Features.CalendarPeriods.CalendarEntries.CreateCalendarEntry;

/// <summary>
/// Request payload for creating a calendar entry within a school calendar (ADR-005).
/// </summary>
/// <param name="EntryDate">Commencement date of the entry.</param>
/// <param name="EndDate">Optional concluding date for multi-day spans.</param>
/// <param name="Designation">Day type: instructional_day, holiday, school_closure, staff_only_day, exam_day, significant_date, domain_window.</param>
/// <param name="Name">Descriptive title of the entry.</param>
/// <param name="Description">Optional detailed description.</param>
/// <param name="IsInstructional">Whether standard teaching occurs on this date.</param>
/// <param name="AppliesTo">Target audience filter (default: "All").</param>
/// <param name="AmendmentReason">Optional justification reason if calendar is published.</param>
public sealed record CreateCalendarEntryRequest(
    DateOnly EntryDate,
    DateOnly? EndDate,
    string Designation,
    string Name,
    string? Description = null,
    bool IsInstructional = false,
    string? AppliesTo = "All",
    string? AmendmentReason = null);
