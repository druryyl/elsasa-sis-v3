namespace Elsasa.Sis.Finance.Features.Allocations.SuggestAllocations;

/// <summary>
/// Data transfer object representing a simulated suggested allocation to an obligation (TD-05).
/// </summary>
public sealed record AllocationSuggestionItemDto(
    Guid ObligationId,
    string ObligationNumber,
    string ChargeType,
    string Description,
    bool IsMandatory,
    DateOnly DueDate,
    long OriginalAmount,
    long TotalPaid,
    long OutstandingAmount,
    long SuggestedAmount,
    long RemainingObligationBalance);
