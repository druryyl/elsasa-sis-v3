namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when an approved academic result is corrected (ADR-003, UC-ACD-19, TD-06).
/// </summary>
/// <param name="ResultId">The unique identifier of the corrected result.</param>
/// <param name="AssessmentId">The unique identifier of the assessment.</param>
/// <param name="StudentId">The unique identifier of the student, or null if applicant.</param>
/// <param name="Reason">The mandatory justification for the correction.</param>
/// <param name="IsMaterial">Indicates whether the alteration was material (e.g. crossing passing threshold).</param>
/// <param name="CorrectedBy">Username of the authority who executed the correction.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record AcademicResultCorrectedEvent(
    Guid ResultId,
    Guid AssessmentId,
    Guid? StudentId,
    string Reason,
    bool IsMaterial,
    string CorrectedBy,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcademicResultCorrectedEvent"/> record with current UTC timestamp.
    /// </summary>
    /// <param name="resultId">The unique identifier of the corrected result.</param>
    /// <param name="assessmentId">The unique identifier of the assessment.</param>
    /// <param name="studentId">The unique identifier of the student, or null if applicant.</param>
    /// <param name="reason">The mandatory justification for the correction.</param>
    /// <param name="isMaterial">Indicates whether the alteration was material.</param>
    /// <param name="correctedBy">Username of the authority who executed the correction.</param>
    public AcademicResultCorrectedEvent(
        Guid resultId,
        Guid assessmentId,
        Guid? studentId,
        string reason,
        bool isMaterial,
        string correctedBy)
        : this(resultId, assessmentId, studentId, reason, isMaterial, correctedBy, DateTimeOffset.UtcNow)
    {
    }
}
