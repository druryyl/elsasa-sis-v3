namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when constituent learning units of an approved student learning plan have been synchronized into academic participations.
/// </summary>
/// <param name="PlanId">The unique identifier of the student learning plan that was synchronized.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="SyncedCount">The number of academic participation records inserted or reconciled.</param>
/// <param name="SyncedBy">Username of the actor who triggered the participation synchronization.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record StudentLearningPlanParticipationsSyncedEvent(
    Guid PlanId,
    Guid StudentId,
    int SyncedCount,
    string SyncedBy,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StudentLearningPlanParticipationsSyncedEvent"/> record with the current UTC timestamp.
    /// </summary>
    /// <param name="planId">The unique identifier of the student learning plan.</param>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="syncedCount">The number of academic participation records inserted or reconciled.</param>
    /// <param name="syncedBy">Username of the actor who triggered the synchronization.</param>
    public StudentLearningPlanParticipationsSyncedEvent(
        Guid planId,
        Guid studentId,
        int syncedCount,
        string syncedBy)
        : this(planId, studentId, syncedCount, syncedBy, DateTimeOffset.UtcNow)
    {
    }
}
