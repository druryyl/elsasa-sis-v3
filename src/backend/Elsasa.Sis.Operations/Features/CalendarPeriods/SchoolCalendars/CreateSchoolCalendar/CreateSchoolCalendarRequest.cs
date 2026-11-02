namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.CreateSchoolCalendar;

/// <summary>
/// Request payload to create a new school calendar for an academic year (ADR-005).
/// </summary>
/// <param name="Name">Human-readable calendar title (e.g., "Kalender SD Elsasa TP 2026/2027").</param>
/// <param name="ScopeType">Scope level: institution, campus, program (default: "institution").</param>
/// <param name="ScopeReference">Specific target scope (default: "All").</param>
/// <param name="Notes">Optional administrative notes.</param>
public sealed record CreateSchoolCalendarRequest(
    string Name,
    string? ScopeType = "institution",
    string? ScopeReference = "All",
    string? Notes = null);
