namespace Elsasa.Sis.Academics.Features.AcademicRequirements.UpdateAcademicRequirement;

/// <summary>
/// Request payload for updating an academic requirement (ADR-004, ADR-005, P3-S09).
/// When owning structure is active, updates are restricted to metadata (name, description, notes).
/// When owning structure is draft, full editing is permitted.
/// </summary>
/// <param name="Name">Title or name of the academic requirement.</param>
/// <param name="Description">Plain text criteria description.</param>
/// <param name="Notes">Optional notes or administrative remarks.</param>
/// <param name="RequirementScope">Optional updated scope ('program', 'level', 'learning_unit') for draft frameworks.</param>
/// <param name="LearningUnitId">Optional updated learning unit identifier if scope is 'learning_unit'.</param>
/// <param name="Level">Optional updated level if scope is 'level'.</param>
/// <param name="RequirementType">Optional updated requirement category.</param>
/// <param name="Version">Optional updated version identifier.</param>
/// <param name="EffectiveDate">Optional updated effective date.</param>
/// <param name="EndDate">Optional updated expiration date.</param>
public sealed record UpdateAcademicRequirementRequest(
    string Name,
    string Description,
    string? Notes = null,
    string? RequirementScope = null,
    Guid? LearningUnitId = null,
    string? Level = null,
    string? RequirementType = null,
    string? Version = null,
    DateOnly? EffectiveDate = null,
    DateOnly? EndDate = null);
