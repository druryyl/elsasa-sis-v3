namespace Elsasa.Sis.Finance.Features.Allocations.ReallocatePayment;

/// <summary>
/// Individual replacement allocation line item requested during reallocation (TD-05).
/// </summary>
public sealed record ReplacementAllocationItemRequest(
    Guid ObligationId,
    long Amount,
    string? Notes = null);
