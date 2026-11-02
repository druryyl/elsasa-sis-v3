namespace Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.GetOperationalPeriodsByAcademicYear;

/// <summary>
/// Flat item representation of an operational period (ADR-005).
/// </summary>
public sealed record OperationalPeriodItemResponse(
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
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
