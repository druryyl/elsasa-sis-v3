namespace Elsasa.Sis.Academics.Features.AcademicStandings.BatchDetermineCohortStanding;

/// <summary>
/// Individual standing determination item within a cohort batch determination command (P3-S07, UC-ACD-17).
/// </summary>
/// <param name="StudentId">Target student identifier.</param>
/// <param name="Standing">Determined academic standing ('good_standing', 'conditional_standing', 'not_in_good_standing').</param>
/// <param name="PolicyReference">Governing policy or criteria reference.</param>
/// <param name="Conditions">Stipulated conditions or requirements for conditional standing.</param>
/// <param name="IsException">Indicates whether this standing determination is an authorized committee exception.</param>
/// <param name="ExceptionReason">Mandatory justification narrative when determined as an exception (min 5 chars).</param>
/// <param name="CommitteeReference">Formal decree or committee reference (e.g., SK Dewan Guru, min 5 chars).</param>
public sealed record StandingItemDto(
    Guid StudentId,
    string Standing,
    string PolicyReference,
    string? Conditions = null,
    bool IsException = false,
    string? ExceptionReason = null,
    string? CommitteeReference = null);
