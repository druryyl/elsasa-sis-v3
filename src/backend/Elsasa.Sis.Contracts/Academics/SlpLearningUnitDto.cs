namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Strongly-typed contract DTO representing a constituent learning unit within a student learning plan.
/// </summary>
/// <param name="Id">The unique identifier of the constituent learning unit entry.</param>
/// <param name="LearningUnitId">The unique identifier of the referenced learning unit.</param>
/// <param name="UnitCode">The institutional code of the learning unit.</param>
/// <param name="Title">The display title or name of the learning unit.</param>
/// <param name="SequenceOrder">Optional sequence order of the unit within the learning plan.</param>
/// <param name="IsRequired">Whether the learning unit is required in this individual plan.</param>
/// <param name="Notes">Optional adaptation, pacing, or level notes.</param>
public sealed record SlpLearningUnitDto(
    Guid Id,
    Guid LearningUnitId,
    string UnitCode,
    string Title,
    int? SequenceOrder,
    bool IsRequired,
    string? Notes);
