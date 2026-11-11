namespace Elsasa.Sis.Teaching.Features.Reassignments.ApprovePermanentReassignment;

using Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Response payload for approved permanent reassignment.
/// </summary>
public sealed record ApprovePermanentReassignmentResponse(
    TeachingReassignmentDto Reassignment,
    TeachingAssignmentDto ReplacementAssignment);
