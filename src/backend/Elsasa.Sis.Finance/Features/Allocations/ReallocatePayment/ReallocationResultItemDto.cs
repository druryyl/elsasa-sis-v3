namespace Elsasa.Sis.Finance.Features.Allocations.ReallocatePayment;

/// <summary>
/// Data transfer object representing a new active replacement allocation resulting from reallocation (TD-05).
/// </summary>
public sealed record ReallocationResultItemDto(
    Guid AllocationId,
    string AllocationNumber,
    Guid ObligationId,
    string ObligationNumber,
    long AllocatedAmount,
    string Status,
    DateTimeOffset AllocationDate);
