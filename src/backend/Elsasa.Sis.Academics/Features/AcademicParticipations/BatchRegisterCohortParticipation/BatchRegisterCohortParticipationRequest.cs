namespace Elsasa.Sis.Academics.Features.AcademicParticipations.BatchRegisterCohortParticipation;

/// <summary>
/// Request payload for cohort batch academic participation registration (P2-S04).
/// </summary>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="AcademicPeriodLabel">Human-readable label of the academic period.</param>
/// <param name="LearningUnitIds">Non-empty collection of learning unit identifiers.</param>
/// <param name="SectionLabel">Cohort section partition label (e.g., "4A").</param>
/// <param name="StartDate">Effective start date of participation.</param>
/// <param name="StudentIds">Optional subset of student IDs to place; if omitted, active students placed in the section are queried.</param>
public sealed record BatchRegisterCohortParticipationRequest(
    Guid AcademicPeriodId,
    string AcademicPeriodLabel,
    IReadOnlyList<Guid> LearningUnitIds,
    string SectionLabel,
    DateOnly StartDate,
    IReadOnlyList<Guid>? StudentIds = null);
