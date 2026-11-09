namespace Elsasa.Sis.Academics.Features.CohortReview.EvaluateCohortStanding;

/// <summary>
/// Request record for dynamically evaluating proposed academic standings across a cohort (P3-S06).
/// </summary>
/// <param name="AcademicPeriodId">The unique identifier of the academic period.</param>
/// <param name="GradeLevel">The cohort grade level to evaluate (e.g., "Grade 4").</param>
public sealed record EvaluateCohortStandingRequest(
    Guid AcademicPeriodId,
    string GradeLevel);
