namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.UpdateTeachingAssignmentStatus;

/// <summary>
/// Command payload for updating teaching assignment status (TD-01, TD-04, P3-S06).
/// </summary>
public sealed record UpdateTeachingAssignmentStatusRequest(
    string Status,
    string? Reason = null);
