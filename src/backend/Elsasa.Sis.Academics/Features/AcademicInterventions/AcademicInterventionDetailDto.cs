namespace Elsasa.Sis.Academics.Features.AcademicInterventions;

/// <summary>
/// Detailed data transfer object for an academic intervention including student and learning unit metadata (UC-ACD-11, P4-S10, TD-04).
/// </summary>
/// <param name="Id">The unique identifier of the intervention.</param>
/// <param name="StudentId">The unique identifier of the target student.</param>
/// <param name="StudentNumber">The administrative student identity number.</param>
/// <param name="StudentName">The full name of the student.</param>
/// <param name="StudentEmail">The official email address of the student.</param>
/// <param name="GradeLevel">The cohort grade level or unit level.</param>
/// <param name="LearningUnitId">The unique identifier of the target learning unit.</param>
/// <param name="LearningUnitCode">The curriculum unit code.</param>
/// <param name="LearningUnitName">The curriculum unit display name.</param>
/// <param name="AcademicPeriodId">The operational academic period identifier.</param>
/// <param name="InterventionType">The category of intervention (remediation, reassessment, tutoring_recommendation, extension_work, recovery_plan, learning_plan_adjustment, academic_warning).</param>
/// <param name="TriggerType">The dual-trigger source type (assessment_below_threshold, missing_work, attendance_concern, standing_concern, teacher_referral).</param>
/// <param name="TriggerDescription">The narrative or metric summary describing the trigger condition.</param>
/// <param name="PlanDescription">The structured pedagogical intervention action plan.</param>
/// <param name="Status">The lifecycle progress status (planned, in_progress, completed, cancelled).</param>
/// <param name="InitiatedBy">The username or display name of the initiator.</param>
/// <param name="InitiatedAt">The UTC timestamp when the intervention was created.</param>
/// <param name="ConcludedAt">The UTC timestamp when the intervention reached a terminal state, if concluded.</param>
/// <param name="OutcomeNotes">The outcome narrative recorded upon conclusion, if concluded.</param>
/// <param name="CreatedAt">The UTC creation timestamp.</param>
/// <param name="CreatedBy">The user who created the record.</param>
/// <param name="UpdatedAt">The UTC modification timestamp, if updated.</param>
/// <param name="UpdatedBy">The user who updated the record, if updated.</param>
public sealed record AcademicInterventionDetailDto(
    Guid Id,
    Guid StudentId,
    string? StudentNumber,
    string? StudentName,
    string? StudentEmail,
    string? GradeLevel,
    Guid LearningUnitId,
    string? LearningUnitCode,
    string? LearningUnitName,
    Guid AcademicPeriodId,
    string InterventionType,
    string TriggerType,
    string TriggerDescription,
    string PlanDescription,
    string Status,
    string InitiatedBy,
    DateTimeOffset InitiatedAt,
    DateTimeOffset? ConcludedAt,
    string? OutcomeNotes,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
