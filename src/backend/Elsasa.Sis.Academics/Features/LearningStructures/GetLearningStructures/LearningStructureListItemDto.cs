namespace Elsasa.Sis.Academics.Features.LearningStructures.GetLearningStructures;

/// <summary>
/// Data transfer object representing a learning structure framework summary item in list projections (ADR-004, P3-S06).
/// </summary>
public sealed record LearningStructureListItemDto(
    Guid Id,
    string Name,
    string? Description,
    Guid AcademicPeriodId,
    string AcademicPeriodLabel,
    string Version,
    DateOnly EffectiveDate,
    DateOnly? EndDate,
    string Status,
    string? ApprovedBy,
    DateOnly? ApprovedDate,
    Guid? SupersedesId,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy,
    int UnitCount,
    int RequirementCount,
    bool HasPredecessor);
