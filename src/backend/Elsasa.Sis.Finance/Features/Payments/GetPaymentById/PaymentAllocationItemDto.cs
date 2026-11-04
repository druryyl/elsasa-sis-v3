namespace Elsasa.Sis.Finance.Features.Payments.GetPaymentById;

/// <summary>
/// Data transfer object representing an allocation applied from a payment to an obligation (TD-05).
/// </summary>
public sealed record PaymentAllocationItemDto(
    Guid Id,
    string AllocationNumber,
    Guid ObligationId,
    string ObligationNumber,
    string ChargeType,
    string Description,
    long AllocatedAmount,
    DateTimeOffset AllocationDate,
    string AllocatedBy,
    string Status,
    Guid? SupersededById,
    string? ReversalReason);
