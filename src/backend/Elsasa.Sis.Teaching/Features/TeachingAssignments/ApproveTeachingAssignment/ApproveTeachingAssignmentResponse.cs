namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.ApproveTeachingAssignment;

/// <summary>
/// Response payload after approving a teaching assignment (TD-01, TD-04, P3-S06).
/// </summary>
public sealed record ApproveTeachingAssignmentResponse(
    Guid Id,
    string AssignmentNumber,
    string Status,
    DateOnly ApprovedDate,
    string ApprovedBy,
    DateTimeOffset ApprovedAt);
