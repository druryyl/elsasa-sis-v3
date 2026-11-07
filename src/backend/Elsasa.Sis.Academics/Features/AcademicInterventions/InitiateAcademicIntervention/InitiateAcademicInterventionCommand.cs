namespace Elsasa.Sis.Academics.Features.AcademicInterventions.InitiateAcademicIntervention;

/// <summary>
/// Command record for initiating a structured academic support intervention plan (UC-ACD-11, P4-S10, TD-04).
/// </summary>
/// <param name="StudentId">The unique identifier of the target student.</param>
/// <param name="LearningUnitId">The unique identifier of the target learning unit.</param>
/// <param name="AcademicPeriodId">The operational academic period identifier.</param>
/// <param name="InterventionType">The intervention type ('remediation', 'reassessment', 'tutoring_recommendation', 'extension_work', 'recovery_plan', 'learning_plan_adjustment', 'academic_warning').</param>
/// <param name="TriggerType">The dual-trigger source type ('assessment_below_threshold', 'missing_work', 'attendance_concern', 'standing_concern', 'teacher_referral').</param>
/// <param name="TriggerDescription">The contextual summary or narrative of the trigger condition.</param>
/// <param name="PlanDescription">The detailed description of the educational support intervention plan.</param>
public sealed record InitiateAcademicInterventionCommand(
    Guid StudentId,
    Guid LearningUnitId,
    Guid AcademicPeriodId,
    string InterventionType,
    string TriggerType,
    string TriggerDescription,
    string PlanDescription);
