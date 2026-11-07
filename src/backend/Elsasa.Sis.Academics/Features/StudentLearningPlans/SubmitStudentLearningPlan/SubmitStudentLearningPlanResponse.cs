namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.SubmitStudentLearningPlan;

/// <summary>
/// Response payload returned upon successful submission of a student learning plan for review (UC-ACD-09, P3-S08).
/// </summary>
/// <param name="Id">The unique identifier of the submitted student learning plan.</param>
/// <param name="Status">The lifecycle status of the plan ('draft').</param>
/// <param name="Version">The linear version of the plan.</param>
/// <param name="SubmittedAt">UTC timestamp when the plan was marked submitted for review.</param>
/// <param name="SubmittedBy">Username of the actor who submitted the plan.</param>
/// <param name="Notes">Plan notes following submission.</param>
public sealed record SubmitStudentLearningPlanResponse(
    Guid Id,
    string Status,
    int Version,
    DateTimeOffset SubmittedAt,
    string SubmittedBy,
    string? Notes);
