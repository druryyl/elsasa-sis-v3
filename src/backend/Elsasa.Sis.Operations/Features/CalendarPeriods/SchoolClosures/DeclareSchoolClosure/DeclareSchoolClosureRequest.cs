namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolClosures.DeclareSchoolClosure;

/// <summary>
/// Request payload for declaring an authorized school closure (UC-OPS-12).
/// </summary>
public sealed record DeclareSchoolClosureRequest(
    Guid? AcademicYearId,
    Guid? CalendarId,
    string ReasonCategory,
    string ReasonDetails,
    DateOnly StartDate,
    DateOnly EndDate,
    string? AffectedCampus,
    string? AffectedPopulation,
    string? AuthorizedBy,
    string? RecoveryPlanNotes);
