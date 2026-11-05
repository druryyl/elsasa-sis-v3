namespace Elsasa.Sis.Academics.Features.LearningStructures.ApproveLearningStructure;

/// <summary>
/// Response payload returned when a learning structure framework is formally approved and activated (P3-S07, TD-06, TD-09).
/// </summary>
public sealed record ApproveLearningStructureResponse(
    Guid Id,
    string Status,
    string ApprovedBy,
    DateOnly ApprovedDate,
    DateTimeOffset UpdatedAt,
    string UpdatedBy);
