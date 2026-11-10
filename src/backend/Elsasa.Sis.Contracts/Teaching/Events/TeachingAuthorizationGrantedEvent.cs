namespace Elsasa.Sis.Contracts.Teaching.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published when a teaching authorization is granted to a teacher profile (ADR-003).
/// </summary>
/// <param name="AuthorizationId">The unique identifier of the granted teaching authorization.</param>
/// <param name="TeacherProfileId">The unique identifier of the authorized teacher profile.</param>
/// <param name="AuthorizationNumber">The authoritative authorization identifier number.</param>
/// <param name="Status">The authorization status ('active' or 'authorized_under_supervision').</param>
/// <param name="HasUnverifiedWarning">Indicates whether the authorization carries an unverified credential warning.</param>
/// <param name="GrantedBy">The identity of the authority who granted the authorization.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record TeachingAuthorizationGrantedEvent(
    Guid AuthorizationId,
    Guid TeacherProfileId,
    string AuthorizationNumber,
    string Status,
    bool HasUnverifiedWarning,
    string GrantedBy,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TeachingAuthorizationGrantedEvent"/> record with current UTC timestamp.
    /// </summary>
    public TeachingAuthorizationGrantedEvent(
        Guid authorizationId,
        Guid teacherProfileId,
        string authorizationNumber,
        string status,
        bool hasUnverifiedWarning,
        string grantedBy)
        : this(authorizationId, teacherProfileId, authorizationNumber, status, hasUnverifiedWarning, grantedBy, DateTimeOffset.UtcNow)
    {
    }
}
