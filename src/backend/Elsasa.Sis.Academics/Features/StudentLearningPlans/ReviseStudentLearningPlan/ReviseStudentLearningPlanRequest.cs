namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.ReviseStudentLearningPlan;

/// <summary>
/// Request payload for creating a new draft revision from an active student learning plan (UC-ACD-09, P3-S09, TD-02).
/// </summary>
/// <param name="Notes">Rationale or context for initiating the plan revision.</param>
public sealed record ReviseStudentLearningPlanRequest(string? Notes);
