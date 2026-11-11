namespace Elsasa.Sis.Teaching.Features.Substitutions.CancelSubstitution;

/// <summary>
/// Response payload after cancelling an emergency substitution arrangement (TD-05, P4-S11).
/// </summary>
public sealed record CancelSubstitutionResponse(
    Guid Id,
    string SubstitutionNumber,
    string Status,
    DateTimeOffset CancelledAt);
