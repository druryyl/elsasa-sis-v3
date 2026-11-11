namespace Elsasa.Sis.Teaching.Features.Reassignments.ConfirmReassignmentHandover;

/// <summary>
/// Command request to confirm syllabus, materials, and grading handover for a reassignment (TD-01, P4-S12).
/// </summary>
/// <param name="HandoverStatus">Target handover state ('completed' or 'waived', default 'completed').</param>
/// <param name="Notes">Optional handover documentation commentary.</param>
public sealed record ConfirmReassignmentHandoverRequest(
    string? HandoverStatus = "completed",
    string? Notes = null);
