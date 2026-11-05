namespace Elsasa.Sis.Academics.Features.Assignments.SubmitCoursework;

/// <summary>
/// Response contract representing a recorded coursework submission (TD-06, TD-07, UC-ACD-01, P5-S11).
/// </summary>
/// <param name="Id">The unique identifier of the submission record.</param>
/// <param name="AssignmentId">The unique identifier of the coursework assignment.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="Status">The submission timeliness status ('submitted' or 'late').</param>
/// <param name="SubmittedDate">The UTC timestamp when the coursework was submitted.</param>
/// <param name="SubmissionReference">The submission evidence: external URL, plain text response, or relative upload file path.</param>
/// <param name="IsResubmission">Indicates whether this submission supersedes a prior submission.</param>
/// <param name="PreviousSubmissionId">Identifier of the superseded submission record, if any.</param>
/// <param name="Notes">Optional submission notes or remarks.</param>
/// <param name="CreatedAt">The UTC timestamp when the record was created.</param>
/// <param name="CreatedBy">The username of the user who recorded the submission.</param>
public sealed record SubmitCourseworkResponse(
    Guid Id,
    Guid AssignmentId,
    Guid StudentId,
    string Status,
    DateTimeOffset SubmittedDate,
    string? SubmissionReference,
    bool IsResubmission,
    Guid? PreviousSubmissionId,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy);
