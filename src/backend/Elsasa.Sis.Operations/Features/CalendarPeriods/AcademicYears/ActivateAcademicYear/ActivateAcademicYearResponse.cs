namespace Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.ActivateAcademicYear;

/// <summary>
/// Response payload returned upon successful activation of an academic year (ADR-005).
/// </summary>
/// <param name="Id">The unique identifier of the academic year.</param>
/// <param name="Code">The academic year code.</param>
/// <param name="Name">The descriptive academic year name.</param>
/// <param name="Status">The new lifecycle status ("active").</param>
public sealed record ActivateAcademicYearResponse(
    Guid Id,
    string Code,
    string Name,
    string Status);
