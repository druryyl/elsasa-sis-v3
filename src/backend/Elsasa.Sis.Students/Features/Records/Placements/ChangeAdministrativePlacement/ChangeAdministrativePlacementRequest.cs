namespace Elsasa.Sis.Students.Features.Records.Placements.ChangeAdministrativePlacement;

/// <summary>
/// Command request payload for reassigning a student's administrative placement (ADR-004, UC-STD-06, STD-WF-05).
/// </summary>
public sealed record ChangeAdministrativePlacementRequest(
    string Campus,
    string SchoolLevel,
    string Grade,
    string AcademicYear,
    DateOnly EffectiveDate,
    string? Program = null,
    string? Stream = null,
    string? Cohort = null,
    string? Homeroom = null,
    string? Reason = null,
    string? AuthorizedBy = null,
    string? DecisionReference = null);
