namespace Elsasa.Sis.Academics.Features.AcademicRequirements.CreateAcademicRequirement;

/// <summary>
/// Request payload for creating a new academic requirement under a learning structure (ADR-004, ADR-005, P3-S09).
/// </summary>
/// <param name="Name">Title or name of the academic requirement.</param>
/// <param name="RequirementScope">Scope of application: 'program', 'level', or 'learning_unit'.</param>
/// <param name="LearningUnitId">Linked learning unit identifier if scope is 'learning_unit', otherwise null.</param>
/// <param name="Level">Target level string if scope is 'level', otherwise null.</param>
/// <param name="RequirementType">Requirement category: 'completion', 'prerequisite', 'credit', 'progression', 'graduation', or 'assessment_policy'.</param>
/// <param name="Description">Plain text criteria description.</param>
/// <param name="Version">Semantic version identifier.</param>
/// <param name="EffectiveDate">Date when this requirement becomes effective.</param>
/// <param name="EndDate">Optional expiration date.</param>
/// <param name="Notes">Optional notes or administrative remarks.</param>
public sealed record CreateAcademicRequirementRequest(
    string Name,
    string RequirementScope,
    Guid? LearningUnitId,
    string? Level,
    string RequirementType,
    string Description,
    string Version,
    DateOnly EffectiveDate,
    DateOnly? EndDate = null,
    string? Notes = null);
