namespace Elsasa.Sis.Finance.Features.Allocations.ReallocatePayment;

/// <summary>
/// Command request to reallocate or reverse payment allocations with full audit lineage (TD-05, OQ-003).
/// </summary>
public sealed record ReallocatePaymentRequest(
    Guid PaymentId,
    IReadOnlyList<Guid> PriorAllocationIds,
    IReadOnlyList<ReplacementAllocationItemRequest>? ReplacementAllocations,
    string ReversalReason,
    string? Notes = null);
