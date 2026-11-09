namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when a student's learning unit progress is updated (ADR-003, UC-ACD-11, UC-ACD-12).
/// </summary>
/// <param name="ProgressId">The unique identifier of the learning progress record.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="LearningUnitId">The unique identifier of the learning unit.</param>
/// <param name="AcademicPeriodId">The unique identifier of the academic period.</param>
/// <param name="Status">The categorical progress status ('not_started', 'in_progress', 'completed', 'needs_remediation', 'exempted').</param>
/// <param name="CompletionDate">The formal date of completion, or null if incomplete.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record LearningProgressUpdatedEvent(
    Guid ProgressId,
    Guid StudentId,
    Guid LearningUnitId,
    Guid AcademicPeriodId,
    string Status,
    DateOnly? CompletionDate,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LearningProgressUpdatedEvent"/> record with current UTC timestamp.
    /// </summary>
    /// <param name="progressId">The unique identifier of the learning progress record.</param>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="learningUnitId">The unique identifier of the learning unit.</param>
    /// <param name="academicPeriodId">The unique identifier of the academic period.</param>
    /// <param name="status">The categorical progress status.</param>
    /// <param name="completionDate">The formal date of completion, or null if incomplete.</param>
    public LearningProgressUpdatedEvent(
        Guid progressId,
        Guid studentId,
        Guid learningUnitId,
        Guid academicPeriodId,
        string status,
        DateOnly? completionDate)
        : this(progressId, studentId, learningUnitId, academicPeriodId, status, completionDate, DateTimeOffset.UtcNow)
    {
    }
}
