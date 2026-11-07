namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when an academic progression recommendation is formally issued (ADR-003, UC-ACD-17).
/// </summary>
/// <param name="RecommendationId">The unique identifier of the promotion recommendation record.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="AcademicPeriodId">The unique identifier of the academic period.</param>
/// <param name="Recommendation">The progression recommendation ('promotion_eligible', 'retention_recommended', 'conditional_promotion').</param>
/// <param name="FromLevel">The current grade level or developmental stage of the student.</param>
/// <param name="RecommendedToLevel">The recommended destination grade level or stage, or null for retention.</param>
/// <param name="IsException">Indicates whether the recommendation was issued as an authorized committee exception.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record PromotionRecommendationIssuedEvent(
    Guid RecommendationId,
    Guid StudentId,
    Guid AcademicPeriodId,
    string Recommendation,
    string FromLevel,
    string? RecommendedToLevel,
    bool IsException,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PromotionRecommendationIssuedEvent"/> record with current UTC timestamp.
    /// </summary>
    /// <param name="recommendationId">The unique identifier of the promotion recommendation record.</param>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="academicPeriodId">The unique identifier of the academic period.</param>
    /// <param name="recommendation">The progression recommendation.</param>
    /// <param name="fromLevel">The current grade level or developmental stage of the student.</param>
    /// <param name="recommendedToLevel">The recommended destination grade level or stage, or null for retention.</param>
    /// <param name="isException">Indicates whether the recommendation was issued as an authorized committee exception.</param>
    public PromotionRecommendationIssuedEvent(
        Guid recommendationId,
        Guid studentId,
        Guid academicPeriodId,
        string recommendation,
        string fromLevel,
        string? recommendedToLevel,
        bool isException)
        : this(recommendationId, studentId, academicPeriodId, recommendation, fromLevel, recommendedToLevel, isException, DateTimeOffset.UtcNow)
    {
    }
}
