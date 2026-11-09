namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when an approved progression recommendation is corrected (ADR-003, UC-ACD-15).
/// </summary>
/// <param name="RecommendationId">The unique identifier of the corrected promotion recommendation record.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="AcademicPeriodId">The unique identifier of the academic period.</param>
/// <param name="OldRecommendation">The previous progression recommendation.</param>
/// <param name="NewRecommendation">The corrected new progression recommendation.</param>
/// <param name="Reason">The mandatory correction justification narrative.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record PromotionRecommendationCorrectedEvent(
    Guid RecommendationId,
    Guid StudentId,
    Guid AcademicPeriodId,
    string OldRecommendation,
    string NewRecommendation,
    string Reason,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PromotionRecommendationCorrectedEvent"/> record with current UTC timestamp.
    /// </summary>
    /// <param name="recommendationId">The unique identifier of the corrected promotion recommendation record.</param>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="academicPeriodId">The unique identifier of the academic period.</param>
    /// <param name="oldRecommendation">The previous progression recommendation.</param>
    /// <param name="newRecommendation">The corrected new progression recommendation.</param>
    /// <param name="reason">The mandatory correction justification narrative.</param>
    public PromotionRecommendationCorrectedEvent(
        Guid recommendationId,
        Guid studentId,
        Guid academicPeriodId,
        string oldRecommendation,
        string newRecommendation,
        string reason)
        : this(recommendationId, studentId, academicPeriodId, oldRecommendation, newRecommendation, reason, DateTimeOffset.UtcNow)
    {
    }
}
