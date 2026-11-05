namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when a learning structure has been approved and activated (ADR-003, TD-09).
/// </summary>
/// <param name="LearningStructureId">The unique identifier of the activated learning structure.</param>
/// <param name="AcademicPeriodId">The unique identifier of the operational period bound to the structure.</param>
/// <param name="Version">The semantic version of the activated structure.</param>
/// <param name="EffectiveDate">The date when the activated structure took effect.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record LearningStructureActivatedEvent(
    Guid LearningStructureId,
    Guid AcademicPeriodId,
    string Version,
    DateOnly EffectiveDate,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LearningStructureActivatedEvent"/> record with the current UTC timestamp.
    /// </summary>
    /// <param name="learningStructureId">The unique identifier of the activated learning structure.</param>
    /// <param name="academicPeriodId">The unique identifier of the operational period bound to the structure.</param>
    /// <param name="version">The semantic version of the activated structure.</param>
    /// <param name="effectiveDate">The date when the activated structure took effect.</param>
    public LearningStructureActivatedEvent(
        Guid learningStructureId,
        Guid academicPeriodId,
        string version,
        DateOnly effectiveDate)
        : this(learningStructureId, academicPeriodId, version, effectiveDate, DateTimeOffset.UtcNow)
    {
    }
}
