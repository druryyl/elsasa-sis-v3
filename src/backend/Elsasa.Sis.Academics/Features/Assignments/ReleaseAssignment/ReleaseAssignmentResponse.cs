namespace Elsasa.Sis.Academics.Features.Assignments.ReleaseAssignment;

/// <summary>
/// Response payload for a released coursework assignment (TD-05, UC-ACD-04, P5-S10).
/// </summary>
public sealed record ReleaseAssignmentResponse(
    Guid Id,
    Guid LearningActivityId,
    string Title,
    string? Description,
    DateOnly ReleaseDate,
    DateOnly? DueDate,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
