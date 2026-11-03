namespace Elsasa.Sis.Students.Features.Records.Guardians.AddGuardianRelationship;

/// <summary>
/// Request payload to link a guardian relationship to a student (ADR-004, ADR-005).
/// </summary>
/// <param name="FullName">Full legal name of the guardian.</param>
/// <param name="RelationshipType">Relationship type (e.g. mother, father, legal_guardian, grandparent, sibling, other).</param>
/// <param name="GovernmentIdType">Optional government identification type (e.g. NIK, Passport).</param>
/// <param name="GovernmentIdNumber">Optional government identification number.</param>
/// <param name="Phone">Optional primary telephone or mobile number.</param>
/// <param name="Email">Optional email address.</param>
/// <param name="Address">Optional residential or mailing address.</param>
/// <param name="Occupation">Optional occupation or employer description.</param>
/// <param name="Notes">Optional notes regarding the guardian.</param>
/// <param name="GuardianId">Optional existing guardian ID if known.</param>
/// <param name="IsPrimaryContact">Whether this guardian serves as the primary contact for official notices.</param>
/// <param name="IsEmergencyContact">Whether this guardian is designated for emergency communications.</param>
/// <param name="IsFinancialContact">Whether this guardian receives invoices and financial notices.</param>
/// <param name="IsAuthorizedPickup">Whether this guardian is authorized for school pickup.</param>
public sealed record AddGuardianRelationshipRequest(
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
