namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when a student's academic standing is formally determined (ADR-003, UC-ACD-17).
/// </summary>
/// <param name="StandingId">The unique identifier of the determined academic standing record.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="AcademicPeriodId">The unique identifier of the academic period.</param>
/// <param name="Standing">The determined academic standing status ('good_standing', 'conditional_standing', 'not_in_good_standing').</param>
/// <param name="IsException">Indicates whether the standing was determined as an authorized committee exception.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record AcademicStandingDeterminedEvent(
    Guid StandingId,
    Guid StudentId,
    Guid AcademicPeriodId,
    string Standing,
    bool IsException,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcademicStandingDeterminedEvent"/> record with current UTC timestamp.
    /// </summary>
    /// <param name="standingId">The unique identifier of the determined academic standing record.</param>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="academicPeriodId">The unique identifier of the academic period.</param>
    /// <param name="standing">The determined academic standing status.</param>
    /// <param name="isException">Indicates whether the standing was determined as an authorized committee exception.</param>
    public AcademicStandingDeterminedEvent(
        Guid standingId,
        Guid studentId,
        Guid academicPeriodId,
        string standing,
        bool isException)
        : this(standingId, studentId, academicPeriodId, standing, isException, DateTimeOffset.UtcNow)
    {
    }
}
