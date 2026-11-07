namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.CreateStudentLearningPlan;

/// <summary>
/// Request payload for formulating a new student learning plan (v1 draft).
/// </summary>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="AcademicPeriodId">The unique identifier of the academic period.</param>
/// <param name="AcademicPeriodLabel">The display label of the associated academic period.</param>
/// <param name="Notes">Optional coordinator or overarching plan notes.</param>
public sealed record CreateStudentLearningPlanRequest(
    Guid StudentId,
    Guid AcademicPeriodId,
    string AcademicPeriodLabel,
    string? Notes = null);
