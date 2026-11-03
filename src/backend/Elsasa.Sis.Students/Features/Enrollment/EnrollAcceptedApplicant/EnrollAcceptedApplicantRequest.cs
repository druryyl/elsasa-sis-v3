namespace Elsasa.Sis.Students.Features.Enrollment.EnrollAcceptedApplicant;

/// <summary>
/// Guardian details supplied during applicant enrollment.
/// </summary>
/// <param name="FullName">Full legal name of the guardian.</param>
/// <param name="RelationshipType">Relationship type (e.g. mother, father, legal_guardian, etc.).</param>
/// <param name="GovernmentIdType">Optional government identification type (e.g. NIK, Passport).</param>
/// <param name="GovernmentIdNumber">Optional government identification number.</param>
/// <param name="Phone">Optional primary contact phone number.</param>
/// <param name="Email">Optional contact email address.</param>
/// <param name="Address">Optional residential address.</param>
/// <param name="Occupation">Optional occupation or employment details.</param>
/// <param name="Notes">Optional notes regarding the guardian.</param>
/// <param name="GuardianId">Optional existing guardian ID if known and already registered.</param>
/// <param name="IsPrimaryContact">Whether this guardian serves as the active primary contact for official notices.</param>
/// <param name="IsEmergencyContact">Whether this guardian is designated for emergency communications.</param>
/// <param name="IsFinancialContact">Whether this guardian receives invoices and financial notices.</param>
/// <param name="IsAuthorizedPickup">Whether this guardian is authorized for school pickup.</param>
public sealed record EnrollGuardianRequest(
    string FullName,
    string RelationshipType,
    string? GovernmentIdType = null,
    string? GovernmentIdNumber = null,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    string? Occupation = null,
    string? Notes = null,
    Guid? GuardianId = null,
    bool IsPrimaryContact = false,
    bool IsEmergencyContact = false,
    bool IsFinancialContact = false,
    bool IsAuthorizedPickup = false);

/// <summary>
/// Request payload to enroll an accepted applicant as an official student (ADR-004, ADR-005, UC-STD-04).
/// </summary>
/// <param name="ApplicantId">The unique identifier of the originating prospective applicant.</param>
/// <param name="AcademicYearId">The academic year identifier for which the student is enrolled.</param>
/// <param name="InitialCampus">Initial administrative campus placement.</param>
/// <param name="InitialLevel">Initial grade/level placement.</param>
/// <param name="InitialAcademicYear">Authoritative textual academic year code (e.g. 2026/2027).</param>
/// <param name="InitialProgram">Optional initial curriculum/program track.</param>
/// <param name="InitialCohort">Optional initial cohort/homeroom assignment.</param>
/// <param name="PaymentWaived">Whether institutional enrollment fees have been waived/granted scholarship.</param>
/// <param name="PaymentReference">Bank receipt or payment reference code when fee is paid manually.</param>
/// <param name="WaiverReference">Official scholarship decree or authorized justification when fee is waived.</param>
/// <param name="CheckFinanceModule">Whether to perform synchronous verification against IFinanceModule.</param>
/// <param name="Guardians">Optional list of guardians to ingest or link. If omitted, admission contacts are used.</param>
/// <param name="Notes">Optional administrative enrollment notes.</param>
/// <param name="ApplicationId">Optional specific admission application ID. If omitted, looked up by applicant ID.</param>
public sealed record EnrollAcceptedApplicantRequest(
    Guid ApplicantId,
    Guid AcademicYearId,
    string InitialCampus,
    string InitialLevel,
    string InitialAcademicYear,
    string? InitialProgram = null,
    string? InitialCohort = null,
    bool PaymentWaived = false,
    string? PaymentReference = null,
    string? WaiverReference = null,
    bool? CheckFinanceModule = null,
    IReadOnlyList<EnrollGuardianRequest>? Guardians = null,
    string? Notes = null,
    Guid? ApplicationId = null);
