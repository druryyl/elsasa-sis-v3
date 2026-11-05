namespace Elsasa.Sis.Academics.Features.Assessments.ConductAssessment;

/// <summary>
/// Response payload for a conducted assessment (UC-ACD-05, P2-S04).
/// </summary>
public sealed record ConductAssessmentResponse(
    Guid Id,
    Guid LearningUnitId,
    string Title,
    string Status,
    DateOnly? AssessmentDate,
    string? Notes,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
