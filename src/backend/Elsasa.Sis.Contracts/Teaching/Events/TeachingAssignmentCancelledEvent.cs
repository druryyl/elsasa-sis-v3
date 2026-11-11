namespace Elsasa.Sis.Contracts.Teaching.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published when a teaching assignment is cancelled or withdrawn (ADR-003).
/// </summary>
/// <param name="AssignmentId">The unique identifier of the cancelled assignment.</param>
/// <param name="AssignmentNumber">The authoritative assignment number.</param>
/// <param name="TeacherProfileId">The teacher profile whose assignment was cancelled.</param>
/// <param name="AcademicPeriodId">The academic period identifier.</param>
/// <param name="ReleasedWorkloadHours">Committed workload hours released back to teacher pool.</param>
/// <param name="CancelledBy">Identity of coordinator or authority who cancelled the assignment.</param>
/// <param name="Reason">Administrative justification for cancellation.</param>
/// <param name="OccurredOn">Timestamp when this event occurred in UTC.</param>
public sealed record TeachingAssignmentCancelledEvent(
    Guid AssignmentId,
    string AssignmentNumber,
    Guid TeacherProfileId,
    Guid AcademicPeriodId,
    decimal ReleasedWorkloadHours,
    string CancelledBy,
    string Reason,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TeachingAssignmentCancelledEvent"/> record with current UTC timestamp.
    /// </summary>
    public TeachingAssignmentCancelledEvent(
        Guid assignmentId,
        string assignmentNumber,
        Guid teacherProfileId,
        Guid academicPeriodId,
        decimal releasedWorkloadHours,
        string cancelledBy,
        string reason)
        : this(
            assignmentId,
            assignmentNumber,
            teacherProfileId,
            academicPeriodId,
            releasedWorkloadHours,
            cancelledBy,
            reason,
            DateTimeOffset.UtcNow)
    {
    }
}
