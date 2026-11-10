namespace Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Strongly-typed contract DTO representing an authoritative school permission for a teacher to perform specified teaching work.
/// </summary>
/// <param name="Id">The unique identifier of the teaching authorization.</param>
/// <param name="AuthorizationNumber">The human-readable unique authorization identifier (e.g., AUTH-2026-0045).</param>
/// <param name="TeacherProfileId">The unique identifier of the authorized teacher profile.</param>
/// <param name="AuthorizedCapabilityId">Optional unique identifier of the capability this authorization is based upon.</param>
/// <param name="ScopeDescription">Detailed description of authorized teaching duties and subjects.</param>
/// <param name="CampusScope">Campus boundary ('All' or specific campus identifier/name).</param>
/// <param name="ProgramScope">Optional educational program boundary.</param>
/// <param name="LevelScope">Optional grade stage or educational level boundary.</param>
/// <param name="DeliveryModeScope">Optional delivery mode boundary (in_person, online, hybrid).</param>
/// <param name="Status">The authorization lifecycle status (active, authorized_under_supervision, restricted, suspended, revoked, expired).</param>
/// <param name="UnverifiedCredentialWarning">Indicates whether this authorization was granted provisionally under submitted unverified credentials.</param>
/// <param name="EffectiveStartDate">The date from which this authorization takes effect.</param>
/// <param name="EffectiveEndDate">Optional date until which this authorization remains effective.</param>
/// <param name="ConditionsOrRestrictions">Optional special operational conditions or restrictions.</param>
/// <param name="DecisionAuthority">The name or identity of the school authority who granted this authorization.</param>
/// <param name="ApprovedDate">The date when formal approval was granted.</param>
/// <param name="SuspensionOrRevocationReason">Optional justification if the authorization was restricted, suspended, or revoked.</param>
/// <param name="Notes">Optional administrative notes.</param>
/// <param name="LinkedEvidenceIds">Optional list of qualification evidence IDs supporting this authorization.</param>
/// <param name="SupervisionRequirements">Optional list of supervision requirements attached to this authorization.</param>
public sealed record TeachingAuthorizationDto(
    Guid Id,
    string AuthorizationNumber,
    Guid TeacherProfileId,
    Guid? AuthorizedCapabilityId,
    string ScopeDescription,
    string CampusScope,
    string? ProgramScope,
    string? LevelScope,
    string? DeliveryModeScope,
    string Status,
    bool UnverifiedCredentialWarning,
    DateOnly EffectiveStartDate,
    DateOnly? EffectiveEndDate,
    string? ConditionsOrRestrictions,
    string DecisionAuthority,
    DateOnly ApprovedDate,
    string? SuspensionOrRevocationReason,
    string? Notes,
    IReadOnlyList<Guid>? LinkedEvidenceIds = null,
    IReadOnlyList<SupervisionRequirementDto>? SupervisionRequirements = null);
