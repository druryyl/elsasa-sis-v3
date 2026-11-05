namespace Elsasa.Sis.Academics.Features.Assessments.GetAssessmentsByUnit;

/// <summary>
/// Summary DTO for an assessment item grouped under a learning unit (P2-S05).
/// </summary>
public sealed record AssessmentSummaryDto(
    Guid Id,
    Guid LearningUnitId,
    Guid? LearningActivityId,
    string? LearningActivityTitle,
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
    int EvidenceCount,
    int ResultCount,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
