namespace Elsasa.Sis.Academics.Features.Assignments.RecordSubmissionException;

/// <summary>
/// Response contract representing a recorded submission exception (TD-06, P5-S11).
/// </summary>
/// <param name="Id">The unique identifier of the submission exception record.</param>
/// <param name="AssignmentId">The unique identifier of the coursework assignment.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="Status">The recorded exception status ('excused' or 'exempted').</param>
/// <param name="SubmittedDate">Timestamp of submission delivery (null for administrative exceptions).</param>
/// <param name="SubmissionReference">Optional submission reference.</param>
/// <param name="IsResubmission">Indicates whether this exception supersedes a previous submission.</param>
/// <param name="PreviousSubmissionId">Identifier of the superseded submission record, if any.</param>
/// <param name="Notes">The justification reason recorded for the policy exception.</param>
/// <param name="CreatedAt">The UTC timestamp when the record was created.</param>
/// <param name="CreatedBy">The username of the authority/teacher who granted the exception.</param>
public sealed record RecordSubmissionExceptionResponse(
    Guid Id,
    Guid AssignmentId,
    Guid StudentId,
    string Status,
    DateTimeOffset? SubmittedDate,
    string? SubmissionReference,
    bool IsResubmission,
    Guid? PreviousSubmissionId,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy);
