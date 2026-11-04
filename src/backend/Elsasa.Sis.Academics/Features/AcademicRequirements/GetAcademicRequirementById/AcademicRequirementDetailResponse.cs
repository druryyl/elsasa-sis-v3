namespace Elsasa.Sis.Academics.Features.AcademicRequirements.GetAcademicRequirementById;

/// <summary>
/// Detailed response payload for inspecting an individual academic requirement (ADR-004, ADR-005, P3-S09).
/// </summary>
public sealed record AcademicRequirementDetailResponse(
    Guid Id,
    string Name,
    string RequirementScope,
    Guid LearningStructureId,
    string? LearningStructureName,
    string? LearningStructureVersion,
    string? LearningStructureStatus,
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
