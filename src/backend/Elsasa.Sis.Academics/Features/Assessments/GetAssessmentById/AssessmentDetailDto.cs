namespace Elsasa.Sis.Academics.Features.Assessments.GetAssessmentById;

/// <summary>
/// Detailed DTO for an assessment including parent unit/activity information, rubric criteria, and participant counts (P2-S05).
/// </summary>
public sealed record AssessmentDetailDto(
    Guid Id,
    Guid LearningUnitId,
    string LearningUnitTitle,
    string LearningUnitCode,
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
    int ParticipantCount,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
