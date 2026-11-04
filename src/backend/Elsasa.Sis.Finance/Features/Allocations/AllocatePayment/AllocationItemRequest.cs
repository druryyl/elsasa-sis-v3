namespace Elsasa.Sis.Finance.Features.Allocations.AllocatePayment;

/// <summary>
/// Individual allocation line item requested against an obligation (TD-05).
/// </summary>
public sealed record AllocationItemRequest(
    Guid ObligationId,
    long Amount,
    string? Notes = null);
