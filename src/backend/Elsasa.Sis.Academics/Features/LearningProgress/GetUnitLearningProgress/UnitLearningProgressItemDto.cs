namespace Elsasa.Sis.Academics.Features.LearningProgress.GetUnitLearningProgress;

/// <summary>
/// Projection DTO representing a student's learning progress and participation state in a learning unit roster (P2-S05, UC-ACD-12, SCR-ACD-07).
/// </summary>
/// <param name="StudentId">The unique identifier of the participating student.</param>
/// <param name="StudentNumber">The student registration number, if available.</param>
/// <param name="StudentName">The full name of the participating student.</param>
/// <param name="LearningUnitId">The unique identifier of the learning unit.</param>
/// <param name="AcademicPeriodId">The academic period identifier.</param>
/// <param name="ParticipationId">The unique identifier of the participation record, if present.</param>
/// <param name="ParticipationStatus">The participation lifecycle status (in_progress, completed, paused, withdrawn, enrolled).</param>
/// <param name="SectionLabel">The cohort section label (e.g. '4A'), if assigned.</param>
/// <param name="ProgressId">The unique identifier of the learning progress record, if recorded.</param>
/// <param name="ProgressStatus">The categorical progress status (not_started, in_progress, completed, needs_remediation, exempted).</param>
/// <param name="Basis">The operational basis for progress (assessment_evidence, learning_work, authorized_decision).</param>
/// <param name="OperativeGrade">The operative grade achieved in the unit (letter, competency label, or formatted score), if available.</param>
/// <param name="OperativeScore">The numeric score achieved, if applicable.</param>
/// <param name="GradeLetter">The letter grade achieved, if applicable.</param>
/// <param name="GradeLabel">The competency or mastery label, if applicable.</param>
/// <param name="CompletionDate">The date on which unit progress or participation was formally completed.</param>
/// <param name="Notes">Teacher notes or administrative remarks.</param>
/// <param name="UpdatedAt">Timestamp of last progress update, if any.</param>
public sealed record UnitLearningProgressItemDto(
    Guid StudentId,
    string? StudentNumber,
    string? StudentName,
    Guid LearningUnitId,
    Guid AcademicPeriodId,
    Guid? ParticipationId,
    string ParticipationStatus,
    string? SectionLabel,
    Guid? ProgressId,
    string ProgressStatus,
    string? Basis,
    string? OperativeGrade,
    decimal? OperativeScore,
    string? GradeLetter,
    string? GradeLabel,
    DateOnly? CompletionDate,
    string? Notes,
    DateTimeOffset? UpdatedAt);
