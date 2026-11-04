namespace Elsasa.Sis.Academics.Features.LearningStructures.CreateLearningStructure;

/// <summary>
/// Response payload returned upon successful creation of a draft learning structure (ADR-004, ADR-005, P3-S06).
/// </summary>
public sealed record CreateLearningStructureResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid AcademicPeriodId,
    string AcademicPeriodLabel,
    string Version,
    DateOnly EffectiveDate,
    DateOnly? EndDate,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy);
