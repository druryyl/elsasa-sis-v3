namespace Elsasa.Sis.Academics.Features.Assignments.GetAssignmentsByActivity;

/// <summary>
/// Data transfer object representing an assignment under a specific learning activity (P5-S10).
/// </summary>
public sealed record ActivityAssignmentItemDto(
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
