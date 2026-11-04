namespace Elsasa.Sis.Academics.Features.LearningStructures.ArchiveLearningStructure;

/// <summary>
/// Response payload returned when a learning structure framework is archived (P3-S07, TD-06).
/// </summary>
public sealed record ArchiveLearningStructureResponse(
    Guid Id,
    string Status,
    DateTimeOffset UpdatedAt,
    string UpdatedBy);
