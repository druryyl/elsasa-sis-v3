namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when a coursework assignment is released to students (ADR-003, TD-08).
/// </summary>
/// <param name="AssignmentId">The unique identifier of the released assignment.</param>
/// <param name="LearningActivityId">The unique identifier of the originating learning activity.</param>
/// <param name="Title">The title of the released assignment.</param>
/// <param name="DueDate">The due date for submissions, or null if open-ended.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record AssignmentReleasedEvent(
    Guid AssignmentId,
    Guid LearningActivityId,
    string Title,
    DateOnly? DueDate,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssignmentReleasedEvent"/> record with current UTC timestamp.
    /// </summary>
    /// <param name="assignmentId">The unique identifier of the released assignment.</param>
    /// <param name="learningActivityId">The unique identifier of the originating learning activity.</param>
    /// <param name="title">The title of the released assignment.</param>
    /// <param name="dueDate">The due date for submissions, or null if open-ended.</param>
    public AssignmentReleasedEvent(
        Guid assignmentId,
        Guid learningActivityId,
        string title,
        DateOnly? dueDate)
        : this(assignmentId, learningActivityId, title, dueDate, DateTimeOffset.UtcNow)
    {
    }
}
