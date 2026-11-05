namespace Elsasa.Sis.Academics.Features.Assessments.CancelAssessment;

/// <summary>
/// Response payload for a cancelled assessment (UC-ACD-05, P2-S04).
/// </summary>
public sealed record CancelAssessmentResponse(
    Guid Id,
    Guid LearningUnitId,
    string Title,
    string Status,
    string? Notes,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
