namespace Elsasa.Sis.Finance.Features.Payers.GetPayers;

using Elsasa.Sis.Shared.Common;

/// <summary>
/// Paginated response container returning payer list items with total count and page metadata.
/// </summary>
public sealed record GetPayersResponse : PaginatedList<PayerListItemDto>
{
    public GetPayersResponse(
        IReadOnlyList<PayerListItemDto> items,
        int pageNumber,
        int pageSize,
        int totalCount,
        int totalPages,
        bool hasPreviousPage,
        bool hasNextPage)
        : base(items, pageNumber, pageSize, totalCount, totalPages, hasPreviousPage, hasNextPage)
    {
    }

    /// <summary>
    /// Factory method calculating page counts and boundary navigation flags.
    /// </summary>
    public static new GetPayersResponse Create(
        IReadOnlyList<PayerListItemDto> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        var baseList = PaginatedList<PayerListItemDto>.Create(items, totalCount, pageNumber, pageSize);
        return new GetPayersResponse(
            baseList.Items,
            baseList.PageNumber,
            baseList.PageSize,
            baseList.TotalCount,
            baseList.TotalPages,
            baseList.HasPreviousPage,
            baseList.HasNextPage);
    }
}
