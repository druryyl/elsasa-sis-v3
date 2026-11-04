namespace Elsasa.Sis.Academics.Features.LearningUnits.UpdateLearningUnit;

/// <summary>
/// Payload for updating a learning unit (P3-S08).
/// </summary>
public sealed record UpdateLearningUnitRequest(
    string Code,
    string Name,
    string UnitType,
    Guid? ParentUnitId = null,
    string? Level = null,
    string? Description = null,
    decimal? CreditWeight = null,
    string? Notes = null);
