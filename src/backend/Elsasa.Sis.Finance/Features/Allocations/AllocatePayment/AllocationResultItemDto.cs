namespace Elsasa.Sis.Finance.Features.Allocations.AllocatePayment;

/// <summary>
/// Data transfer object representing an applied allocation result (TD-05).
/// </summary>
public sealed record AllocationResultItemDto(
    Guid AllocationId,
    string AllocationNumber,
    Guid ObligationId,
    string ObligationNumber,
    long AllocatedAmount,
    string Status,
    DateTimeOffset AllocationDate);
