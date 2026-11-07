namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Strongly-typed contract DTO representing an academic achievement, honor, or distinction conferred on a student (ADR-001, UC-ACD-21).
/// </summary>
/// <param name="Id">The unique identifier of the academic achievement record.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="LearningUnitId">The unique identifier of the related learning unit, or null if institutional/program-wide.</param>
/// <param name="AcademicPeriodId">The unique identifier of the academic period in which the achievement was conferred.</param>
/// <param name="AchievementType">The category of achievement (e.g., 'subject_distinction', 'academic_honors', 'mastery_recognition', 'academic_certification', 'academic_competition').</param>
/// <param name="Title">The formal title of the achievement or distinction.</param>
/// <param name="Description">Descriptive narrative or details of the recognition, if provided.</param>
/// <param name="CriteriaMet">Summary of the academic criteria or thresholds satisfied.</param>
/// <param name="SupportingEvidence">Reference to academic results or portfolio evidence supporting the honor.</param>
/// <param name="RecognizedBy">Username or identifier of the certifying Academic Authority.</param>
/// <param name="RecognizedAt">The date when the achievement was formally recognized.</param>
public sealed record AcademicAchievementDto(
    Guid Id,
    Guid StudentId,
    Guid? LearningUnitId,
    Guid AcademicPeriodId,
    string AchievementType,
    string Title,
    string? Description,
    string CriteriaMet,
    string? SupportingEvidence,
    string RecognizedBy,
    DateOnly RecognizedAt)
{
    /// <summary>
    /// Gets the unique identifier of the academic achievement (alias for <see cref="Id"/>).
    /// </summary>
    public Guid AchievementId => Id;
}
