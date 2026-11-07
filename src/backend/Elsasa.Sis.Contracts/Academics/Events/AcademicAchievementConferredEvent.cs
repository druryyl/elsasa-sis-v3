namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when an academic achievement or honor is formally conferred (ADR-003, UC-ACD-21).
/// </summary>
/// <param name="AchievementId">The unique identifier of the academic achievement record.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="AcademicPeriodId">The unique identifier of the academic period.</param>
/// <param name="AchievementType">The category of achievement (e.g., 'subject_distinction', 'academic_honors').</param>
/// <param name="Title">The formal title of the achievement.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record AcademicAchievementConferredEvent(
    Guid AchievementId,
    Guid StudentId,
    Guid AcademicPeriodId,
    string AchievementType,
    string Title,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcademicAchievementConferredEvent"/> record with current UTC timestamp.
    /// </summary>
    /// <param name="achievementId">The unique identifier of the academic achievement record.</param>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="academicPeriodId">The unique identifier of the academic period.</param>
    /// <param name="achievementType">The category of achievement.</param>
    /// <param name="title">The formal title of the achievement.</param>
    public AcademicAchievementConferredEvent(
        Guid achievementId,
        Guid studentId,
        Guid academicPeriodId,
        string achievementType,
        string title)
        : this(achievementId, studentId, academicPeriodId, achievementType, title, DateTimeOffset.UtcNow)
    {
    }
}
