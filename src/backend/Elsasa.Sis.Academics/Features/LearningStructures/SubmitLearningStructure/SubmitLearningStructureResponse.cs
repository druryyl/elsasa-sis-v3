namespace Elsasa.Sis.Academics.Features.LearningStructures.SubmitLearningStructure;

/// <summary>
/// Response payload returned when a learning structure framework is submitted for formal authority review (P3-S07, TD-06).
/// </summary>
public sealed record SubmitLearningStructureResponse(
    Guid Id,
    string Status,
    DateTimeOffset UpdatedAt,
    string UpdatedBy);
