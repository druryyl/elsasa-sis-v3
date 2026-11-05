namespace Elsasa.Sis.Academics.Features.LearningUnits.CreateLearningUnit;

/// <summary>
/// Payload for creating a learning unit within a learning structure framework (P3-S08).
/// </summary>
public sealed record CreateLearningUnitRequest(
    string Code,
    string Name,
    string UnitType,
    Guid? ParentUnitId = null,
    string? Level = null,
    string? Description = null,
    decimal? CreditWeight = null,
    string? Notes = null);
