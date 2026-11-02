namespace Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.UpdateOperationalPeriod;

/// <summary>
/// Response payload representing an updated operational period (ADR-005).
/// </summary>
public sealed record UpdateOperationalPeriodResponse(
    Guid Id,
    Guid AcademicYearId,
    string PeriodCode,
    string Name,
    string PeriodType,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? ParentPeriodId,
    string CampusContext,
    string Status,
    string? Notes,
    DateTimeOffset? UpdatedAt);
