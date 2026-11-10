namespace Elsasa.Sis.Contracts.Teaching.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published when a new teacher candidate profile has been registered (ADR-003).
/// </summary>
/// <param name="TeacherProfileId">The unique identifier of the registered teacher profile.</param>
/// <param name="TeacherNumber">The authoritative teacher number.</param>
/// <param name="FullName">The full name of the teacher candidate.</param>
/// <param name="Standing">The initial standing state (typically 'pending_approval').</param>
/// <param name="CreatedBy">The user identity who registered the profile.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record TeacherProfileCreatedEvent(
    Guid TeacherProfileId,
    string TeacherNumber,
    string FullName,
    string Standing,
    string CreatedBy,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TeacherProfileCreatedEvent"/> record with current UTC timestamp.
    /// </summary>
    public TeacherProfileCreatedEvent(
        Guid teacherProfileId,
        string teacherNumber,
        string fullName,
        string standing,
        string createdBy)
        : this(teacherProfileId, teacherNumber, fullName, standing, createdBy, DateTimeOffset.UtcNow)
    {
    }
}
