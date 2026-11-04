namespace Elsasa.Sis.Finance.Features.Obligations.GetObligations;

using Elsasa.Sis.Shared.Common;

/// <summary>
/// Paginated list response for financial obligations matching filter criteria.
/// </summary>
public sealed record GetObligationsResponse : PaginatedList<ObligationListItemDto>
{
    public GetObligationsResponse(
        IReadOnlyList<ObligationListItemDto> items,
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
    /// Factory creating a paginated response calculating total pages and navigation boundaries.
    /// </summary>
    public static new GetObligationsResponse Create(
        IReadOnlyList<ObligationListItemDto> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        ArgumentNullException.ThrowIfNull(items);

        var safePageSize = pageSize < 1 ? 1 : pageSize;
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var totalPages = (int)Math.Ceiling(totalCount / (double)safePageSize);

        return new GetObligationsResponse(
            items,
            safePageNumber,
            safePageSize,
            totalCount,
            totalPages,
            safePageNumber > 1,
            safePageNumber < totalPages);
    }
}
