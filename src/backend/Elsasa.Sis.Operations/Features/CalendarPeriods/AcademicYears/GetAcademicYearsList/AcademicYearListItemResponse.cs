namespace Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.GetAcademicYearsList;

/// <summary>
/// Response payload for an academic year summary in collection listings (ADR-005).
/// </summary>
public sealed record AcademicYearListItemResponse(
    Guid Id,
    string Code,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    string? ApprovedBy,
    DateOnly? ApprovedDate,
    string? Notes,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
