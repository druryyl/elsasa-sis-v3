namespace Elsasa.Sis.Finance.Features.Allocations.ReallocatePayment;

/// <summary>
/// Command response representing the outcome of a reallocation or reversal (TD-05, OQ-003).
/// </summary>
public sealed record ReallocatePaymentResponse(
    Guid PaymentId,
    string PaymentNumber,
    string ReversalReason,
    IReadOnlyList<Guid> SupersededAllocationIds,
    IReadOnlyList<Guid> ReversedAllocationIds,
    long PaymentAmount,
    long ActiveAllocatedAmount,
    long RemainingUnallocatedAmount,
    IReadOnlyList<ReallocationResultItemDto> ReplacementAllocations);
