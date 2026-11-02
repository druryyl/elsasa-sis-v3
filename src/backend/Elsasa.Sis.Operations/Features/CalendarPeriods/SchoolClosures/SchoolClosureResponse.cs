namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolClosures;

/// <summary>
/// Response payload representing an authoritative school closure record (UC-OPS-12).
/// </summary>
public sealed record SchoolClosureResponse(
    Guid Id,
    Guid AcademicYearId,
    string ClosureNumber,
    string ReasonCategory,
    string ReasonDetails,
    DateOnly StartDate,
    DateOnly EndDate,
    string AffectedCampus,
    string AffectedPopulation,
    string Status,
    string AuthorizedBy,
    DateOnly AuthorizedDate,
    string CommunicationStatus,
    string? RecoveryPlanNotes,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
