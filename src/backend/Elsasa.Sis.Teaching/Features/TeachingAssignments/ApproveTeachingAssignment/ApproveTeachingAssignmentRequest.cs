namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.ApproveTeachingAssignment;

/// <summary>
/// Command payload for approving a proposed teaching assignment (TD-01, TD-04, P3-S06).
/// </summary>
public sealed record ApproveTeachingAssignmentRequest(
    DateOnly? ApprovedDate = null,
    string? Notes = null);
