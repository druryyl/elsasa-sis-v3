namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Strongly-typed contract DTO representing an academic requirement or graduation rule.
/// </summary>
/// <param name="Id">The unique identifier of the academic requirement.</param>
/// <param name="Name">The title or name of the academic requirement.</param>
/// <param name="RequirementScope">The scope of application ('program', 'level', or 'learning_unit').</param>
/// <param name="LearningStructureId">The unique identifier of the owning learning structure.</param>
/// <param name="LearningUnitId">The linked learning unit identifier if scope is 'learning_unit', otherwise null.</param>
/// <param name="Level">The target level string if scope is 'level', otherwise null.</param>
/// <param name="RequirementType">The requirement policy category ('completion', 'prerequisite', 'credit', 'progression', 'graduation', 'assessment_policy').</param>
/// <param name="Description">The plain text specification of the criteria.</param>
/// <param name="Version">The semantic version identifier.</param>
/// <param name="EffectiveDate">The date when this requirement became effective.</param>
/// <param name="EndDate">The optional expiration or sunset date of this requirement.</param>
/// <param name="Status">The lifecycle status (e.g., draft, active, superseded, archived).</param>
public sealed record AcademicRequirementDto(
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
    string Status);
