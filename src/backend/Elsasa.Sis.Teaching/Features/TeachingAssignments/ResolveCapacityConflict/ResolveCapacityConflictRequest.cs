namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.ResolveCapacityConflict;

/// <summary>
/// Command payload for resolving or waiving a capacity conflict (TD-01, P3-S07).
/// </summary>
/// <param name="Status">Target resolution status ('resolved' or 'waived_with_exception').</param>
/// <param name="ResolutionNotes">Optional descriptive notes explaining the resolution.</param>
/// <param name="ResolutionAction">Optional resolution action identifier.</param>
public sealed record ResolveCapacityConflictRequest(
    string Status,
    string? ResolutionNotes = null,
    string? ResolutionAction = null);
