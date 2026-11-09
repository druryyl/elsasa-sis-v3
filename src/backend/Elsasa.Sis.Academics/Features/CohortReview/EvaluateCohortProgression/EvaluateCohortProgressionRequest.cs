namespace Elsasa.Sis.Academics.Features.CohortReview.EvaluateCohortProgression;

/// <summary>
/// Request record for dynamically evaluating proposed grade progression recommendations across a cohort (P3-S06).
/// </summary>
/// <param name="AcademicPeriodId">The unique identifier of the academic period.</param>
/// <param name="GradeLevel">The cohort grade level to evaluate (e.g., "Grade 4").</param>
public sealed record EvaluateCohortProgressionRequest(
    Guid AcademicPeriodId,
    string GradeLevel);
