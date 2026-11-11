namespace Elsasa.Sis.Contracts.Teaching.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published when a formal workload exception is approved by Teaching Authority (ADR-003).
/// </summary>
/// <param name="ExceptionId">The unique identifier of the approved exception.</param>
/// <param name="ExceptionNumber">The authoritative exception number (e.g. WEX-2026-0001).</param>
/// <param name="TeachingAssignmentId">The assignment triggering the overload.</param>
/// <param name="TeacherProfileId">The teacher profile granted the exception.</param>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="ExceededHours">Number of hours approved above prohibited ceiling.</param>
/// <param name="ApprovedBy">Teaching Authority who granted approval.</param>
/// <param name="ApprovedDate">Date when approval was granted.</param>
/// <param name="OccurredOn">Timestamp when this event occurred in UTC.</param>
public sealed record WorkloadExceptionApprovedEvent(
    Guid ExceptionId,
    string ExceptionNumber,
    Guid TeachingAssignmentId,
    Guid TeacherProfileId,
    Guid AcademicPeriodId,
    decimal ExceededHours,
    string ApprovedBy,
    DateOnly ApprovedDate,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkloadExceptionApprovedEvent"/> record with current UTC timestamp.
    /// </summary>
    public WorkloadExceptionApprovedEvent(
        Guid exceptionId,
        string exceptionNumber,
        Guid teachingAssignmentId,
        Guid teacherProfileId,
        Guid academicPeriodId,
        decimal exceededHours,
        string approvedBy,
        DateOnly approvedDate)
        : this(
            exceptionId,
            exceptionNumber,
            teachingAssignmentId,
            teacherProfileId,
            academicPeriodId,
            exceededHours,
            approvedBy,
            approvedDate,
            DateTimeOffset.UtcNow)
    {
    }
}
