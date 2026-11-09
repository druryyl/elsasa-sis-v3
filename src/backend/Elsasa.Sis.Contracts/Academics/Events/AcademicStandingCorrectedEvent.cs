namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when an approved academic standing determination is corrected (ADR-003, UC-ACD-15).
/// </summary>
/// <param name="StandingId">The unique identifier of the corrected academic standing record.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="AcademicPeriodId">The unique identifier of the academic period.</param>
/// <param name="OldStanding">The previous academic standing status.</param>
/// <param name="NewStanding">The corrected new academic standing status.</param>
/// <param name="Reason">The mandatory correction justification narrative.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record AcademicStandingCorrectedEvent(
    Guid StandingId,
    Guid StudentId,
    Guid AcademicPeriodId,
    string OldStanding,
    string NewStanding,
    string Reason,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcademicStandingCorrectedEvent"/> record with current UTC timestamp.
    /// </summary>
    /// <param name="standingId">The unique identifier of the corrected academic standing record.</param>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="academicPeriodId">The unique identifier of the academic period.</param>
    /// <param name="oldStanding">The previous academic standing status.</param>
    /// <param name="newStanding">The corrected new academic standing status.</param>
    /// <param name="reason">The mandatory correction justification narrative.</param>
    public AcademicStandingCorrectedEvent(
        Guid standingId,
        Guid studentId,
        Guid academicPeriodId,
        string oldStanding,
        string newStanding,
        string reason)
        : this(standingId, studentId, academicPeriodId, oldStanding, newStanding, reason, DateTimeOffset.UtcNow)
    {
    }
}
