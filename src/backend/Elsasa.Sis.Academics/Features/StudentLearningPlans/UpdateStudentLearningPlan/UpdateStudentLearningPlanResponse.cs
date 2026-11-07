namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.UpdateStudentLearningPlan;

/// <summary>
/// Response payload returned upon successful update of a student learning plan.
/// </summary>
/// <param name="Id">The unique identifier of the student learning plan.</param>
/// <param name="Status">The lifecycle status of the plan.</param>
/// <param name="Version">The linear version identifier of the plan.</param>
/// <param name="Notes">The updated notes.</param>
/// <param name="UpdatedAt">The UTC timestamp when the plan was updated.</param>
/// <param name="UpdatedBy">Username of the actor who updated the plan.</param>
public sealed record UpdateStudentLearningPlanResponse(
    Guid Id,
    string Status,
    int Version,
    string? Notes,
    DateTimeOffset UpdatedAt,
    string UpdatedBy);
