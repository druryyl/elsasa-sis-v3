namespace Elsasa.Sis.Finance.Features.Obligations.BatchAssessObligations;

/// <summary>
/// Summary response for batch obligation assessment containing metrics breakdown, candidate list, and created IDs.
/// </summary>
/// <param name="TotalCandidatesCount">Total count of student candidates matching placement and rule criteria.</param>
/// <param name="EligibleCandidatesCount">Count of candidate students eligible for assessment (excluding duplicates).</param>
/// <param name="DuplicateCount">Count of candidate students skipped due to existing duplicate obligations.</param>
/// <param name="GrossAssessedAmount">Total gross assessed monetary liability amount in IDR integer representation.</param>
/// <param name="Candidates">Detailed list of evaluated candidate student items.</param>
/// <param name="CreatedObligationIds">List of created obligation UUIDs (populated in commit mode).</param>
public sealed record BatchAssessObligationsResponse(
    int TotalCandidatesCount,
    int EligibleCandidatesCount,
    int DuplicateCount,
    long GrossAssessedAmount,
    IReadOnlyList<BatchAssessCandidateDto> Candidates,
    IReadOnlyList<Guid> CreatedObligationIds);
