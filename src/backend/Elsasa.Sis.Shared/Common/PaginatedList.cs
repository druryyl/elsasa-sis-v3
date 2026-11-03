namespace Elsasa.Sis.Shared.Common;

/// <summary>
/// Universal paginated response container matching frontend PaginatedResponse (ADR-004, ADR-005).
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public record PaginatedList<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage)
{
    /// <summary>
    /// Creates a new <see cref="PaginatedList{T}"/> calculating total pages and navigation flags.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Convenient factory method for paginated lists.")]
    public static PaginatedList<T> Create(
        IReadOnlyList<T> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        ArgumentNullException.ThrowIfNull(items);

        var safePageSize = pageSize < 1 ? 1 : pageSize;
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var totalPages = (int)Math.Ceiling(totalCount / (double)safePageSize);

        return new PaginatedList<T>(
            items,
            safePageNumber,
            safePageSize,
            totalCount,
            totalPages,
            safePageNumber > 1,
            safePageNumber < totalPages);
    }
}
