namespace Elsasa.Sis.Academics.Features.LearningStructures.UpdateLearningStructure;

/// <summary>
/// Response payload returned upon successful update of learning structure metadata (ADR-004, ADR-005, P3-S06).
/// </summary>
public sealed record UpdateLearningStructureResponse(
    Guid Id,
    string Name,
    string? Description,
    string? Notes,
    string Status,
    DateTimeOffset UpdatedAt,
    string UpdatedBy);
