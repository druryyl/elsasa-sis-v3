namespace Elsasa.Sis.Academics.Features.AcademicInterventions.GetAtRiskLearners;

/// <summary>
/// Query parameters for dynamic evaluation of at-risk learners across an academic period (UC-ACD-11, P4-S10, TD-04).
/// </summary>
/// <param name="AcademicPeriodId">The required operational academic period identifier.</param>
/// <param name="GradeLevel">Optional grade level or cohort filter.</param>
public sealed record GetAtRiskLearnersQuery(
    Guid AcademicPeriodId,
    string? GradeLevel = null);
