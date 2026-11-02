namespace Elsasa.Sis.Operations.Features.CalendarPeriods.CreateAcademicYear;

/// <summary>
/// Response payload returned upon successful creation of an academic year (ADR-005).
/// </summary>
/// <param name="Id">The unique identifier of the academic year.</param>
/// <param name="Code">The unique academic year code.</param>
/// <param name="Name">The descriptive academic year name.</param>
/// <param name="StartDate">The starting calendar date.</param>
/// <param name="EndDate">The ending calendar date.</param>
/// <param name="Status">The lifecycle state of the academic year (initial: "draft").</param>
/// <param name="Notes">Optional administrative notes.</param>
public sealed record CreateAcademicYearResponse(
    Guid Id,
    string Code,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    string? Notes = null)
{
    /// <summary>
    /// Backwards-compatibility property for existing test suites prior to P5-S09 lifecycle test migration.
    /// </summary>
    public bool IsActive => string.Equals(Status, "active", StringComparison.OrdinalIgnoreCase);
}
