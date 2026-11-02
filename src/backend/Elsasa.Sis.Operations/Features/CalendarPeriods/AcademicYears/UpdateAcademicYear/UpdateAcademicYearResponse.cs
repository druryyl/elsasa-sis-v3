namespace Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.UpdateAcademicYear;

/// <summary>
/// Response payload returned upon successful update of an academic year (ADR-005).
/// </summary>
/// <param name="Id">The unique identifier of the academic year.</param>
/// <param name="Code">The updated academic year code.</param>
/// <param name="Name">The updated academic year name.</param>
/// <param name="StartDate">The updated starting calendar date.</param>
/// <param name="EndDate">The updated ending calendar date.</param>
/// <param name="Status">The lifecycle state of the academic year.</param>
/// <param name="Notes">Optional administrative notes.</param>
public sealed record UpdateAcademicYearResponse(
    Guid Id,
    string Code,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    string? Notes);
