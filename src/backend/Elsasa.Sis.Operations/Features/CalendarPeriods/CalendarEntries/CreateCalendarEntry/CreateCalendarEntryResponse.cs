namespace Elsasa.Sis.Operations.Features.CalendarPeriods.CalendarEntries.CreateCalendarEntry;

/// <summary>
/// Response payload for a newly created calendar entry (ADR-005).
/// </summary>
public sealed record CreateCalendarEntryResponse(
    Guid Id,
    Guid CalendarId,
    DateOnly EntryDate,
    DateOnly? EndDate,
    string Designation,
    string Name,
    string? Description,
    bool IsInstructional,
    string AppliesTo,
    string? CalendarVersion,
    string? AmendmentNumber,
    DateTimeOffset CreatedAt);
