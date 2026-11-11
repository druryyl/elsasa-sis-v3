namespace Elsasa.Sis.Teaching.Features.Substitutions.ApproveSubstitutionException;

/// <summary>
/// Command request for approving an escalated emergency substitution exception (TD-05, P4-S11).
/// </summary>
/// <param name="DecisionRemarks">Optional decision remarks or justification notes by the Teaching Authority.</param>
public sealed record ApproveSubstitutionExceptionRequest(string? DecisionRemarks = null);
