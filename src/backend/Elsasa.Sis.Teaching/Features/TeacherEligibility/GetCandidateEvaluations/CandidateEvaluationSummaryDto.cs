namespace Elsasa.Sis.Teaching.Features.TeacherEligibility.GetCandidateEvaluations;

/// <summary>
/// Summary DTO representing an evaluation item when querying historical candidate evaluations (TD-03, P2-S05).
/// </summary>
public sealed record CandidateEvaluationSummaryDto(
    Guid Id,
    Guid TeachingDemandId,
    string DemandCode,
    Guid TeacherProfileId,
    string TeacherName,
    string TeacherNumber,
    DateOnly EvaluationDate,
    string EvaluatedBy,
    bool IsEligible,
    string EligibilityStatus,
    bool ProfileActiveCheck,
    bool AuthorizationCheckPassed,
    bool QualificationCheckPassed,
    bool AvailabilityCheckPassed,
    bool CapacityCheckPassed,
    string Explanation,
    string? Notes,
    DateTimeOffset CreatedAt);
