namespace Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.UpdateOperationalPeriod;

/// <summary>
/// Request payload to update an existing operational period (ADR-005).
/// </summary>
public sealed record UpdateOperationalPeriodRequest(
    string PeriodCode,
    string Name,
    string PeriodType,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? ParentPeriodId = null,
    string? CampusContext = null,
    string? Notes = null);
