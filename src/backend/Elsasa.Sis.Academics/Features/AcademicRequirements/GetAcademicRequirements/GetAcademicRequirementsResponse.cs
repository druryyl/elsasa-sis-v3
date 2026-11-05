namespace Elsasa.Sis.Academics.Features.AcademicRequirements.GetAcademicRequirements;

/// <summary>
/// Response payload for querying academic requirements for a learning structure (ADR-004, ADR-005, P3-S09).
/// </summary>
public sealed record GetAcademicRequirementsResponse(
    Guid LearningStructureId,
    IReadOnlyList<AcademicRequirementListItemDto> Requirements);
