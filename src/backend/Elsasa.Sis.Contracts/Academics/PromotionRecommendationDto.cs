namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Strongly-typed contract DTO representing an academic progression recommendation for a student (ADR-001, UC-ACD-17).
/// </summary>
/// <param name="Id">The unique identifier of the promotion recommendation record.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="AcademicPeriodId">The unique identifier of the academic period.</param>
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
public sealed record PromotionRecommendationDto(
    Guid Id,
    Guid StudentId,
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
    DateTimeOffset IssuedAt)
{
    /// <summary>
    /// Gets the unique identifier of the promotion recommendation (alias for <see cref="Id"/>).
    /// </summary>
    public Guid RecommendationId => Id;
}
