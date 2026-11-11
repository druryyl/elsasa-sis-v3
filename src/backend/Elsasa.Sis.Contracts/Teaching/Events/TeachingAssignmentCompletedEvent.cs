namespace Elsasa.Sis.Contracts.Teaching.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published when a teaching assignment is formally completed and capacity released (ADR-003).
/// </summary>
/// <param name="AssignmentId">The unique identifier of the completed assignment.</param>
/// <param name="CompletionId">The completion record identifier.</param>
/// <param name="AssignmentNumber">The authoritative assignment number.</param>
/// <param name="TeacherProfileId">The teacher profile whose assignment completed.</param>
/// <param name="AcademicPeriodId">The academic period identifier.</param>
/// <param name="ReleasedWorkloadHours">Committed workload hours released back to teacher pool.</param>
/// <param name="CompletionDate">Date of formal assignment completion.</param>
/// <param name="CompletedBy">Coordinator who concluded the assignment.</param>
/// <param name="OccurredOn">Timestamp when this event occurred in UTC.</param>
public sealed record TeachingAssignmentCompletedEvent(
    Guid AssignmentId,
    Guid CompletionId,
    string AssignmentNumber,
    Guid TeacherProfileId,
    Guid AcademicPeriodId,
    decimal ReleasedWorkloadHours,
    DateOnly CompletionDate,
    string CompletedBy,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TeachingAssignmentCompletedEvent"/> record with current UTC timestamp.
    /// </summary>
    public TeachingAssignmentCompletedEvent(
        Guid assignmentId,
        Guid completionId,
        string assignmentNumber,
        Guid teacherProfileId,
        Guid academicPeriodId,
        decimal releasedWorkloadHours,
        DateOnly completionDate,
        string completedBy)
        : this(
            assignmentId,
            completionId,
            assignmentNumber,
            teacherProfileId,
            academicPeriodId,
            releasedWorkloadHours,
            completionDate,
            completedBy,
            DateTimeOffset.UtcNow)
    {
    }
}
