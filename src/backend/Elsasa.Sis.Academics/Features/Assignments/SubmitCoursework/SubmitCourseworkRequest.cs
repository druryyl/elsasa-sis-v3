namespace Elsasa.Sis.Academics.Features.Assignments.SubmitCoursework;

/// <summary>
/// Request contract for submitting coursework for an assignment (TD-07, UC-ACD-01, P5-S11).
/// </summary>
/// <param name="StudentId">The unique identifier of the submitting student.</param>
/// <param name="SubmissionReference">The submission evidence: external URL, plain text response, or relative upload file path.</param>
/// <param name="Notes">Optional student notes or delivery remarks.</param>
public sealed record SubmitCourseworkRequest(
    Guid StudentId,
    string SubmissionReference,
    string? Notes = null);
