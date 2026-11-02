namespace Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.UpdateAcademicYear;

/// <summary>
/// Request payload to update an existing draft academic year (ADR-005).
/// </summary>
/// <param name="Code">Unique academic year code (e.g., "AY-2026-2027").</param>
/// <param name="Name">Descriptive academic year name.</param>
/// <param name="StartDate">The starting calendar date.</param>
/// <param name="EndDate">The ending calendar date.</param>
/// <param name="Notes">Optional administrative notes.</param>
public sealed record UpdateAcademicYearRequest(
    string Code,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Notes = null);
