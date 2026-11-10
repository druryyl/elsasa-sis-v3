namespace Elsasa.Sis.Contracts.Teaching.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published when a teacher profile's standing state is altered (ADR-003).
/// </summary>
/// <param name="TeacherProfileId">The unique identifier of the teacher profile.</param>
/// <param name="PreviousStanding">The standing state prior to this change.</param>
/// <param name="NewStanding">The updated standing state (e.g., restricted, suspended, inactive, active).</param>
/// <param name="Reason">The mandatory justification for the standing change.</param>
/// <param name="ChangedBy">The user identity who altered the standing state.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record TeacherStandingChangedEvent(
    Guid TeacherProfileId,
    string PreviousStanding,
    string NewStanding,
    string Reason,
    string ChangedBy,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TeacherStandingChangedEvent"/> record with current UTC timestamp.
    /// </summary>
    public TeacherStandingChangedEvent(
        Guid teacherProfileId,
        string previousStanding,
        string newStanding,
        string reason,
        string changedBy)
        : this(teacherProfileId, previousStanding, newStanding, reason, changedBy, DateTimeOffset.UtcNow)
    {
    }
}
