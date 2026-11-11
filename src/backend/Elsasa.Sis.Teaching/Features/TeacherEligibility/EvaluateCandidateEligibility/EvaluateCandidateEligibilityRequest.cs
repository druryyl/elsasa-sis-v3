namespace Elsasa.Sis.Teaching.Features.TeacherEligibility.EvaluateCandidateEligibility;

/// <summary>
/// Request payload to trigger automated candidate teacher eligibility evaluation against a teaching demand (TD-03, P2-S05).
/// </summary>
/// <param name="TeachingDemandId">The teaching demand to evaluate against.</param>
/// <param name="TeacherProfileId">The candidate teacher to evaluate.</param>
/// <param name="CoordinatorRemarks">Optional commentary or audit notes provided by the coordinator.</param>
public sealed record EvaluateCandidateEligibilityRequest(
    Guid TeachingDemandId,
    Guid TeacherProfileId,
    string? CoordinatorRemarks = null);
