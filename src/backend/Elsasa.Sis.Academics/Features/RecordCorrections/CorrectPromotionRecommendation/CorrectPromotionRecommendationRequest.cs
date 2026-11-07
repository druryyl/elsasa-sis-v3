namespace Elsasa.Sis.Academics.Features.RecordCorrections.CorrectPromotionRecommendation;

/// <summary>
/// Request payload for auditable correction of an approved promotion recommendation (P5-S12, UC-ACD-15, TD-05).
/// </summary>
/// <param name="Recommendation">Corrected recommendation ('promotion_eligible', 'retention_recommended', 'conditional_promotion').</param>
/// <param name="FromLevel">Student's current grade level at conclusion of period.</param>
/// <param name="RecommendedToLevel">Recommended destination level (nullable for retention).</param>
/// <param name="Conditions">Mandatory conditions when recommendation is 'conditional_promotion'.</param>
/// <param name="SupportingEvidence">Summary of evidence supporting recommendation.</param>
/// <param name="IsException">Indicates whether this recommendation is an authorized committee exception.</param>
/// <param name="ExceptionReason">Mandatory justification narrative when recommended as an exception (min 5 chars).</param>
/// <param name="CommitteeReference">Formal decree or committee reference (e.g., SK Dewan Guru, min 5 chars).</param>
/// <param name="Reason">Mandatory justification explaining the historical recommendation correction (min 10 chars).</param>
/// <param name="Id">Optional recommendation ID if included in request payload.</param>
public sealed record CorrectPromotionRecommendationRequest(
    string Recommendation,
    string FromLevel,
    string? RecommendedToLevel = null,
    string? Conditions = null,
    string SupportingEvidence = "",
    bool IsException = false,
    string? ExceptionReason = null,
    string? CommitteeReference = null,
    string Reason = "",
    Guid? Id = null);
