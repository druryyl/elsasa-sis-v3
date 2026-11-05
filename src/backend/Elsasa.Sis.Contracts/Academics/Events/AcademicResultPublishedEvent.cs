namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when academic results are published for student/parent portal visibility (ADR-003).
/// </summary>
/// <param name="ResultId">The unique identifier of the published result.</param>
/// <param name="AssessmentId">The unique identifier of the assessment.</param>
/// <param name="StudentId">The unique identifier of the student, or null if applicant.</param>
/// <param name="ScoreNumeric">The published numeric score, or null if non-numeric.</param>
/// <param name="GradeLetter">The published letter grade, or null.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record AcademicResultPublishedEvent(
    Guid ResultId,
    Guid AssessmentId,
    Guid? StudentId,
    decimal? ScoreNumeric,
    string? GradeLetter,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcademicResultPublishedEvent"/> record with current UTC timestamp.
    /// </summary>
    /// <param name="resultId">The unique identifier of the published result.</param>
    /// <param name="assessmentId">The unique identifier of the assessment.</param>
    /// <param name="studentId">The unique identifier of the student, or null if applicant.</param>
    /// <param name="scoreNumeric">The published numeric score, or null if non-numeric.</param>
    /// <param name="gradeLetter">The published letter grade, or null.</param>
    public AcademicResultPublishedEvent(
        Guid resultId,
        Guid assessmentId,
        Guid? studentId,
        decimal? scoreNumeric,
        string? gradeLetter)
        : this(resultId, assessmentId, studentId, scoreNumeric, gradeLetter, DateTimeOffset.UtcNow)
    {
    }
}
