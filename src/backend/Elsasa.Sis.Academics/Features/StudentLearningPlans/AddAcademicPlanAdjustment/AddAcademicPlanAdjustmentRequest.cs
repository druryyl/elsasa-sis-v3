namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.AddAcademicPlanAdjustment;

/// <summary>
/// Request payload for adding an academic plan adjustment to a draft student learning plan (UC-ACD-09, P3-S07, TD-03).
/// </summary>
/// <param name="AdjustmentType">The adjustment category ('accommodation', 'substitution', 'exemption').</param>
/// <param name="Description">Detailed specification of the adjustment.</param>
/// <param name="Rationale">Pedagogical, developmental, or medical justification for the adjustment (min 10 chars).</param>
/// <param name="EffectiveDate">The date when this adjustment becomes effective.</param>
/// <param name="EndDate">Optional conclusion date when this adjustment ceases to apply.</param>
/// <param name="Notes">Optional contextual or administrative notes.</param>
public sealed record AddAcademicPlanAdjustmentRequest(
    string AdjustmentType,
    string Description,
    string Rationale,
    DateOnly EffectiveDate,
    DateOnly? EndDate = null,
    string? Notes = null);
