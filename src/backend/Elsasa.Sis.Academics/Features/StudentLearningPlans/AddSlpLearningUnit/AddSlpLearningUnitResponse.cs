namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.AddSlpLearningUnit;

/// <summary>
/// Response payload for a newly added constituent learning unit entry on a student learning plan (UC-ACD-09, P2-S05).
/// </summary>
/// <param name="Id">The unique identifier of the slp_learning_units entry.</param>
/// <param name="StudentLearningPlanId">The student learning plan ID.</param>
/// <param name="LearningUnitId">The referenced learning unit ID.</param>
/// <param name="SequenceOrder">Sequence order of the unit within the plan.</param>
/// <param name="IsRequired">Whether the unit is required.</param>
/// <param name="Notes">Adaptation, pacing, or level notes.</param>
/// <param name="CreatedAt">Timestamp when the constituent unit was added.</param>
/// <param name="CreatedBy">Username of the authenticated user who added the unit.</param>
public sealed record AddSlpLearningUnitResponse(
    Guid Id,
    Guid StudentLearningPlanId,
    Guid LearningUnitId,
    int? SequenceOrder,
    bool IsRequired,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy);
