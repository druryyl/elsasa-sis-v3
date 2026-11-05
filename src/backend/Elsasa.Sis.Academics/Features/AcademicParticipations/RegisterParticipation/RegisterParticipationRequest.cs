namespace Elsasa.Sis.Academics.Features.AcademicParticipations.RegisterParticipation;

/// <summary>
/// Request payload for individual student academic participation registration (P2-S04).
/// </summary>
/// <param name="StudentId">Target student identifier.</param>
/// <param name="LearningUnitId">Target learning unit identifier.</param>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="AcademicPeriodLabel">Human-readable label of the academic period.</param>
/// <param name="SectionLabel">Optional cohort section label (e.g. "4A").</param>
/// <param name="ParticipationType">Participation type (standard, repeat, transferred_in, recognized_prior).</param>
/// <param name="Source">Registration source (assigned, student_selected, learning_plan).</param>
/// <param name="StartDate">Effective start date of participation.</param>
public sealed record RegisterParticipationRequest(
    Guid StudentId,
    Guid LearningUnitId,
    Guid AcademicPeriodId,
    string AcademicPeriodLabel,
    string? SectionLabel = null,
    string ParticipationType = "standard",
    string Source = "assigned",
    DateOnly StartDate = default);
