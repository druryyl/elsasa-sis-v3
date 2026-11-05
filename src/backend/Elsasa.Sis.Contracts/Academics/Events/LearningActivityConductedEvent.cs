namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when an instructional learning activity is conducted and delivery notes are logged (ADR-003, TD-08).
/// </summary>
/// <param name="ActivityId">The unique identifier of the conducted learning activity.</param>
/// <param name="LearningUnitId">The unique identifier of the owning learning unit.</param>
/// <param name="ActivityDate">The date on which the activity was conducted.</param>
/// <param name="ActivityType">The instructional activity type.</param>
/// <param name="SectionLabel">The cohort section label, or null if unassigned.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record LearningActivityConductedEvent(
    Guid ActivityId,
    Guid LearningUnitId,
    DateOnly ActivityDate,
    string ActivityType,
    string? SectionLabel,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LearningActivityConductedEvent"/> record with current UTC timestamp.
    /// </summary>
    /// <param name="activityId">The unique identifier of the conducted learning activity.</param>
    /// <param name="learningUnitId">The unique identifier of the owning learning unit.</param>
    /// <param name="activityDate">The date on which the activity was conducted.</param>
    /// <param name="activityType">The instructional activity type.</param>
    /// <param name="sectionLabel">The cohort section label, or null if unassigned.</param>
    public LearningActivityConductedEvent(
        Guid activityId,
        Guid learningUnitId,
        DateOnly activityDate,
        string activityType,
        string? sectionLabel)
        : this(activityId, learningUnitId, activityDate, activityType, sectionLabel, DateTimeOffset.UtcNow)
    {
    }
}
