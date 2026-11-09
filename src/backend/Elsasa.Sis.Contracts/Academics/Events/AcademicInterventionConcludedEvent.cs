namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when an academic intervention is concluded or resolved (ADR-003, UC-ACD-11).
/// </summary>
/// <param name="InterventionId">The unique identifier of the academic intervention record.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="OutcomeNotes">The outcome summary narrative or notes recorded upon conclusion.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record AcademicInterventionConcludedEvent(
    Guid InterventionId,
    Guid StudentId,
    string OutcomeNotes,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcademicInterventionConcludedEvent"/> record with current UTC timestamp.
    /// </summary>
    /// <param name="interventionId">The unique identifier of the academic intervention record.</param>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="outcomeNotes">The outcome summary narrative or notes recorded upon conclusion.</param>
    public AcademicInterventionConcludedEvent(
        Guid interventionId,
        Guid studentId,
        string outcomeNotes)
        : this(interventionId, studentId, outcomeNotes, DateTimeOffset.UtcNow)
    {
    }
}
