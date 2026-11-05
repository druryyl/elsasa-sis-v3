namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when a student's participation in a learning unit is concluded (ADR-003, TD-08).
/// </summary>
/// <param name="ParticipationId">The unique identifier of the concluded academic participation.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="LearningUnitId">The unique identifier of the learning unit.</param>
/// <param name="CompletionDate">The formal completion date of the participation.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record ParticipationConcludedEvent(
    Guid ParticipationId,
    Guid StudentId,
    Guid LearningUnitId,
    DateOnly CompletionDate,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParticipationConcludedEvent"/> record with current UTC timestamp.
    /// </summary>
    /// <param name="participationId">The unique identifier of the concluded academic participation.</param>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="learningUnitId">The unique identifier of the learning unit.</param>
    /// <param name="completionDate">The formal completion date of the participation.</param>
    public ParticipationConcludedEvent(
        Guid participationId,
        Guid studentId,
        Guid learningUnitId,
        DateOnly completionDate)
        : this(participationId, studentId, learningUnitId, completionDate, DateTimeOffset.UtcNow)
    {
    }
}
