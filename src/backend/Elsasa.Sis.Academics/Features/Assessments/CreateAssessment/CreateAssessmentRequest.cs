namespace Elsasa.Sis.Academics.Features.Assessments.CreateAssessment;

/// <summary>
/// Request payload for creating a planned assessment event (UC-ACD-05, ACD-WF-07, P2-S04).
/// </summary>
public sealed record CreateAssessmentRequest(
    Guid LearningUnitId,
    Guid? LearningActivityId,
    string Title,
    string AssessmentType,
    string AssessmentMethod,
    decimal? MaxScore,
    decimal? PassingScore,
    string? ReassessmentPolicy,
    string? Criteria,
    DateOnly? AssessmentDate,
    string? AssessorReference,
    string? Notes);
