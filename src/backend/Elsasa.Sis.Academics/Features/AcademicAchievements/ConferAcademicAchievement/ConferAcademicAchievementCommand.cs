namespace Elsasa.Sis.Academics.Features.AcademicAchievements.ConferAcademicAchievement;

/// <summary>
/// Command to formally confer an academic achievement, honor, or distinction (UC-ACD-21, P4-S11).
/// </summary>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="LearningUnitId">The optional related learning unit identifier.</param>
/// <param name="AcademicPeriodId">The academic period identifier.</param>
/// <param name="AchievementType">The category of achievement (e.g., 'subject_distinction', 'academic_honors').</param>
/// <param name="Title">The formal title of the achievement.</param>
/// <param name="Description">Optional description of the achievement.</param>
/// <param name="CriteriaMet">The criteria or qualification met.</param>
/// <param name="SupportingEvidence">Optional supporting academic evidence reference.</param>
/// <param name="RecognizedAt">The date when formally recognized.</param>
public sealed record ConferAcademicAchievementCommand(
    Guid StudentId,
    Guid? LearningUnitId,
    Guid AcademicPeriodId,
    string AchievementType,
    string Title,
    string? Description,
    string CriteriaMet,
    string? SupportingEvidence,
    DateOnly RecognizedAt);
