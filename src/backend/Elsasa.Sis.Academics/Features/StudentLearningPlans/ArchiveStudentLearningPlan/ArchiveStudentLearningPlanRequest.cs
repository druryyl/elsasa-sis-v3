namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.ArchiveStudentLearningPlan;

/// <summary>
/// Request payload for archiving or completing a student learning plan (UC-ACD-09, P2-S04).
/// </summary>
/// <param name="TargetStatus">The target status: 'archived' (default) or 'completed'.</param>
/// <param name="Notes">Optional archival or completion notes.</param>
public sealed record ArchiveStudentLearningPlanRequest(
    string? TargetStatus = "archived",
    string? Notes = null);
