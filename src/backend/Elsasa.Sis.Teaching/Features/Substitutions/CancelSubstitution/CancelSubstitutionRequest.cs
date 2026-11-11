namespace Elsasa.Sis.Teaching.Features.Substitutions.CancelSubstitution;

/// <summary>
/// Command request for cancelling an emergency substitution arrangement (TD-05, P4-S11).
/// </summary>
/// <param name="Reason">Optional cancellation rationale or justification.</param>
public sealed record CancelSubstitutionRequest(string? Reason = null);
