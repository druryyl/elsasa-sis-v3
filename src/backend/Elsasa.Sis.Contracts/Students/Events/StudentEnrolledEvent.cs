namespace Elsasa.Sis.Contracts.Students.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published when an applicant has been officially enrolled as a student (ADR-003).
/// Triggers post-commit side-effects in other domains, such as obligation assessment in Finance.
/// </summary>
/// <param name="StudentId">The unique identifier of the enrolled student.</param>
/// <param name="ApplicantId">The originating applicant identifier.</param>
/// <param name="AcademicYearId">The academic year identifier for which the student is enrolled.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record StudentEnrolledEvent(
    Guid StudentId,
    Guid ApplicantId,
    Guid AcademicYearId,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StudentEnrolledEvent"/> record with the current UTC timestamp.
    /// </summary>
    /// <param name="studentId">The unique identifier of the enrolled student.</param>
    /// <param name="applicantId">The originating applicant identifier.</param>
    /// <param name="academicYearId">The academic year identifier for which the student is enrolled.</param>
    public StudentEnrolledEvent(Guid studentId, Guid applicantId, Guid academicYearId)
        : this(studentId, applicantId, academicYearId, DateTimeOffset.UtcNow)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StudentEnrolledEvent"/> record with a default applicant identifier and current UTC timestamp.
    /// </summary>
    /// <param name="studentId">The unique identifier of the enrolled student.</param>
    /// <param name="academicYearId">The academic year identifier for which the student is enrolled.</param>
    public StudentEnrolledEvent(Guid studentId, Guid academicYearId)
        : this(studentId, Guid.Empty, academicYearId, DateTimeOffset.UtcNow)
    {
    }
}
