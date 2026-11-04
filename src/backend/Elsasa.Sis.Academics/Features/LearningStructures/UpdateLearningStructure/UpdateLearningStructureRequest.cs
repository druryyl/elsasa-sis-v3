namespace Elsasa.Sis.Academics.Features.LearningStructures.UpdateLearningStructure;

/// <summary>
/// Request payload for updating non-structural metadata of a learning structure framework (ADR-004, ADR-005, P3-S06).
/// </summary>
public sealed record UpdateLearningStructureRequest(
    string Name,
    string? Description,
    string? Notes);
