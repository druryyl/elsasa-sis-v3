namespace Elsasa.Sis.Academics.Features.PromotionRecommendations.BatchIssuePromotionRecommendations;

using Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Response payload summarizing batch academic progression recommendation certification (P3-S08, UC-ACD-17).
/// </summary>
/// <param name="TotalProcessed">Count of recommendation records certified and persisted.</param>
/// <param name="Recommendations">List of certified progression recommendation DTOs.</param>
public sealed record BatchIssuePromotionRecommendationsResponse(
    int TotalProcessed,
    IReadOnlyList<PromotionRecommendationDto> Recommendations);
