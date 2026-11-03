namespace Elsasa.Sis.Students.Features.Records.Guardians.AddGuardianRelationship;

/// <summary>
/// Response returned upon successfully creating a student-guardian relationship.
/// </summary>
/// <param name="RelationshipId">Unique identifier of the newly created relationship.</param>
/// <param name="GuardianId">Unique identifier of the associated guardian.</param>
public sealed record AddGuardianRelationshipResponse(
    Guid RelationshipId,
    Guid GuardianId)
{
    /// <summary>
    /// Alias matching RelationshipId for generic REST clients.
    /// </summary>
    public Guid Id => RelationshipId;
}
