namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.ReviseStudentLearningPlan;

/// <summary>
/// Response payload returned upon successfully cloning an active plan into a new draft revision (UC-ACD-09, P3-S09, TD-02).
/// </summary>
/// <param name="Id">The unique identifier of the newly cloned draft plan revision.</param>
/// <param name="PriorPlanId">The unique identifier of the original active plan being revised.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="AcademicPeriodId">The unique identifier of the associated academic period.</param>
/// <param name="AcademicPeriodLabel">The display label of the associated academic period.</param>
/// <param name="Status">The lifecycle status of the new plan ('draft').</param>
/// <param name="Version">The new plan version (PriorPlan.Version + 1).</param>
/// <param name="Notes">Rationale or context recorded for this revision.</param>
/// <param name="CreatedAt">The UTC timestamp when the revision was created.</param>
/// <param name="CreatedBy">Username of the actor who initiated the revision.</param>
public sealed record ReviseStudentLearningPlanResponse(
    Guid Id,
    Guid PriorPlanId,
    Guid StudentId,
    Guid AcademicPeriodId,
    string AcademicPeriodLabel,
    string Status,
    int Version,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy);
