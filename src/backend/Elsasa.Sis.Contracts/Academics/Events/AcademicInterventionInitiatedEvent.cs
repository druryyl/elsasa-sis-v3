namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when an academic intervention plan is initiated (ADR-003, UC-ACD-11).
/// </summary>
/// <param name="InterventionId">The unique identifier of the academic intervention record.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="LearningUnitId">The unique identifier of the target learning unit.</param>
/// <param name="InterventionType">The category of intervention (e.g., 'remediation', 'reassessment', 'extension_work').</param>
/// <param name="TriggerType">The dual-trigger source type (e.g., 'assessment_below_threshold', 'teacher_referral').</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record AcademicInterventionInitiatedEvent(
    Guid InterventionId,
    Guid StudentId,
    Guid LearningUnitId,
    string InterventionType,
    string TriggerType,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcademicInterventionInitiatedEvent"/> record with current UTC timestamp.
    /// </summary>
    /// <param name="interventionId">The unique identifier of the academic intervention record.</param>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="learningUnitId">The unique identifier of the target learning unit.</param>
    /// <param name="interventionType">The category of intervention.</param>
    /// <param name="triggerType">The dual-trigger source type.</param>
    public AcademicInterventionInitiatedEvent(
        Guid interventionId,
        Guid studentId,
        Guid learningUnitId,
        string interventionType,
        string triggerType)
        : this(interventionId, studentId, learningUnitId, interventionType, triggerType, DateTimeOffset.UtcNow)
    {
    }
}
