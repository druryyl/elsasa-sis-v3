namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.AddAcademicPlanAdjustment;

/// <summary>
/// Response payload returned upon successful creation of an academic plan adjustment (UC-ACD-09, P3-S07).
/// </summary>
/// <param name="Id">The unique identifier of the created academic plan adjustment.</param>
/// <param name="StudentLearningPlanId">The parent student learning plan identifier.</param>
/// <param name="AdjustmentType">The adjustment category ('accommodation', 'substitution', 'exemption').</param>
/// <param name="Description">Detailed specification of the adjustment.</param>
/// <param name="Rationale">Pedagogical, developmental, or medical justification.</param>
/// <param name="EffectiveDate">The date when this adjustment becomes effective.</param>
/// <param name="EndDate">Optional conclusion date when this adjustment ceases to apply.</param>
/// <param name="ApprovedBy">Username of the approving academic authority (empty until plan approval).</param>
/// <param name="ApprovedDate">The date when this adjustment was formally approved (1900-01-01 until plan approval).</param>
/// <param name="Notes">Optional contextual or administrative notes.</param>
/// <param name="CreatedAt">Timestamp when this adjustment was created.</param>
/// <param name="CreatedBy">Username who created this adjustment.</param>
public sealed record AddAcademicPlanAdjustmentResponse(
    Guid Id,
    Guid StudentLearningPlanId,
    string AdjustmentType,
    string Description,
    string Rationale,
    DateOnly EffectiveDate,
    DateOnly? EndDate,
    string ApprovedBy,
    DateOnly ApprovedDate,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy);
