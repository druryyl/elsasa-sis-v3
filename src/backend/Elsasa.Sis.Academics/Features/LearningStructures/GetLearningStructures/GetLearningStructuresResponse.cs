namespace Elsasa.Sis.Academics.Features.LearningStructures.GetLearningStructures;

using Elsasa.Sis.Shared.Common;

/// <summary>
/// Paginated list response for learning structures inquiry (ADR-004, ADR-005, P3-S06).
/// </summary>
public sealed record GetLearningStructuresResponse : PaginatedList<LearningStructureListItemDto>
{
    public GetLearningStructuresResponse(
        IReadOnlyList<LearningStructureListItemDto> items,
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
    /// Factory creating a paginated response instance with calculated page totals and navigation flags.
    /// </summary>
    public static new GetLearningStructuresResponse Create(
        IReadOnlyList<LearningStructureListItemDto> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        ArgumentNullException.ThrowIfNull(items);

        var safePageSize = pageSize < 1 ? 1 : pageSize;
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var totalPages = (int)Math.Ceiling(totalCount / (double)safePageSize);

        return new GetLearningStructuresResponse(
            items,
            safePageNumber,
            safePageSize,
            totalCount,
            totalPages,
            safePageNumber > 1,
            safePageNumber < totalPages);
    }
}
