namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Strongly-typed contract DTO representing an academic participation record.
/// </summary>
/// <param name="Id">The unique identifier of the academic participation.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="LearningUnitId">The unique identifier of the learning unit.</param>
/// <param name="AcademicPeriodId">The unique identifier of the academic period.</param>
/// <param name="SectionLabel">The cohort section label (e.g., "4A"), or null if unsectioned.</param>
/// <param name="ParticipationType">The type of participation (standard, repeat, transferred_in, recognized_prior).</param>
/// <param name="AttemptNumber">The attempt count for retakes/repeats (starts at 1).</param>
/// <param name="Status">The lifecycle status of the participation (in_progress, paused, withdrawn, completed).</param>
/// <param name="StartDate">The start date of the participation.</param>
/// <param name="EndDate">The end date of the participation, if concluded or withdrawn.</param>
public sealed record AcademicParticipationDto(
    Guid Id,
    Guid StudentId,
    Guid LearningUnitId,
    Guid AcademicPeriodId,
    string? SectionLabel,
    string ParticipationType,
    int AttemptNumber,
    string Status,
    DateOnly StartDate,
    DateOnly? EndDate);
