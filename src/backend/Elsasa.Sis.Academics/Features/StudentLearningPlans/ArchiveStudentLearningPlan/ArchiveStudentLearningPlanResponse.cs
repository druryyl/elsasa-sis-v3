namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.ArchiveStudentLearningPlan;

/// <summary>
/// Response payload returned upon successful archival or completion of a student learning plan.
/// </summary>
/// <param name="Id">The unique identifier of the student learning plan.</param>
/// <param name="Status">The resulting lifecycle status ('archived' or 'completed').</param>
/// <param name="Version">The linear plan version.</param>
/// <param name="UpdatedAt">The UTC timestamp when the plan status was updated.</param>
/// <param name="UpdatedBy">Username of the actor who updated the plan.</param>
public sealed record ArchiveStudentLearningPlanResponse(
    Guid Id,
    string Status,
    int Version,
    DateTimeOffset UpdatedAt,
    string UpdatedBy);
