namespace Elsasa.Sis.Academics.Features.LearningUnits.GetLearningUnits;

/// <summary>
/// DTO representing a learning unit in the flat query result for tree projection (P3-S08).
/// </summary>
public sealed record LearningUnitItemDto(
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
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
