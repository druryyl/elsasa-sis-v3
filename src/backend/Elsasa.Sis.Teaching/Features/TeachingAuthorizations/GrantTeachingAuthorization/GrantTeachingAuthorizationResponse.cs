namespace Elsasa.Sis.Teaching.Features.TeachingAuthorizations.GrantTeachingAuthorization;

/// <summary>
/// Response payload returned after granting a formal teaching authorization (P4-S11).
/// </summary>
/// <param name="Id">The unique identifier of the granted authorization.</param>
/// <param name="AuthorizationNumber">The authoritative unique authorization identifier number.</param>
/// <param name="TeacherProfileId">The unique identifier of the authorized teacher profile.</param>
/// <param name="AuthorizedCapabilityId">Optional capability this authorization is based upon.</param>
/// <param name="ScopeDescription">Detailed description of authorized teaching duties and subjects.</param>
/// <param name="CampusScope">Campus boundary ('All' or specific campus).</param>
/// <param name="ProgramScope">Optional educational program boundary.</param>
/// <param name="LevelScope">Optional grade stage or level boundary.</param>
/// <param name="DeliveryModeScope">Optional delivery mode boundary.</param>
/// <param name="Status">The authorization status ('active' or 'authorized_under_supervision').</param>
/// <param name="UnverifiedCredentialWarning">Indicates whether this authorization was granted provisionally under submitted unverified credentials.</param>
/// <param name="EffectiveStartDate">Effective start date.</param>
/// <param name="EffectiveEndDate">Optional effective end date.</param>
/// <param name="DecisionAuthority">Name/identity of the authority who granted the authorization.</param>
/// <param name="ApprovedDate">Date when authorization was approved.</param>
/// <param name="LinkedEvidenceIds">List of linked qualification evidence IDs.</param>
/// <param name="SupervisionRequirementId">Optional supervision requirement ID if granted under supervision.</param>
public sealed record GrantTeachingAuthorizationResponse(
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
    string DecisionAuthority,
    DateOnly ApprovedDate,
    IReadOnlyList<Guid> LinkedEvidenceIds,
    Guid? SupervisionRequirementId = null);
