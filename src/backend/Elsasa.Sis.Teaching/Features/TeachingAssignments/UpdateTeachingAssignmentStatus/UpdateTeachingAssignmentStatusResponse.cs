namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.UpdateTeachingAssignmentStatus;

/// <summary>
/// Response payload after updating teaching assignment status (TD-01, TD-04, P3-S06).
/// </summary>
public sealed record UpdateTeachingAssignmentStatusResponse(
    Guid Id,
    string AssignmentNumber,
    string Status,
    DateTimeOffset UpdatedAt);
