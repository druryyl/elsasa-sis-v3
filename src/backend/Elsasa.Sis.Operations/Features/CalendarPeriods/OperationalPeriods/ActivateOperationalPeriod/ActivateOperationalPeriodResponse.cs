namespace Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.ActivateOperationalPeriod;

/// <summary>
/// Response payload confirming operational period activation (ADR-005).
/// </summary>
/// <param name="Id">The unique identifier of the operational period.</param>
/// <param name="PeriodCode">The operational period code.</param>
/// <param name="Name">The descriptive period name.</param>
/// <param name="Status">The new lifecycle status ("active").</param>
/// <param name="UpdatedAt">The timestamp of the state transition.</param>
public sealed record ActivateOperationalPeriodResponse(
    Guid Id,
    string PeriodCode,
    string Name,
    string Status,
    DateTimeOffset UpdatedAt);


