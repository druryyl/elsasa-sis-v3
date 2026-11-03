namespace Elsasa.Sis.Students.Features.Enrollment.EnrollAcceptedApplicant;

/// <summary>
/// Response payload returned after successfully enrolling an accepted applicant as an official student (ADR-004, ADR-005).
/// </summary>
/// <param name="StudentId">The unique identifier of the newly created official student record.</param>
/// <param name="StudentNumber">The authoritative, sequential student number generated for the student.</param>
/// <param name="EnrollmentId">The unique identifier of the administrative enrollment event record.</param>
public sealed record EnrollAcceptedApplicantResponse(
    Guid StudentId,
    string StudentNumber,
    Guid EnrollmentId);
