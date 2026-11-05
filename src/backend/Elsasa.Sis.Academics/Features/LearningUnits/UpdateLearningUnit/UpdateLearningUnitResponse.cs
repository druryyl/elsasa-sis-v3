namespace Elsasa.Sis.Academics.Features.LearningUnits.UpdateLearningUnit;

/// <summary>
/// Response payload after updating a learning unit (P3-S08).
/// </summary>
public sealed record UpdateLearningUnitResponse(
    Guid Id,
    Guid LearningStructureId,
    Guid? ParentUnitId,
    string Code,
    string Name,
    string UnitType,
    string? Level,
    string? Description,
    decimal? CreditWeight,
    string Status,
    string? Notes,
    DateTimeOffset UpdatedAt,
    string UpdatedBy);
