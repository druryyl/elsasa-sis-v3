namespace Elsasa.Sis.Academics.Features.LearningUnits.GetLearningUnitById;

/// <summary>
/// Detailed response payload for a single learning unit including parent unit info (P3-S08).
/// </summary>
public sealed record LearningUnitDetailResponse(
    Guid Id,
    Guid LearningStructureId,
    Guid? ParentUnitId,
    string? ParentCode,
    string? ParentName,
    string? ParentUnitType,
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
