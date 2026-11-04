namespace Elsasa.Sis.Finance.Features.Payments.GetPayments;

/// <summary>
/// Paginated response payload for the payments queue and transaction history query.
/// </summary>
public sealed record GetPaymentsResponse(
    IReadOnlyList<PaymentListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
