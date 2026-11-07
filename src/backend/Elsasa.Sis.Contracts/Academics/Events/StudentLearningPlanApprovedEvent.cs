namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when a student learning plan is formally approved by the Academic Authority.
/// </summary>
/// <param name="PlanId">The unique identifier of the approved student learning plan.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="AcademicPeriodId">The unique identifier of the associated academic period.</param>
/// <param name="Version">The approved version of the learning plan.</param>
/// <param name="ApprovalType">The approval decision category ('initial_approval' or 'revision_approval').</param>
/// <param name="ApprovedBy">Username of the approving academic authority.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record StudentLearningPlanApprovedEvent(
    Guid PlanId,
    Guid StudentId,
    Guid AcademicPeriodId,
    int Version,
    string ApprovalType,
    string ApprovedBy,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StudentLearningPlanApprovedEvent"/> record with the current UTC timestamp.
    /// </summary>
    /// <param name="planId">The unique identifier of the student learning plan.</param>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="academicPeriodId">The unique identifier of the academic period.</param>
    /// <param name="version">The approved version of the learning plan.</param>
    /// <param name="approvalType">The approval decision category.</param>
    /// <param name="approvedBy">Username of the approving academic authority.</param>
    public StudentLearningPlanApprovedEvent(
        Guid planId,
        Guid studentId,
        Guid academicPeriodId,
        int version,
        string approvalType,
        string approvedBy)
        : this(planId, studentId, academicPeriodId, version, approvalType, approvedBy, DateTimeOffset.UtcNow)
    {
    }
}
