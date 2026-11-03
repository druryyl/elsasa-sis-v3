namespace Elsasa.Sis.Students.Features.Records.Guardians.UpdateGuardianRelationship;

/// <summary>
/// Request payload to update an existing student-guardian relationship (ADR-004, ADR-005).
/// </summary>
/// <param name="RelationshipType">Relationship type (e.g. mother, father, legal_guardian, grandparent, sibling, other).</param>
/// <param name="IsPrimaryContact">Whether this guardian serves as the primary contact for official notices.</param>
/// <param name="IsEmergencyContact">Whether this guardian is designated for emergency communications.</param>
/// <param name="IsFinancialContact">Whether this guardian receives invoices and financial notices.</param>
/// <param name="IsAuthorizedPickup">Whether this guardian is authorized for school pickup.</param>
/// <param name="IsActive">Whether this relationship is active.</param>
/// <param name="Phone">Optional updated primary phone number for the guardian.</param>
/// <param name="Email">Optional updated email address for the guardian.</param>
/// <param name="Address">Optional updated residential address for the guardian.</param>
/// <param name="Occupation">Optional updated occupation for the guardian.</param>
/// <param name="Notes">Optional administrative notes.</param>
public sealed record UpdateGuardianRelationshipRequest(
    string RelationshipType,
    bool IsPrimaryContact = false,
    bool IsEmergencyContact = false,
    bool IsFinancialContact = false,
    bool IsAuthorizedPickup = false,
    bool IsActive = true,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    string? Occupation = null,
    string? Notes = null);
