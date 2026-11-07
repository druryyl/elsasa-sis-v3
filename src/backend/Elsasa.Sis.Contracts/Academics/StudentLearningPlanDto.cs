namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Strongly-typed contract DTO representing a complete student learning plan, including constituent units, adjustments, and approval history.
/// </summary>
/// <param name="Id">The unique identifier of the student learning plan.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="AcademicPeriodId">The unique identifier of the associated academic period.</param>
/// <param name="AcademicPeriodLabel">The display label of the associated academic period.</param>
/// <param name="Status">The lifecycle status ('draft', 'active', 'revised', 'completed', 'archived').</param>
/// <param name="Version">The linear version identifier (starts at 1).</param>
/// <param name="Notes">Optional coordinator or overarching plan notes.</param>
/// <param name="CreatedAt">The UTC timestamp when the plan was formulated.</param>
/// <param name="CreatedBy">Username of the actor who formulated the plan.</param>
/// <param name="UpdatedAt">Optional UTC timestamp when the plan was last modified.</param>
/// <param name="UpdatedBy">Optional username of the actor who last modified the plan.</param>
/// <param name="ConstituentUnits">The collection of constituent learning units configured for this plan.</param>
/// <param name="Adjustments">The collection of academic adjustments (accommodations, substitutions, exemptions) configured for this plan.</param>
/// <param name="Approvals">The immutable audit log of formal approval decisions recorded for this plan.</param>
public sealed record StudentLearningPlanDto(
    Guid Id,
    Guid StudentId,
    Guid AcademicPeriodId,
    string AcademicPeriodLabel,
    string Status,
    int Version,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy,
    IReadOnlyList<SlpLearningUnitDto> ConstituentUnits,
    IReadOnlyList<AcademicPlanAdjustmentDto> Adjustments,
    IReadOnlyList<SlpApprovalDto> Approvals)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StudentLearningPlanDto"/> record without constituent collections.
    /// </summary>
    public StudentLearningPlanDto(
        Guid id,
        Guid studentId,
        Guid academicPeriodId,
        string academicPeriodLabel,
        string status,
        int version,
        string? notes,
        DateTimeOffset createdAt,
        string createdBy,
        DateTimeOffset? updatedAt,
        string? updatedBy)
        : this(
            id,
            studentId,
            academicPeriodId,
            academicPeriodLabel,
            status,
            version,
            notes,
            createdAt,
            createdBy,
            updatedAt,
            updatedBy,
            [],
            [],
            [])
    {
    }
}
