namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when a student is registered for academic participation (ADR-003, TD-08).
/// </summary>
/// <param name="ParticipationId">The unique identifier of the academic participation record.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="LearningUnitId">The unique identifier of the learning unit.</param>
/// <param name="AcademicPeriodId">The unique identifier of the academic period.</param>
/// <param name="SectionLabel">The cohort section label, or null if unassigned.</param>
/// <param name="ParticipationType">The type of participation (standard, repeat, transferred_in, recognized_prior).</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record AcademicParticipationRegisteredEvent(
    Guid ParticipationId,
    Guid StudentId,
    Guid LearningUnitId,
    Guid AcademicPeriodId,
    string? SectionLabel,
    string ParticipationType,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcademicParticipationRegisteredEvent"/> record with current UTC timestamp.
    /// </summary>
    /// <param name="participationId">The unique identifier of the academic participation record.</param>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="learningUnitId">The unique identifier of the learning unit.</param>
    /// <param name="academicPeriodId">The unique identifier of the academic period.</param>
    /// <param name="sectionLabel">The cohort section label, or null if unassigned.</param>
    /// <param name="participationType">The type of participation (standard, repeat, transferred_in, recognized_prior).</param>
    public AcademicParticipationRegisteredEvent(
        Guid participationId,
        Guid studentId,
        Guid learningUnitId,
        Guid academicPeriodId,
        string? sectionLabel,
        string participationType)
        : this(participationId, studentId, learningUnitId, academicPeriodId, sectionLabel, participationType, DateTimeOffset.UtcNow)
    {
    }
}
