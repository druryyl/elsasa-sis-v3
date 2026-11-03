namespace Elsasa.Sis.Students.Features.Admission.RegisterApplication;

/// <summary>
/// Request payload to register a new admission application via staff intake or public portal (ADR-004, ADR-005).
/// </summary>
public sealed record RegisterApplicationRequest(
    Guid? ApplicantId,
    string FullName,
    DateOnly? DateOfBirth,
    string? Gender = null,
    string? GovernmentIdType = null,
    string? GovernmentIdNumber = null,
    string? Email = null,
    string? Phone = null,
    string? Address = null,
    string? PreviousSchoolName = null,
    string? PreviousSchoolAddress = null,
    Guid AdmissionPeriodId = default,
    string? ApplicationType = "new",
    string RequestedEntryLevel = "",
    string? RequestedCampus = null,
    string? RequestedProgram = null,
    IReadOnlyList<RegisterApplicationContactRequest>? Contacts = null,
    bool? BypassWindowCheck = false,
    string? Notes = null);

/// <summary>
/// Guardian or emergency contact intake details associated with an admission application.
/// </summary>
public sealed record RegisterApplicationContactRequest(
    string FullName,
    string Relationship,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    bool IsPrimary = true);
