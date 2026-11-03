namespace Elsasa.Sis.Students.Features.Admission.GetAdmissionApplicationsList;

/// <summary>
/// Projection item for the admission queue worklist (ADR-004, ADR-005).
/// </summary>
public sealed record AdmissionApplicationListItemResponse(
    Guid Id,
    string ApplicationNumber,
    string ApplicantName,
    string? GovernmentIdNumber,
    Guid AdmissionPeriodId,
    string RequestedEntryLevel,
    string Status,
    DateOnly RegistrationDate);
