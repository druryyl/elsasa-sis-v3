namespace Elsasa.Sis.Academics.Features.LearningStructures.GetLearningStructureById;

/// <summary>
/// Detailed response record representing a learning structure framework with period details,
/// version lineage, approval record, and predecessor link (ADR-004, ADR-005, P3-S06).
/// </summary>
public sealed record LearningStructureDetailResponse(
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
    string? PredecessorName,
    string? PredecessorVersion,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy,
    int UnitCount,
    int RequirementCount,
    bool HasPredecessor);
