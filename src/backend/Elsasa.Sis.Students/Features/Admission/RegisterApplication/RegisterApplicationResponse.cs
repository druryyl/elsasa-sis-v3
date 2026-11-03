namespace Elsasa.Sis.Students.Features.Admission.RegisterApplication;

/// <summary>
/// Response returned upon successful registration of an admission application (ADR-005).
/// </summary>
/// <param name="Id">The unique identifier of the created admission application.</param>
/// <param name="ApplicationNumber">The human-readable traceable application number (e.g. ADM-2026-0001).</param>
/// <param name="ApplicantId">The unique identifier of the matched or newly created applicant record.</param>
/// <param name="Status">The initial status of the admission application (typically 'registered').</param>
/// <param name="RegistrationDate">The date when the application was formally registered.</param>
public sealed record RegisterApplicationResponse(
    Guid Id,
    string ApplicationNumber,
    Guid ApplicantId,
    string Status,
    DateOnly RegistrationDate);
