namespace Elsasa.Sis.Academics.Features.AcademicRequirements.CreateAcademicRequirement;

/// <summary>
/// Response payload returned after creating an academic requirement (ADR-004, ADR-005, P3-S09).
/// </summary>
public sealed record CreateAcademicRequirementResponse(
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
    DateTimeOffset CreatedAt,
    string CreatedBy);
