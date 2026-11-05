namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when an attendance record is corrected with mandatory audit justification (ADR-003, TD-08).
/// </summary>
/// <param name="AttendanceRecordId">The unique identifier of the modified attendance record.</param>
/// <param name="LearningActivityId">The unique identifier of the associated learning activity.</param>
/// <param name="StudentId">The unique identifier of the affected student.</param>
/// <param name="PreviousStatus">The previous attendance status value.</param>
/// <param name="NewStatus">The updated attendance status value.</param>
/// <param name="Reason">Mandatory justification explaining the correction.</param>
/// <param name="CorrectedBy">Username or identifier of the authority making the correction.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record AttendanceCorrectedEvent(
    Guid AttendanceRecordId,
    Guid LearningActivityId,
    Guid StudentId,
    string PreviousStatus,
    string NewStatus,
    string Reason,
    string CorrectedBy,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AttendanceCorrectedEvent"/> record with current UTC timestamp.
    /// </summary>
    /// <param name="attendanceRecordId">The unique identifier of the modified attendance record.</param>
    /// <param name="learningActivityId">The unique identifier of the associated learning activity.</param>
    /// <param name="studentId">The unique identifier of the affected student.</param>
    /// <param name="previousStatus">The previous attendance status value.</param>
    /// <param name="newStatus">The updated attendance status value.</param>
    /// <param name="reason">Mandatory justification explaining the correction.</param>
    /// <param name="correctedBy">Username or identifier of the authority making the correction.</param>
    public AttendanceCorrectedEvent(
        Guid attendanceRecordId,
        Guid learningActivityId,
        Guid studentId,
        string previousStatus,
        string newStatus,
        string reason,
        string correctedBy)
        : this(attendanceRecordId, learningActivityId, studentId, previousStatus, newStatus, reason, correctedBy, DateTimeOffset.UtcNow)
    {
    }
}
