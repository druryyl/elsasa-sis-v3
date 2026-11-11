namespace Elsasa.Sis.Teaching.Features.Reassignments.ConfirmReassignmentHandover;

using Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Response payload for confirmed reassignment handover.
/// </summary>
public sealed record ConfirmReassignmentHandoverResponse(TeachingReassignmentDto Reassignment);
