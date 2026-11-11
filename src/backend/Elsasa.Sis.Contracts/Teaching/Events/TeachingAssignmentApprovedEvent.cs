namespace Elsasa.Sis.Contracts.Teaching.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published when a teaching assignment is approved by school authority (ADR-003).
/// </summary>
/// <param name="AssignmentId">The unique identifier of the approved assignment.</param>
/// <param name="AssignmentNumber">The authoritative assignment number (e.g. ASN-2026-0001).</param>
/// <param name="TeachingDemandId">The demand satisfied by this assignment.</param>
/// <param name="TeacherProfileId">The approved teacher profile identifier.</param>
/// <param name="AssignmentRoleId">The assigned role identifier.</param>
/// <param name="AssignmentScope">The scope category ('learning_unit', 'cohort_group', 'individual_student', 'extracurricular_group').</param>
/// <param name="ScopeReferenceId">External scope reference identifier.</param>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="CommittedWorkloadHours">Approved committed weekly workload hours.</param>
/// <param name="ApprovedBy">Teaching Authority who granted approval.</param>
/// <param name="ApprovedDate">Date when approval was granted.</param>
/// <param name="OccurredOn">Timestamp when this event occurred in UTC.</param>
public sealed record TeachingAssignmentApprovedEvent(
    Guid AssignmentId,
    string AssignmentNumber,
    Guid TeachingDemandId,
    Guid TeacherProfileId,
    Guid AssignmentRoleId,
    string AssignmentScope,
    Guid ScopeReferenceId,
    Guid AcademicPeriodId,
    decimal CommittedWorkloadHours,
    string ApprovedBy,
    DateOnly ApprovedDate,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TeachingAssignmentApprovedEvent"/> record with current UTC timestamp.
    /// </summary>
    public TeachingAssignmentApprovedEvent(
        Guid assignmentId,
        string assignmentNumber,
        Guid teachingDemandId,
        Guid teacherProfileId,
        Guid assignmentRoleId,
        string assignmentScope,
        Guid scopeReferenceId,
        Guid academicPeriodId,
        decimal committedWorkloadHours,
        string approvedBy,
        DateOnly approvedDate)
        : this(
            assignmentId,
            assignmentNumber,
            teachingDemandId,
            teacherProfileId,
            assignmentRoleId,
            assignmentScope,
            scopeReferenceId,
            academicPeriodId,
            committedWorkloadHours,
            approvedBy,
            approvedDate,
            DateTimeOffset.UtcNow)
    {
    }
}
