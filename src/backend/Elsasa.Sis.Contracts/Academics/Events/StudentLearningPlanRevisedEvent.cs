namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when a new draft revision is created from an active student learning plan.
/// </summary>
/// <param name="PriorPlanId">The unique identifier of the active plan being revised.</param>
/// <param name="NewPlanId">The unique identifier of the newly cloned draft plan revision.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="AcademicPeriodId">The unique identifier of the associated academic period.</param>
/// <param name="NewVersion">The version number of the new plan revision (PriorPlan.Version + 1).</param>
/// <param name="RevisedBy">Username of the actor who initiated the revision.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record StudentLearningPlanRevisedEvent(
    Guid PriorPlanId,
    Guid NewPlanId,
    Guid StudentId,
    Guid AcademicPeriodId,
    int NewVersion,
    string RevisedBy,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StudentLearningPlanRevisedEvent"/> record with the current UTC timestamp.
    /// </summary>
    /// <param name="priorPlanId">The unique identifier of the active plan being revised.</param>
    /// <param name="newPlanId">The unique identifier of the newly cloned draft plan revision.</param>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="academicPeriodId">The unique identifier of the academic period.</param>
    /// <param name="newVersion">The version number of the new plan revision.</param>
    /// <param name="revisedBy">Username of the actor who initiated the revision.</param>
    public StudentLearningPlanRevisedEvent(
        Guid priorPlanId,
        Guid newPlanId,
        Guid studentId,
        Guid academicPeriodId,
        int newVersion,
        string revisedBy)
        : this(priorPlanId, newPlanId, studentId, academicPeriodId, newVersion, revisedBy, DateTimeOffset.UtcNow)
    {
    }
}
