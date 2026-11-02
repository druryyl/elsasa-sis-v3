namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.GetSchoolCalendarsList;

/// <summary>
/// Response payload representing a school calendar container for an academic year (ADR-005).
/// </summary>
public sealed record SchoolCalendarResponse(
    Guid Id,
    Guid AcademicYearId,
    string Name,
    string ScopeType,
    string ScopeReference,
    string Version,
    string Status,
    string PublicationStatus,
    DateTimeOffset? PublishedAt,
    string? PublishedBy,
    string? ApprovedBy,
    DateOnly? ApprovedDate,
    string? Notes,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);