namespace Elsasa.Sis.Teaching.Features.TeacherEligibility.EvaluateCandidateEligibility;

/// <summary>
/// Detailed response record representing the result of candidate teacher eligibility evaluation (TD-03, P2-S05).
/// </summary>
/// <param name="Id">The unique identifier of the persisted evaluation record.</param>
/// <param name="TeachingDemandId">The demand evaluated against.</param>
/// <param name="TeacherProfileId">The candidate teacher evaluated.</param>
/// <param name="DemandCode">Unique code of the evaluated demand.</param>
/// <param name="TeacherName">Full name of the candidate teacher.</param>
/// <param name="TeacherNumber">Official teacher identification number.</param>
/// <param name="EvaluationDate">Date the evaluation was executed.</param>
/// <param name="EvaluatedBy">Identity of the coordinator or automated engine executing the evaluation.</param>
/// <param name="IsEligible">Overall binary eligibility determination.</param>
/// <param name="EligibilityStatus">Detailed eligibility outcome status ('eligible', 'eligible_under_supervision', 'ineligible_unqualified', 'ineligible_conflicted', 'ineligible_capacity_exceeded').</param>
/// <param name="ProfileActiveCheck">Result of Check 1: teacher profile active standing.</param>
/// <param name="AuthorizationCheckPassed">Result of Check 2: active matching authorization.</param>
/// <param name="QualificationCheckPassed">Result of Check 3: verified qualification evidence if required.</param>
/// <param name="AvailabilityCheckPassed">Result of Check 4: schedule & unavailability window check.</param>
/// <param name="CapacityCheckPassed">Result of Check 5: remaining workload capacity within limits.</param>
/// <param name="Explanation">Human-readable natural language breakdown of check results.</param>
/// <param name="Notes">Optional coordinator remarks or audit commentary.</param>
/// <param name="CreatedAt">Timestamp when evaluation record was persisted in UTC.</param>
public sealed record EvaluateCandidateEligibilityResponse(
    Guid Id,
    Guid TeachingDemandId,
    Guid TeacherProfileId,
    string DemandCode,
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
