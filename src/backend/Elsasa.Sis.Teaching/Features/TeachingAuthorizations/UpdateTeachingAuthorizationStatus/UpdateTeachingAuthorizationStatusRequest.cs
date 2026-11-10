namespace Elsasa.Sis.Teaching.Features.TeachingAuthorizations.UpdateTeachingAuthorizationStatus;

/// <summary>
/// Request payload for transitioning a teaching authorization's lifecycle status (TD-05, P4-S12).
/// </summary>
/// <param name="Status">Target status ('active', 'authorized_under_supervision', 'restricted', 'suspended', 'revoked', 'expired').</param>
/// <param name="StatusReason">Mandatory justification rationale for the status modification.</param>
/// <param name="Reason">Optional alias for StatusReason.</param>
/// <param name="SuspensionOrRevocationReason">Optional alias for StatusReason.</param>
public sealed record UpdateTeachingAuthorizationStatusRequest(
    string? Status = null,
    string? StatusReason = null,
    string? Reason = null,
    string? SuspensionOrRevocationReason = null)
{
    /// <summary>
    /// Gets the resolved normalized lowercase status.
    /// </summary>
    public string ResolvedStatus => Status?.Trim().ToLowerInvariant() ?? string.Empty;

    /// <summary>
    /// Gets the resolved status justification reason from StatusReason, Reason, or SuspensionOrRevocationReason.
    /// </summary>
    public string ResolvedStatusReason =>
        !string.IsNullOrWhiteSpace(StatusReason) ? StatusReason.Trim()
        : (!string.IsNullOrWhiteSpace(Reason) ? Reason.Trim()
        : (!string.IsNullOrWhiteSpace(SuspensionOrRevocationReason) ? SuspensionOrRevocationReason.Trim() : string.Empty));
}
