namespace Elsasa.Sis.Finance.Features.Allocations.AllocatePayment;

/// <summary>
/// Command request to explicitly allocate confirmed payment funds across obligations (TD-05, OQ-003).
/// </summary>
public sealed record AllocatePaymentRequest(
    Guid PaymentId,
    IReadOnlyList<AllocationItemRequest> Allocations,
    string? Notes = null);
