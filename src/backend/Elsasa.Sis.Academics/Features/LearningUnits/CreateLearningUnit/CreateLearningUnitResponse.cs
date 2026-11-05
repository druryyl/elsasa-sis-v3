namespace Elsasa.Sis.Academics.Features.LearningUnits.CreateLearningUnit;

/// <summary>
/// Response payload for learning unit creation (P3-S08).
/// </summary>
public sealed record CreateLearningUnitResponse(
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
    DateTimeOffset CreatedAt,
    string CreatedBy);
