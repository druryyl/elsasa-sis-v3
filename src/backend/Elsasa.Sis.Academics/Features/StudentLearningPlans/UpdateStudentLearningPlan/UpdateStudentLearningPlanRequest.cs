namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.UpdateStudentLearningPlan;

/// <summary>
/// Request payload for updating draft student learning plan notes and metadata (P2-S04).
/// </summary>
/// <param name="Notes">Updated coordinator or overarching plan notes.</param>
public sealed record UpdateStudentLearningPlanRequest(
    string? Notes = null);
