namespace Elsasa.Sis.Teaching.Features.Reassignments.ApprovePermanentReassignment;

/// <summary>
/// Command request to approve a permanent teaching responsibility reassignment (TD-01, TD-04, P4-S12).
/// </summary>
/// <param name="Notes">Optional approval commentary or handover notes.</param>
public sealed record ApprovePermanentReassignmentRequest(string? Notes = null);
