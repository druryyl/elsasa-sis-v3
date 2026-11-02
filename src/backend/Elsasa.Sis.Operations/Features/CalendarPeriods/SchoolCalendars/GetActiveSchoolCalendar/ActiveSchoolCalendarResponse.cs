namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.GetActiveSchoolCalendar;

/// <summary>
/// Response payload for querying the active published school calendar of an academic year (ADR-005).
/// </summary>
public sealed record ActiveSchoolCalendarResponse(
    Guid Id,
    Guid AcademicYearId,
    string Name,
    string ScopeType,
    string ScopeReference,
    string Version,
    string Status,
    string PublicationStatus,
    DateTimeOffset? PublishedAt,
    string? PublishedBy,
    string? ApprovedBy,
    DateOnly? ApprovedDate,
    string? Notes,
    DateTimeOffset CreatedAt);
