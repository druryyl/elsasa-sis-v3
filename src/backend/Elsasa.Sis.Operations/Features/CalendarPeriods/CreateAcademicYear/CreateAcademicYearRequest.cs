namespace Elsasa.Sis.Operations.Features.CalendarPeriods.CreateAcademicYear;

/// <summary>
/// Request payload to create a new academic year in the Operations catalogue (ADR-005).
/// </summary>
/// <param name="Code">Unique academic year code (e.g., "AY-2026-2027").</param>
/// <param name="Name">Human-readable descriptive name (e.g., "Academic Year 2026/2027").</param>
/// <param name="StartDate">The starting calendar date of the academic year.</param>
/// <param name="EndDate">The ending calendar date of the academic year.</param>
/// <param name="Notes">Optional administrative notes.</param>
public sealed record CreateAcademicYearRequest(
    string Code,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Notes = null);
