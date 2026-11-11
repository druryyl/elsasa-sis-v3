namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.ProposeTeachingAssignment;

/// <summary>
/// Command payload for proposing a new teaching assignment (TD-01, TD-04, P3-S06).
/// </summary>
public sealed record ProposeTeachingAssignmentRequest(
    Guid TeachingDemandId,
    Guid TeacherProfileId,
    Guid AssignmentRoleId,
    string AssignmentScope,
    Guid ScopeReferenceId,
    string ScopeLabel,
    Guid AcademicPeriodId,
    DateOnly EffectiveStartDate,
    DateOnly EffectiveEndDate,
    decimal WorkloadCommitmentValue,
    string? WorkloadUnit = null,
    string? AssignmentNumber = null,
    Guid? PredecessorAssignmentId = null,
    string? Notes = null);
