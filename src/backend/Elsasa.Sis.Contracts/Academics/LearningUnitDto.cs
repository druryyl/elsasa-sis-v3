namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Strongly-typed contract DTO representing a learning unit within a learning structure.
/// </summary>
/// <param name="Id">The unique identifier of the learning unit.</param>
/// <param name="LearningStructureId">The unique identifier of the owning learning structure.</param>
/// <param name="ParentUnitId">The unique identifier of the parent unit in a 2-level hierarchy, or null for root units.</param>
/// <param name="Code">The institutional code of the learning unit (unique within structure).</param>
/// <param name="Name">The display name of the learning unit.</param>
/// <param name="UnitType">The category/type of unit (e.g., subject, course, module, pace, project, competency, unit, other).</param>
/// <param name="Level">The target educational level, or null if unassigned.</param>
/// <param name="CreditWeight">The credit or weight value, or null if unweighted.</param>
/// <param name="Status">The lifecycle status (e.g., draft, active, archived).</param>
public sealed record LearningUnitDto(
    Guid Id,
    Guid LearningStructureId,
    Guid? ParentUnitId,
    string Code,
    string Name,
    string UnitType,
    string? Level,
    decimal? CreditWeight,
    string Status);
