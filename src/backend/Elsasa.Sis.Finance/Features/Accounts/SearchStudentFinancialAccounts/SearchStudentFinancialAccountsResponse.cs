namespace Elsasa.Sis.Finance.Features.Accounts.SearchStudentFinancialAccounts;

using Elsasa.Sis.Shared.Common;

/// <summary>
/// Paginated response container returning student financial account summaries (TD-02).
/// </summary>
public sealed record SearchStudentFinancialAccountsResponse : PaginatedList<StudentFinancialAccountListItemDto>
{
    public SearchStudentFinancialAccountsResponse(
        IReadOnlyList<StudentFinancialAccountListItemDto> items,
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
    /// Factory method calculating page counts and boundary states.
    /// </summary>
    public static new SearchStudentFinancialAccountsResponse Create(
        IReadOnlyList<StudentFinancialAccountListItemDto> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        ArgumentNullException.ThrowIfNull(items);

        var safePageSize = pageSize < 1 ? 1 : pageSize;
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var totalPages = (int)Math.Ceiling(totalCount / (double)safePageSize);

        return new SearchStudentFinancialAccountsResponse(
            items,
            safePageNumber,
            safePageSize,
            totalCount,
            totalPages,
            safePageNumber > 1,
            safePageNumber < totalPages);
    }
}
