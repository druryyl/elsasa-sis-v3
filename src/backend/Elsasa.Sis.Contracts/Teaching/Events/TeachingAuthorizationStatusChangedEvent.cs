namespace Elsasa.Sis.Contracts.Teaching.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published when a teaching authorization's status is modified (ADR-003).
/// </summary>
/// <param name="AuthorizationId">The unique identifier of the teaching authorization.</param>
/// <param name="TeacherProfileId">The unique identifier of the owning teacher profile.</param>
/// <param name="PreviousStatus">The authorization status prior to this change.</param>
/// <param name="NewStatus">The updated status (active, restricted, suspended, revoked, expired).</param>
/// <param name="Reason">The justification for the status change.</param>
/// <param name="ChangedBy">The user identity who modified the authorization status.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record TeachingAuthorizationStatusChangedEvent(
    Guid AuthorizationId,
    Guid TeacherProfileId,
    string PreviousStatus,
    string NewStatus,
    string Reason,
    string ChangedBy,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TeachingAuthorizationStatusChangedEvent"/> record with current UTC timestamp.
    /// </summary>
    public TeachingAuthorizationStatusChangedEvent(
        Guid authorizationId,
        Guid teacherProfileId,
        string previousStatus,
        string newStatus,
        string reason,
        string changedBy)
        : this(authorizationId, teacherProfileId, previousStatus, newStatus, reason, changedBy, DateTimeOffset.UtcNow)
    {
    }
}
