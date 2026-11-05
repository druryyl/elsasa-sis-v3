namespace Elsasa.Sis.Academics.Features.Assessments.UpdateAssessment;

/// <summary>
/// Response payload for an updated assessment (UC-ACD-05, P2-S04).
/// </summary>
public sealed record UpdateAssessmentResponse(
    Guid Id,
    Guid LearningUnitId,
    Guid? LearningActivityId,
    string Title,
    string AssessmentType,
    string AssessmentMethod,
    decimal? MaxScore,
    decimal? PassingScore,
    string ReassessmentPolicy,
    string? Criteria,
    DateOnly? AssessmentDate,
    string? AssessorReference,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
