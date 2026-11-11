namespace Elsasa.Sis.Contracts.Teaching.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published when a teaching assignment is proposed (ADR-003).
/// </summary>
/// <param name="AssignmentId">The unique identifier of the proposed assignment.</param>
/// <param name="AssignmentNumber">The authoritative assignment number (e.g. ASN-2026-0001).</param>
/// <param name="TeachingDemandId">The demand satisfied by this assignment.</param>
/// <param name="TeacherProfileId">The candidate teacher assigned.</param>
/// <param name="AssignmentRoleId">The assignment role identifier.</param>
/// <param name="AssignmentScope">The scope category ('learning_unit', 'cohort_group', 'individual_student', 'extracurricular_group').</param>
/// <param name="ScopeReferenceId">External scope reference identifier.</param>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="WorkloadCommitmentValue">Proposed weekly workload effort.</param>
/// <param name="ProposedBy">Coordinator who proposed the assignment.</param>
/// <param name="OccurredOn">Timestamp when this event occurred in UTC.</param>
public sealed record TeachingAssignmentProposedEvent(
    Guid AssignmentId,
    string AssignmentNumber,
    Guid TeachingDemandId,
    Guid TeacherProfileId,
    Guid AssignmentRoleId,
    string AssignmentScope,
    Guid ScopeReferenceId,
    Guid AcademicPeriodId,
    decimal WorkloadCommitmentValue,
    string ProposedBy,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TeachingAssignmentProposedEvent"/> record with current UTC timestamp.
    /// </summary>
    public TeachingAssignmentProposedEvent(
        Guid assignmentId,
        string assignmentNumber,
        Guid teachingDemandId,
        Guid teacherProfileId,
        Guid assignmentRoleId,
        string assignmentScope,
        Guid scopeReferenceId,
        Guid academicPeriodId,
        decimal workloadCommitmentValue,
        string proposedBy)
        : this(
            assignmentId,
            assignmentNumber,
            teachingDemandId,
            teacherProfileId,
            assignmentRoleId,
            assignmentScope,
            scopeReferenceId,
            academicPeriodId,
            workloadCommitmentValue,
            proposedBy,
            DateTimeOffset.UtcNow)
    {
    }
}
