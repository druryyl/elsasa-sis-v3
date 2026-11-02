namespace Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.CloseOperationalPeriod;

/// <summary>
/// Response payload confirming operational period closure (ADR-005).
/// </summary>
public sealed record CloseOperationalPeriodResponse(
    Guid Id,
    string Status,
    DateTimeOffset? UpdatedAt);
