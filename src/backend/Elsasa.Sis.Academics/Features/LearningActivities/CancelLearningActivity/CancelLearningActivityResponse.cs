namespace Elsasa.Sis.Academics.Features.LearningActivities.CancelLearningActivity;

/// <summary>
/// Response returned upon cancelling a learning activity (P3-S07).
/// </summary>
public sealed record CancelLearningActivityResponse(
    Guid Id,
    Guid LearningUnitId,
    string Title,
    string Status,
    string? Notes,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
