namespace Elsasa.Sis.Academics.Features.Assessments.CreateAssessment;

/// <summary>
/// Response payload for a newly created assessment (UC-ACD-05, ACD-WF-07, P2-S04).
/// </summary>
public sealed record CreateAssessmentResponse(
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
    string CreatedBy);
