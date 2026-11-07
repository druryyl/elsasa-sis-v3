namespace Elsasa.Sis.Academics.Features.PromotionRecommendations.GetCohortPromotionRecommendations;

/// <summary>
/// Data transfer object representing a committed academic progression recommendation for a student in a cohort review (TD-03, UC-ACD-17, P3-S09, SCR-ACD-09).
/// </summary>
/// <param name="Id">The unique identifier of the promotion recommendation record.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="StudentNumber">The student registration number or identifier.</param>
/// <param name="StudentName">The full name of the student.</param>
/// <param name="AcademicPeriodId">The academic period identifier.</param>
/// <param name="Recommendation">The progression recommendation (e.g., 'promotion_eligible', 'retention_recommended', 'conditional_promotion').</param>
/// <param name="FromLevel">The current grade level or developmental stage of the student.</param>
/// <param name="RecommendedToLevel">The recommended destination grade level or stage, or null for retention.</param>
/// <param name="Conditions">Mandatory conditions or stipulations for conditional promotion, if applicable.</param>
/// <param name="SupportingEvidence">Summary of evidence supporting this progression recommendation.</param>
/// <param name="AcademicStandingId">The identifier of the linked academic standing determination, if linked.</param>
/// <param name="IsException">Indicates whether the recommendation is an authorized committee exception override.</param>
/// <param name="ExceptionReason">Mandatory justification narrative when determined as an exception.</param>
/// <param name="CommitteeReference">Reference to formal decree or minutes (e.g., SK Dewan Guru), if an exception.</param>
/// <param name="AuthorizedBy">Username or identifier of the certifying Academic Authority.</param>
/// <param name="IssuedAt">The timestamp when the recommendation was issued.</param>
/// <param name="CreatedAt">The timestamp when the record was created.</param>
/// <param name="UpdatedAt">The timestamp when the record was last updated, if any.</param>
public sealed record CohortPromotionRecommendationItemDto(
    Guid Id,
    Guid StudentId,
    string? StudentNumber,
    string StudentName,
    Guid AcademicPeriodId,
    string Recommendation,
    string FromLevel,
    string? RecommendedToLevel,
    string? Conditions,
    string SupportingEvidence,
    Guid? AcademicStandingId,
    bool IsException,
    string? ExceptionReason,
    string? CommitteeReference,
    string AuthorizedBy,
    DateTimeOffset IssuedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    /// <summary>
    /// Gets the unique identifier of the promotion recommendation (alias for <see cref="Id"/>).
    /// </summary>
    public Guid RecommendationId => Id;
}
