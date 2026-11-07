namespace Elsasa.Sis.Academics.Features.RecordCorrections.CorrectAcademicStanding;

/// <summary>
/// Request payload for auditable correction of an approved academic standing determination (P5-S12, UC-ACD-15, TD-05).
/// </summary>
/// <param name="Standing">Corrected standing determination ('good_standing', 'conditional_standing', 'not_in_good_standing').</param>
/// <param name="PolicyReference">Governing policy or criteria reference.</param>
/// <param name="Conditions">Stipulated conditions or requirements for conditional standing.</param>
/// <param name="IsException">Indicates whether this standing determination is an authorized committee exception.</param>
/// <param name="ExceptionReason">Mandatory justification narrative when determined as an exception (min 5 chars).</param>
/// <param name="CommitteeReference">Formal decree or committee reference (e.g., SK Dewan Guru, min 5 chars).</param>
/// <param name="Reason">Mandatory justification explaining the historical standing correction (min 10 chars).</param>
/// <param name="Id">Optional standing ID if included in request payload.</param>
public sealed record CorrectAcademicStandingRequest(
    string Standing,
    string PolicyReference,
    string? Conditions = null,
    bool IsException = false,
    string? ExceptionReason = null,
    string? CommitteeReference = null,
    string Reason = "",
    Guid? Id = null);
