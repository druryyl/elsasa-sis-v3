namespace Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.GetOperationalPeriodsByAcademicYear;

/// <summary>
/// Response payload returning operational periods as both flat list and nested tree (ADR-005).
/// </summary>
public sealed record GetOperationalPeriodsByAcademicYearResponse(
    Guid AcademicYearId,
    IReadOnlyList<OperationalPeriodItemResponse> Periods,
    IReadOnlyList<OperationalPeriodNodeResponse> Tree);
