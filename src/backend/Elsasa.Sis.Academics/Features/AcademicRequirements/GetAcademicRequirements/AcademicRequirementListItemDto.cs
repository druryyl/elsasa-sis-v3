namespace Elsasa.Sis.Academics.Features.AcademicRequirements.GetAcademicRequirements;

/// <summary>
/// DTO representing an academic requirement item in list queries (ADR-004, ADR-005, P3-S09).
/// </summary>
public sealed record AcademicRequirementListItemDto(
    Guid Id,
    string Name,
    string RequirementScope,
    Guid LearningStructureId,
    Guid? LearningUnitId,
    string? LearningUnitCode,
    string? LearningUnitName,
    string? Level,
    string RequirementType,
    string Description,
    string Version,
    DateOnly EffectiveDate,
    DateOnly? EndDate,
    string Status,
    string? ApprovedBy,
    DateOnly? ApprovedDate,
    Guid? SupersedesId,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
