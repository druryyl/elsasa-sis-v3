namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.ResolveCapacityConflict;

/// <summary>
/// Response payload after resolving a capacity conflict (TD-01, P3-S07).
/// </summary>
/// <param name="Id">The unique identifier of the resolved capacity conflict.</param>
/// <param name="Status">The resulting conflict status ('resolved' or 'waived_with_exception').</param>
/// <param name="ResolutionAction">The resolution action applied.</param>
/// <param name="ResolvedBy">The identity of the user who resolved the conflict.</param>
/// <param name="ResolvedDate">The date on which the conflict was marked resolved.</param>
/// <param name="ResolvedAt">The UTC timestamp when resolution was recorded.</param>
/// <param name="ResolutionNotes">Optional resolution notes.</param>
public sealed record ResolveCapacityConflictResponse(
    Guid Id,
    string Status,
    string? ResolutionAction,
    string? ResolvedBy,
    DateOnly? ResolvedDate,
    DateTimeOffset? ResolvedAt,
    string? ResolutionNotes);
