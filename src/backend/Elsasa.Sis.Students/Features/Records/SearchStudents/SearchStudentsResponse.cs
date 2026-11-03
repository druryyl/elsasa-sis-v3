namespace Elsasa.Sis.Students.Features.Records.SearchStudents;

using Elsasa.Sis.Shared.Common;

/// <summary>
/// Paginated response container returning student summary cards.
/// </summary>
public sealed record SearchStudentsResponse : PaginatedList<StudentListItemDto>
{
    public SearchStudentsResponse(
        IReadOnlyList<StudentListItemDto> items,
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
    /// Factory method calculating page counts and boundaries.
    /// </summary>
    public static new SearchStudentsResponse Create(
        IReadOnlyList<StudentListItemDto> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        ArgumentNullException.ThrowIfNull(items);

        var safePageSize = pageSize < 1 ? 1 : pageSize;
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var totalPages = (int)Math.Ceiling(totalCount / (double)safePageSize);

        return new SearchStudentsResponse(
            items,
            safePageNumber,
            safePageSize,
            totalCount,
            totalPages,
            safePageNumber > 1,
            safePageNumber < totalPages);
    }
}
