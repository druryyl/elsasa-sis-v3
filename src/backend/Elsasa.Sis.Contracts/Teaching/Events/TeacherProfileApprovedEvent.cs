namespace Elsasa.Sis.Contracts.Teaching.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published when a teacher profile is formally approved by the Teaching Authority (ADR-003).
/// Transitions teacher standing to 'active'.
/// </summary>
/// <param name="TeacherProfileId">The unique identifier of the approved teacher profile.</param>
/// <param name="TeacherNumber">The authoritative teacher number.</param>
/// <param name="ApprovedBy">The identity of the Teaching Authority who approved the profile.</param>
/// <param name="ApprovedDate">The date when approval was granted.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record TeacherProfileApprovedEvent(
    Guid TeacherProfileId,
    string TeacherNumber,
    string ApprovedBy,
    DateOnly ApprovedDate,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TeacherProfileApprovedEvent"/> record with current UTC timestamp.
    /// </summary>
    public TeacherProfileApprovedEvent(
        Guid teacherProfileId,
        string teacherNumber,
        string approvedBy,
        DateOnly approvedDate)
        : this(teacherProfileId, teacherNumber, approvedBy, approvedDate, DateTimeOffset.UtcNow)
    {
    }
}
