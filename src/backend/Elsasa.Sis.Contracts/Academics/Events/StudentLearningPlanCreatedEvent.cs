namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when a new student learning plan is formulated (draft v1).
/// </summary>
/// <param name="PlanId">The unique identifier of the newly created student learning plan.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="AcademicPeriodId">The unique identifier of the associated academic period.</param>
/// <param name="Version">The initial plan version (typically 1).</param>
/// <param name="CreatedBy">Username of the actor who formulated the plan.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record StudentLearningPlanCreatedEvent(
    Guid PlanId,
    Guid StudentId,
    Guid AcademicPeriodId,
    int Version,
    string CreatedBy,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StudentLearningPlanCreatedEvent"/> record with the current UTC timestamp.
    /// </summary>
    /// <param name="planId">The unique identifier of the student learning plan.</param>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="academicPeriodId">The unique identifier of the academic period.</param>
    /// <param name="version">The initial plan version.</param>
    /// <param name="createdBy">Username of the actor who formulated the plan.</param>
    public StudentLearningPlanCreatedEvent(
        Guid planId,
        Guid studentId,
        Guid academicPeriodId,
        int version,
        string createdBy)
        : this(planId, studentId, academicPeriodId, version, createdBy, DateTimeOffset.UtcNow)
    {
    }
}
