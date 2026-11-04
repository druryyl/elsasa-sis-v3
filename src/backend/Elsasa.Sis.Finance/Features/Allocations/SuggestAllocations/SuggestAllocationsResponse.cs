namespace Elsasa.Sis.Finance.Features.Allocations.SuggestAllocations;

/// <summary>
/// Query response representing simulated allocation recommendations and remaining unallocated balance (TD-05, OQ-003).
/// </summary>
public sealed record SuggestAllocationsResponse(
    Guid? StudentId,
    Guid? ApplicantId,
    long PaymentAmount,
    long TotalSuggestedAllocated,
    long RemainingUnallocatedAmount,
    IReadOnlyList<AllocationSuggestionItemDto> Suggestions);
