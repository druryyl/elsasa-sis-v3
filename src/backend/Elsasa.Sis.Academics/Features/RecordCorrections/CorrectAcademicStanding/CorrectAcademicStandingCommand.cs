namespace Elsasa.Sis.Academics.Features.RecordCorrections.CorrectAcademicStanding;

/// <summary>
/// Command payload for performing auditable in-place corrections to an approved academic standing determination (P5-S12, UC-ACD-15, TD-05).
/// </summary>
/// <param name="Id">Target academic standing identifier.</param>
/// <param name="Standing">Corrected standing determination ('good_standing', 'conditional_standing', 'not_in_good_standing').</param>
/// <param name="PolicyReference">Governing policy or criteria reference.</param>
/// <param name="Conditions">Stipulated conditions or requirements for conditional standing.</param>
/// <param name="IsException">Indicates whether this standing determination is an authorized committee exception.</param>
/// <param name="ExceptionReason">Mandatory justification narrative when determined as an exception (min 5 chars).</param>
/// <param name="CommitteeReference">Formal decree or committee reference (e.g., SK Dewan Guru, min 5 chars).</param>
/// <param name="Reason">Mandatory justification explaining the historical standing correction (min 10 chars).</param>
public sealed record CorrectAcademicStandingCommand(
    Guid Id,
    string Standing,
    string PolicyReference,
    string? Conditions,
    bool IsException,
    string? ExceptionReason,
    string? CommitteeReference,
    string Reason);
