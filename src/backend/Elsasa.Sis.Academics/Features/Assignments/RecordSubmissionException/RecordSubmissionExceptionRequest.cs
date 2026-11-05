namespace Elsasa.Sis.Academics.Features.Assignments.RecordSubmissionException;

/// <summary>
/// Request contract for recording an administrative policy exception for a student assignment (TD-06, P5-S11).
/// </summary>
/// <param name="StudentId">The unique identifier of the student receiving the exception.</param>
/// <param name="ExceptionType">The exception policy type ('excused' or 'exempted').</param>
/// <param name="Reason">Mandatory justification reason for granting the policy exception.</param>
public sealed record RecordSubmissionExceptionRequest(
    Guid StudentId,
    string ExceptionType,
    string Reason);
