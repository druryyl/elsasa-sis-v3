namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.ProposeTeachingAssignment;

/// <summary>
/// Response payload after proposing a teaching assignment (TD-01, TD-04, P3-S06).
/// </summary>
public sealed record ProposeTeachingAssignmentResponse(
    Guid Id,
    string AssignmentNumber,
    string Status,
    Guid TeachingDemandId,
    Guid TeacherProfileId,
    decimal WorkloadCommitmentValue,
    DateTimeOffset CreatedAt);
