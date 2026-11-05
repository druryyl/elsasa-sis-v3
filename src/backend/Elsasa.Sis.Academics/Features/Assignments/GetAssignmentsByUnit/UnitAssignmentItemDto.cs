namespace Elsasa.Sis.Academics.Features.Assignments.GetAssignmentsByUnit;

/// <summary>
/// Data transfer object representing an assignment under a learning unit with originating activity details (P5-S10).
/// </summary>
public sealed record UnitAssignmentItemDto(
    Guid Id,
    Guid LearningActivityId,
    string LearningActivityTitle,
    string LearningActivityType,
    DateOnly LearningActivityDate,
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
