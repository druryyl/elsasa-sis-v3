namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.PublishSchoolCalendar;

/// <summary>
/// Response payload for a published school calendar (ADR-005).
/// </summary>
public sealed record PublishSchoolCalendarResponse(
    Guid Id,
    Guid AcademicYearId,
    string Name,
    string Version,
    string Status,
    string PublicationStatus,
    DateTimeOffset? PublishedAt,
    string? PublishedBy);
