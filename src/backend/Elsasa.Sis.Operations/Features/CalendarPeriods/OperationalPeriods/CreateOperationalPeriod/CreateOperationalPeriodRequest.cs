namespace Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.CreateOperationalPeriod;

/// <summary>
/// Request payload to establish a new operational period within an academic year (ADR-005).
/// </summary>
public sealed record CreateOperationalPeriodRequest(
    string PeriodCode,
    string Name,
    string PeriodType,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? ParentPeriodId = null,
    string? CampusContext = null,
    string? Notes = null);
