namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when a summative academic result is certified to approved status (ADR-003, UC-ACD-16).
/// </summary>
/// <param name="ResultId">The unique identifier of the approved result.</param>
/// <param name="AssessmentId">The unique identifier of the assessment.</param>
/// <param name="StudentId">The unique identifier of the student, if candidate is an enrolled student.</param>
/// <param name="ApplicantId">The unique identifier of the applicant, if candidate is an applicant.</param>
/// <param name="ScoreNumeric">The approved numeric score, or null if non-numeric.</param>
/// <param name="GradeLetter">The approved letter grade, or null.</param>
/// <param name="ApprovedBy">Username or identifier of the certifying academic authority.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record AcademicResultApprovedEvent(
    Guid ResultId,
    Guid AssessmentId,
    Guid? StudentId,
    Guid? ApplicantId,
    decimal? ScoreNumeric,
    string? GradeLetter,
    string ApprovedBy,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcademicResultApprovedEvent"/> record with current UTC timestamp.
    /// </summary>
    /// <param name="resultId">The unique identifier of the approved result.</param>
    /// <param name="assessmentId">The unique identifier of the assessment.</param>
    /// <param name="studentId">The unique identifier of the student, if candidate is an enrolled student.</param>
    /// <param name="applicantId">The unique identifier of the applicant, if candidate is an applicant.</param>
    /// <param name="scoreNumeric">The approved numeric score, or null if non-numeric.</param>
    /// <param name="gradeLetter">The approved letter grade, or null.</param>
    /// <param name="approvedBy">Username or identifier of the certifying academic authority.</param>
    public AcademicResultApprovedEvent(
        Guid resultId,
        Guid assessmentId,
        Guid? studentId,
        Guid? applicantId,
        decimal? scoreNumeric,
        string? gradeLetter,
        string approvedBy)
        : this(resultId, assessmentId, studentId, applicantId, scoreNumeric, gradeLetter, approvedBy, DateTimeOffset.UtcNow)
    {
    }
}
