namespace Elsasa.Sis.Academics.Features.CohortReview;

/// <summary>
/// Data transfer object representing a proposed grade progression recommendation for a student in a cohort review (TD-03, UC-ACD-17, P3-S06).
/// </summary>
public sealed record CohortProgressionProposalDto(
    Guid StudentId,
    string? StudentNumber,
    string StudentName,
    string FromLevel,
    string? RecommendedToLevel,
    string ProposedRecommendation,
    string? Conditions,
    string SupportingEvidence,
    string CurrentStanding,
    bool IsStandingCommitted = false);
