namespace Elsasa.Sis.Teaching.Features.Reassignments.ProposePermanentReassignment;

using Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Response payload for proposed permanent reassignment.
/// </summary>
public sealed record ProposePermanentReassignmentResponse(TeachingReassignmentDto Reassignment);
