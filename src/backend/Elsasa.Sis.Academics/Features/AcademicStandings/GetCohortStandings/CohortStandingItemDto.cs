namespace Elsasa.Sis.Academics.Features.AcademicStandings.GetCohortStandings;

/// <summary>
/// Data transfer object representing a determined academic standing for a student in a cohort review (TD-03, UC-ACD-17, P3-S09, SCR-ACD-09).
/// </summary>
/// <param name="Id">The unique identifier of the academic standing record.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="StudentNumber">The student registration number or identifier.</param>
/// <param name="StudentName">The full name of the student.</param>
/// <param name="GradeLevel">The cohort grade level.</param>
/// <param name="AcademicPeriodId">The academic period identifier.</param>
/// <param name="Standing">The formal standing status (e.g., 'good_standing', 'conditional_standing', 'not_in_good_standing').</param>
/// <param name="PolicyReference">Governing policy or criteria reference.</param>
/// <param name="Conditions">Stipulated conditions or requirements for conditional standing, if applicable.</param>
/// <param name="IsException">Indicates whether the standing was determined as an authorized committee exception.</param>
/// <param name="ExceptionReason">Mandatory justification narrative when determined as an exception.</param>
/// <param name="CommitteeReference">Reference to formal decree or minutes (e.g., SK Dewan Guru), if an exception.</param>
/// <param name="AuthorizedBy">Username or identifier of the certifying Academic Authority.</param>
/// <param name="DeterminedAt">The timestamp when the standing was determined.</param>
/// <param name="CreatedAt">The timestamp when the record was created.</param>
/// <param name="UpdatedAt">The timestamp when the record was last updated, if any.</param>
public sealed record CohortStandingItemDto(
    Guid Id,
    Guid StudentId,
    string? StudentNumber,
    string StudentName,
    string? GradeLevel,
    Guid AcademicPeriodId,
    string Standing,
    string PolicyReference,
    string? Conditions,
    bool IsException,
    string? ExceptionReason,
    string? CommitteeReference,
    string AuthorizedBy,
    DateTimeOffset DeterminedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    /// <summary>
    /// Gets the unique identifier of the academic standing (alias for <see cref="Id"/>).
    /// </summary>
    public Guid StandingId => Id;
}
