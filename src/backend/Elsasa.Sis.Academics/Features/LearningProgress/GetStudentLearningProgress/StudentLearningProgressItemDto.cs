namespace Elsasa.Sis.Academics.Features.LearningProgress.GetStudentLearningProgress;

/// <summary>
/// Projection DTO representing student learning progress for an enrolled unit within an academic period (P2-S05, UC-ACD-12, SCR-ACD-07).
/// </summary>
/// <param name="ProgressId">The unique identifier of the learning progress record, if recorded.</param>
/// <param name="ParticipationId">The unique identifier of the participation record, if enrolled.</param>
/// <param name="StudentId">The unique identifier of the target student.</param>
/// <param name="LearningUnitId">The unique identifier of the learning unit.</param>
/// <param name="LearningUnitCode">The curricular code of the learning unit.</param>
/// <param name="LearningUnitName">The display title or name of the learning unit.</param>
/// <param name="CreditWeight">The credit weight of the learning unit.</param>
/// <param name="AcademicPeriodId">The academic period identifier.</param>
/// <param name="SectionLabel">The cohort section label (e.g. '4A'), if assigned.</param>
/// <param name="ParticipationStatus">The participation lifecycle status (e.g. 'in_progress', 'completed').</param>
/// <param name="ProgressStatus">The categorical learning progress status (not_started, in_progress, completed, needs_remediation, exempted).</param>
/// <param name="Basis">The operational basis for the progress status (assessment_evidence, learning_work, authorized_decision).</param>
/// <param name="CompletionDate">The explicit completion date, supporting out-of-period stamping.</param>
/// <param name="Notes">Teacher notes or administrative remarks.</param>
/// <param name="CreatedAt">Timestamp when progress was created.</param>
/// <param name="CreatedBy">Username who created the progress record.</param>
/// <param name="UpdatedAt">Timestamp of last update, if any.</param>
/// <param name="UpdatedBy">Username who last updated the record, if any.</param>
public sealed record StudentLearningProgressItemDto(
    Guid? ProgressId,
    Guid? ParticipationId,
    Guid StudentId,
    Guid LearningUnitId,
    string LearningUnitCode,
    string LearningUnitName,
    decimal CreditWeight,
    Guid AcademicPeriodId,
    string? SectionLabel,
    string ParticipationStatus,
    string ProgressStatus,
    string? Basis,
    DateOnly? CompletionDate,
    string? Notes,
    DateTimeOffset? CreatedAt,
    string? CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
