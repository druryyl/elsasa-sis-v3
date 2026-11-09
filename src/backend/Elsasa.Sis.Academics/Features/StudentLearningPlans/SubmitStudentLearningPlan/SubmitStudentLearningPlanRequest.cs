namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.SubmitStudentLearningPlan;

/// <summary>
/// Request payload for submitting a draft student learning plan for Academic Authority review (UC-ACD-09, P3-S08).
/// </summary>
/// <param name="Notes">Optional submission remarks or coordinator notes.</param>
public sealed record SubmitStudentLearningPlanRequest(string? Notes = null);
