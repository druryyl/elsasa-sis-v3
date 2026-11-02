namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.CreateSchoolCalendar;

/// <summary>
/// Response payload for a newly created school calendar (ADR-005).
/// </summary>
public sealed record CreateSchoolCalendarResponse(
    Guid Id,
    Guid AcademicYearId,
    string Name,
    string ScopeType,
    string ScopeReference,
    string Version,
    string Status,
    string PublicationStatus,
    string? Notes,
    DateTimeOffset CreatedAt);
