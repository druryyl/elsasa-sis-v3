namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.CreateStudentLearningPlan;

/// <summary>
/// Response payload returned upon successful formulation of a student learning plan.
/// </summary>
/// <param name="Id">The unique identifier of the newly created student learning plan.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="AcademicPeriodId">The unique identifier of the academic period.</param>
/// <param name="AcademicPeriodLabel">The display label of the associated academic period.</param>
/// <param name="Status">The initial lifecycle status ('draft').</param>
/// <param name="Version">The initial linear plan version (1).</param>
/// <param name="Notes">Optional coordinator or overarching plan notes.</param>
/// <param name="CreatedAt">The UTC timestamp when the plan was formulated.</param>
/// <param name="CreatedBy">Username of the actor who formulated the plan.</param>
public sealed record CreateStudentLearningPlanResponse(
    Guid Id,
    Guid StudentId,
    Guid AcademicPeriodId,
    string AcademicPeriodLabel,
    string Status,
    int Version,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy);
