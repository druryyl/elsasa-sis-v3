namespace Elsasa.Sis.Teaching.Features.TeachingAuthorizations.ResolveSupervisionRequirement;

/// <summary>
/// Response payload returned after successfully resolving a supervision requirement (TD-05, P4-S13).
/// </summary>
/// <param name="Id">The unique identifier of the resolved supervision requirement.</param>
/// <param name="AuthorizationId">The unique identifier of the parent teaching authorization.</param>
/// <param name="Status">The resolution status assigned ('completed', 'waived', 'escalated').</param>
/// <param name="ResolutionNotes">Mandatory resolution assessment rationale.</param>
/// <param name="ResolvedDate">The date when resolution occurred.</param>
/// <param name="ResolvedBy">The user identity who resolved the requirement.</param>
/// <param name="AuthorizationStatus">The current status of the parent teaching authorization.</param>
/// <param name="AuthorizationUpdatedToActive">Whether the parent authorization was promoted from 'authorized_under_supervision' to 'active'.</param>
public sealed record ResolveSupervisionRequirementResponse(
    Guid Id,
    Guid AuthorizationId,
    string Status,
    string ResolutionNotes,
    DateOnly ResolvedDate,
    string ResolvedBy,
    string AuthorizationStatus,
    bool AuthorizationUpdatedToActive);
