namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.AddSlpLearningUnit;

/// <summary>
/// Request payload for linking a constituent learning unit to a student learning plan (UC-ACD-09, P2-S05).
/// </summary>
/// <param name="LearningUnitId">The unique identifier of the referenced learning unit.</param>
/// <param name="SequenceOrder">Optional sequence order of the unit within the learning plan.</param>
/// <param name="IsRequired">Whether the learning unit is required in this individual plan. Defaults to true.</param>
/// <param name="Notes">Optional adaptation, pacing, or level notes.</param>
public sealed record AddSlpLearningUnitRequest(
    Guid LearningUnitId,
    int? SequenceOrder = null,
    bool IsRequired = true,
    string? Notes = null);
