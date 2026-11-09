namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.GetStudentLearningPlans;

/// <summary>
/// Summary item for paginated or filterable learning plan roster navigation (SCR-ACD-04 Master Sidebar, P2-S06).
/// </summary>
/// <param name="Id">The unique identifier of the student learning plan.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="StudentName">Projected full name of the student from the Students domain.</param>
/// <param name="StudentNis">Projected student number (NIS) from the Students domain.</param>
/// <param name="AcademicPeriodId">The unique identifier of the associated operational academic period.</param>
/// <param name="AcademicPeriodLabel">The display label of the associated academic period.</param>
/// <param name="Status">The lifecycle status ('draft', 'active', 'revised', 'completed', 'archived').</param>
/// <param name="Version">The linear version identifier (starts at 1).</param>
/// <param name="UnitsCount">Number of constituent learning units configured for this plan.</param>
/// <param name="AdjustmentsCount">Number of academic adjustments configured for this plan.</param>
/// <param name="CreatedAt">The UTC timestamp when the plan was formulated.</param>
/// <param name="UpdatedAt">Optional UTC timestamp when the plan was last modified.</param>
public sealed record StudentLearningPlanListItemDto(
    Guid Id,
    Guid StudentId,
    string? StudentName,
    string? StudentNis,
    Guid AcademicPeriodId,
    string AcademicPeriodLabel,
    string Status,
    int Version,
    int UnitsCount,
    int AdjustmentsCount,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? UpdatedAt);
