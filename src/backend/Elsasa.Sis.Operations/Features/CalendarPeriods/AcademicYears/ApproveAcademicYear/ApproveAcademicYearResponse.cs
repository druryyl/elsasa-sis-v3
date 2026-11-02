namespace Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.ApproveAcademicYear;

/// <summary>
/// Response payload returned upon successful approval of an academic year (ADR-005).
/// </summary>
/// <param name="Id">The unique identifier of the academic year.</param>
/// <param name="Code">The academic year code.</param>
/// <param name="Name">The descriptive academic year name.</param>
/// <param name="Status">The new lifecycle status ("approved").</param>
/// <param name="ApprovedBy">Identity of the authority approving the academic year.</param>
/// <param name="ApprovedDate">Formal date of approval.</param>
public sealed record ApproveAcademicYearResponse(
    Guid Id,
    string Code,
    string Name,
    string Status,
    string? ApprovedBy,
    DateOnly? ApprovedDate);
