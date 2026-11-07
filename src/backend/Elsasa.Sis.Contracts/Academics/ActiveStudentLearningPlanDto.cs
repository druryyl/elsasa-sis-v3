namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Strongly-typed contract DTO representing an active student learning plan and its active constituents.
/// </summary>
/// <param name="PlanId">The unique identifier of the active student learning plan.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="AcademicPeriodId">The unique identifier of the associated academic period.</param>
/// <param name="AcademicPeriodLabel">The display label of the associated academic period.</param>
/// <param name="Version">The linear plan version identifier.</param>
/// <param name="Status">The lifecycle status (e.g., 'active').</param>
/// <param name="Notes">Optional coordinator or overarching plan notes.</param>
/// <param name="ConstituentUnits">The read-only collection of constituent learning units linked to the plan.</param>
/// <param name="Adjustments">The read-only collection of active academic adjustments (accommodations, substitutions, exemptions).</param>
public sealed record ActiveStudentLearningPlanDto(
    Guid PlanId,
    Guid StudentId,
    Guid AcademicPeriodId,
    string AcademicPeriodLabel,
    int Version,
    string Status,
    string? Notes,
    IReadOnlyList<SlpLearningUnitDto> ConstituentUnits,
    IReadOnlyList<AcademicPlanAdjustmentDto> Adjustments);
