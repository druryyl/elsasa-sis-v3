namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.GetStudentLearningPlans;

using Elsasa.Sis.Shared.Common;

/// <summary>
/// Universal paginated response container matching PaginatedList and frontend PaginatedResponse (P2-S06).
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed record PagedResult<T> : PaginatedList<T>
{
    public PagedResult(
        IReadOnlyList<T> items,
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
    /// Creates a new <see cref="PagedResult{T}"/> calculating total pages and navigation flags.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Convenient factory method for paginated lists.")]
    public static new PagedResult<T> Create(
        IReadOnlyList<T> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        var list = PaginatedList<T>.Create(items, totalCount, pageNumber, pageSize);
        return new PagedResult<T>(
            list.Items,
            list.PageNumber,
            list.PageSize,
            list.TotalCount,
            list.TotalPages,
            list.HasPreviousPage,
            list.HasNextPage);
    }
}
