namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Strongly-typed contract DTO representing an authoritative student academic standing determination (ADR-001, UC-ACD-17).
/// </summary>
/// <param name="Id">The unique identifier of the academic standing record.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="AcademicPeriodId">The unique identifier of the academic period.</param>
/// <param name="Standing">The formal standing status (e.g., 'good_standing', 'conditional_standing', 'not_in_good_standing').</param>
/// <param name="PolicyReference">Governing policy or criteria reference used for the determination.</param>
/// <param name="Conditions">Stipulated conditions or requirements for conditional standing, if applicable.</param>
/// <param name="IsException">Indicates whether the standing was determined as an authorized committee exception.</param>
/// <param name="ExceptionReason">Mandatory justification narrative when determined as an exception.</param>
/// <param name="CommitteeReference">Reference to formal decree or minutes (e.g., SK Dewan Guru), if an exception.</param>
/// <param name="AuthorizedBy">Username or identifier of the certifying Academic Authority.</param>
/// <param name="DeterminedAt">The timestamp when the standing was determined.</param>
public sealed record StudentAcademicStandingDto(
    Guid Id,
    Guid StudentId,
    Guid AcademicPeriodId,
    string Standing,
    string PolicyReference,
    string? Conditions,
    bool IsException,
    string? ExceptionReason,
    string? CommitteeReference,
    string AuthorizedBy,
    DateTimeOffset DeterminedAt)
{
    /// <summary>
    /// Gets the unique identifier of the academic standing (alias for <see cref="Id"/>).
    /// </summary>
    public Guid StandingId => Id;
}
