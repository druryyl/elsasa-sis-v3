namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.ApproveStudentLearningPlan;

/// <summary>
/// Request payload for Academic Authority approval of a student learning plan (UC-ACD-09, P3-S08).
/// </summary>
/// <param name="ApprovalType">The approval decision category ('initial_approval' or 'revision_approval').</param>
/// <param name="Notes">Optional authority governance remarks or rationale.</param>
public sealed record ApproveStudentLearningPlanRequest(
    string ApprovalType,
    string? Notes = null);
