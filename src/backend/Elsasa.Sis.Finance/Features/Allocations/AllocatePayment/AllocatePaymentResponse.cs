namespace Elsasa.Sis.Finance.Features.Allocations.AllocatePayment;

/// <summary>
/// Command response returning the committed allocations and resulting balances for the payment (TD-05, OQ-003).
/// </summary>
public sealed record AllocatePaymentResponse(
    Guid PaymentId,
    string PaymentNumber,
    long PaymentAmount,
    long TotalAllocatedInRequest,
    long CumulativeAllocatedAmount,
    long RemainingUnallocatedAmount,
    IReadOnlyList<AllocationResultItemDto> Allocations);
