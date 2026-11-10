namespace Elsasa.Sis.Teaching.Features.TeachingAuthorizations.ResolveSupervisionRequirement;

/// <summary>
/// Request payload to resolve (complete, waive, or escalate) a supervision requirement (TD-05, P4-S13).
/// </summary>
/// <param name="ResolutionStatus">The resolution status ('completed', 'waived', 'escalated').</param>
/// <param name="ResolutionNotes">Mandatory resolution assessment rationale and notes.</param>
/// <param name="UpdateAuthorizationToActive">Optional flag to force promoting parent authorization to 'active'. Defaults to true when status is completed/waived and no active supervision requirements remain.</param>
public sealed record ResolveSupervisionRequirementRequest(
    string ResolutionStatus,
    string ResolutionNotes,
    bool? UpdateAuthorizationToActive = null)
{
    /// <summary>
    /// Optional alias property for clients passing 'status' instead of 'resolution_status'.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Resolves the effective status from either property.
    /// </summary>
    public string EffectiveStatus => !string.IsNullOrWhiteSpace(ResolutionStatus)
        ? ResolutionStatus
        : (Status ?? string.Empty);
}
