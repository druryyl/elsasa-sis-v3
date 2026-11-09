namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.GetStudentLearningPlanById;

using Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Authoritative record of a complete Student Learning Plan with constituent units, adjustments, and approval history (SCR-ACD-04, P2-S06).
/// </summary>
/// <param name="Id">The unique identifier of the student learning plan.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="StudentName">Projected full name of the student from the Students domain.</param>
/// <param name="StudentNis">Projected student number (NIS) from the Students domain.</param>
/// <param name="AcademicPeriodId">The unique identifier of the associated operational academic period.</param>
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
public sealed record StudentLearningPlanDetailDto(
    Guid Id,
    Guid StudentId,
    string? StudentName,
    string? StudentNis,
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
    IReadOnlyList<SlpApprovalDto> Approvals);
