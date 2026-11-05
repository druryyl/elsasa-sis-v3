namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when a student submits coursework (ADR-003, TD-08).
/// </summary>
/// <param name="SubmissionId">The unique identifier of the submission record.</param>
/// <param name="AssignmentId">The unique identifier of the coursework assignment.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="Status">The submission status (e.g., submitted, late).</param>
/// <param name="SubmittedDate">Timestamp of submission delivery.</param>
/// <param name="IsResubmission">Indicates whether this submission supersedes a previous submission.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record CourseworkSubmittedEvent(
    Guid SubmissionId,
    Guid AssignmentId,
    Guid StudentId,
    string Status,
    DateTimeOffset SubmittedDate,
    bool IsResubmission,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CourseworkSubmittedEvent"/> record with current UTC timestamp.
    /// </summary>
    /// <param name="submissionId">The unique identifier of the submission record.</param>
    /// <param name="assignmentId">The unique identifier of the coursework assignment.</param>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="status">The submission status (e.g., submitted, late).</param>
    /// <param name="submittedDate">Timestamp of submission delivery.</param>
    /// <param name="isResubmission">Indicates whether this submission supersedes a previous submission.</param>
    public CourseworkSubmittedEvent(
        Guid submissionId,
        Guid assignmentId,
        Guid studentId,
        string status,
        DateTimeOffset submittedDate,
        bool isResubmission)
        : this(submissionId, assignmentId, studentId, status, submittedDate, isResubmission, DateTimeOffset.UtcNow)
    {
    }
}
