namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.ApproveStudentLearningPlan;

/// <summary>
/// Response returned upon formal Academic Authority approval of a student learning plan (UC-ACD-09, P3-S08).
/// </summary>
/// <param name="PlanId">The unique identifier of the approved student learning plan.</param>
/// <param name="ApprovalId">The unique identifier of the immutable approval audit record.</param>
/// <param name="Version">The approved plan version.</param>
/// <param name="Status">The new status ('active').</param>
/// <param name="ApprovalType">The approval decision category ('initial_approval' or 'revision_approval').</param>
/// <param name="ApprovedBy">Username of the approving academic authority.</param>
/// <param name="ApprovedDate">Date when approval was granted.</param>
/// <param name="Notes">Optional governance notes recorded with the approval.</param>
public sealed record ApproveStudentLearningPlanResponse(
    Guid PlanId,
    Guid ApprovalId,
    int Version,
    string Status,
    string ApprovalType,
    string ApprovedBy,
    DateOnly ApprovedDate,
    string? Notes);
