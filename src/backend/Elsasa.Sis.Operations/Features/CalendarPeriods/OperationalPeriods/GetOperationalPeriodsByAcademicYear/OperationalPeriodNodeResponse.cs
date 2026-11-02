namespace Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.GetOperationalPeriodsByAcademicYear;

/// <summary>
/// Hierarchical tree node representation of an operational period with nested children (ADR-005).
/// </summary>
public sealed record OperationalPeriodNodeResponse(
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
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<OperationalPeriodNodeResponse> Children);
