namespace Elsasa.Sis.Academics.Features.AcademicRequirements.UpdateAcademicRequirement;

/// <summary>
/// Response payload returned after updating an academic requirement (ADR-004, ADR-005, P3-S09).
/// </summary>
public sealed record UpdateAcademicRequirementResponse(
    Guid Id,
    string Name,
    string RequirementScope,
    Guid LearningStructureId,
    Guid? LearningUnitId,
    string? Level,
    string RequirementType,
    string Description,
    string Version,
    DateOnly EffectiveDate,
    DateOnly? EndDate,
    string Status,
    string? Notes,
    DateTimeOffset UpdatedAt,
    string UpdatedBy);
