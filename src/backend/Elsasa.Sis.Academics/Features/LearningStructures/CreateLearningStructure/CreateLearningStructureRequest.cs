namespace Elsasa.Sis.Academics.Features.LearningStructures.CreateLearningStructure;

/// <summary>
/// Request payload for drafting a new learning structure framework (ADR-004, ADR-005, P3-S06).
/// </summary>
public sealed record CreateLearningStructureRequest(
    string Name,
    string? Description,
    Guid AcademicPeriodId,
    string Version,
    DateOnly EffectiveDate,
    DateOnly? EndDate = null,
    string? Notes = null);
