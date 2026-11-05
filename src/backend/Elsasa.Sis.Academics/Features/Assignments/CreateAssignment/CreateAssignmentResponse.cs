namespace Elsasa.Sis.Academics.Features.Assignments.CreateAssignment;

/// <summary>
/// Response payload for a newly created coursework assignment (TD-05, UC-ACD-04, P5-S10).
/// </summary>
public sealed record CreateAssignmentResponse(
    Guid Id,
    Guid LearningActivityId,
    string Title,
    string? Description,
    DateOnly ReleaseDate,
    DateOnly? DueDate,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy);
