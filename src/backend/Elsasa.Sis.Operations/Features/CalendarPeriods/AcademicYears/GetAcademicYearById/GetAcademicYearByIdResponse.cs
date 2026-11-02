namespace Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.GetAcademicYearById;

/// <summary>
/// Detailed response payload for a single academic year record (ADR-005).
/// </summary>
public sealed record GetAcademicYearByIdResponse(
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
