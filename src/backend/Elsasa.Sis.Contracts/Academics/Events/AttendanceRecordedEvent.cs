namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when roll-call attendance is recorded for a learning activity session (ADR-003, TD-08).
/// </summary>
/// <param name="LearningActivityId">The unique identifier of the learning activity.</param>
/// <param name="RecordedDate">The date when roll call was recorded.</param>
/// <param name="TotalPresent">Total count of students marked present.</param>
/// <param name="TotalAbsent">Total count of students marked absent.</param>
/// <param name="TotalLate">Total count of students marked late.</param>
/// <param name="TotalExcused">Total count of students marked excused absence.</param>
/// <param name="RecordedBy">Username or identifier of the teacher/actor who recorded attendance.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record AttendanceRecordedEvent(
    Guid LearningActivityId,
    DateOnly RecordedDate,
    int TotalPresent,
    int TotalAbsent,
    int TotalLate,
    int TotalExcused,
    string RecordedBy,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AttendanceRecordedEvent"/> record with current UTC timestamp.
    /// </summary>
    /// <param name="learningActivityId">The unique identifier of the learning activity.</param>
    /// <param name="recordedDate">The date when roll call was recorded.</param>
    /// <param name="totalPresent">Total count of students marked present.</param>
    /// <param name="totalAbsent">Total count of students marked absent.</param>
    /// <param name="totalLate">Total count of students marked late.</param>
    /// <param name="totalExcused">Total count of students marked excused absence.</param>
    /// <param name="recordedBy">Username or identifier of the teacher/actor who recorded attendance.</param>
    public AttendanceRecordedEvent(
        Guid learningActivityId,
        DateOnly recordedDate,
        int totalPresent,
        int totalAbsent,
        int totalLate,
        int totalExcused,
        string recordedBy)
        : this(learningActivityId, recordedDate, totalPresent, totalAbsent, totalLate, totalExcused, recordedBy, DateTimeOffset.UtcNow)
    {
    }
}
