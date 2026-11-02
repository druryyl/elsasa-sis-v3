namespace Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.CreateOperationalPeriod;

/// <summary>
/// Response payload representing a created operational period (ADR-005).
/// </summary>
public sealed record CreateOperationalPeriodResponse(
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
    DateTimeOffset CreatedAt);
