namespace Elsasa.Sis.Students.Features.Records.Guardians.UpdateGuardianRelationship;

/// <summary>
/// Response payload returned upon successfully updating a student-guardian relationship.
/// </summary>
public sealed record UpdateGuardianRelationshipResponse(
    Guid RelationshipId,
    Guid GuardianId,
    string RelationshipType,
    bool IsPrimaryContact,
    bool IsEmergencyContact,
    bool IsFinancialContact,
    bool IsAuthorizedPickup,
    bool IsActive)
{
    /// <summary>
    /// Alias matching RelationshipId for generic REST clients.
    /// </summary>
    public Guid Id => RelationshipId;
}
