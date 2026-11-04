namespace Elsasa.Sis.Finance.Features.Allocations.SuggestAllocations;

/// <summary>
/// Query request for smart payment allocation auto-suggestions (TD-05, OQ-003).
/// </summary>
public sealed record SuggestAllocationsRequest(
    Guid? StudentId,
    Guid? ApplicantId,
    long PaymentAmount);
