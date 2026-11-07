namespace Elsasa.Sis.Academics.Features.PromotionRecommendations.BatchIssuePromotionRecommendations;

/// <summary>
/// Command record for certifying and issuing cohort batch academic progression recommendations (P3-S08, UC-ACD-17, TD-03).
/// </summary>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="Items">Collection of individual student progression recommendation items to certify.</param>
public sealed record BatchIssuePromotionRecommendationsCommand(
    Guid AcademicPeriodId,
    IReadOnlyList<ProgressionItemDto> Items);
