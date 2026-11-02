namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.WithdrawSchoolCalendar;

/// <summary>
/// Response payload for withdrawing a school calendar (ADR-005).
/// </summary>
public sealed record WithdrawSchoolCalendarResponse(
    Guid Id,
    Guid AcademicYearId,
    string Name,
    string Version,
    string PublicationStatus);
