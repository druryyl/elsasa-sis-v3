namespace Elsasa.Sis.Academics.Features.PromotionRecommendations.IssuePromotionRecommendation;

/// <summary>
/// Command record for certifying and issuing an individual student's academic progression recommendation (P3-S08, UC-ACD-17).
/// </summary>
/// <param name="StudentId">Target student identifier.</param>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="Recommendation">Progression recommendation ('promotion_eligible', 'retention_recommended', 'conditional_promotion').</param>
/// <param name="FromLevel">Current grade level or developmental stage of the student.</param>
/// <param name="RecommendedToLevel">Recommended destination grade level, or null for retention.</param>
/// <param name="Conditions">Mandatory conditions or stipulations for conditional promotion, if applicable.</param>
/// <param name="SupportingEvidence">Summary of evidence supporting this progression recommendation.</param>
/// <param name="AcademicStandingId">Optional identifier of the linked academic standing determination record.</param>
/// <param name="IsException">Indicates whether this recommendation is an authorized committee exception override.</param>
/// <param name="ExceptionReason">Mandatory justification narrative when determined as an exception (min 5 chars).</param>
/// <param name="CommitteeReference">Formal decree or committee reference (e.g., SK Dewan Guru, min 5 chars).</param>
public sealed record IssuePromotionRecommendationCommand(
    Guid StudentId,
    Guid AcademicPeriodId,
    string Recommendation,
    string FromLevel,
    string? RecommendedToLevel = null,
    string? Conditions = null,
    string SupportingEvidence = "",
    Guid? AcademicStandingId = null,
    bool IsException = false,
    string? ExceptionReason = null,
    string? CommitteeReference = null);
