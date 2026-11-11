namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.AcknowledgeTeachingAssignment;

/// <summary>
/// Response payload after acknowledging a teaching assignment (TD-01, P3-S06).
/// </summary>
public sealed record AcknowledgeTeachingAssignmentResponse(
    Guid Id,
    string AssignmentNumber,
    bool TeacherAcknowledged,
    DateOnly TeacherAcknowledgedDate,
    DateTimeOffset AcknowledgedAt);
