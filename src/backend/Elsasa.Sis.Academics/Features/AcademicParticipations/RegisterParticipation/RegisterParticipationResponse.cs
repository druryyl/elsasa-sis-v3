namespace Elsasa.Sis.Academics.Features.AcademicParticipations.RegisterParticipation;

/// <summary>
/// Response payload for single academic participation registration (P2-S04).
/// </summary>
/// <param name="Id">The created academic participation identifier.</param>
/// <param name="StudentId">Target student identifier.</param>
/// <param name="LearningUnitId">Target learning unit identifier.</param>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="AcademicPeriodLabel">Human-readable label of the academic period.</param>
/// <param name="ParticipationType">Participation type (standard, repeat, transferred_in, recognized_prior).</param>
/// <param name="AttemptNumber">Computed attempt number.</param>
/// <param name="Source">Registration source (assigned, student_selected, learning_plan).</param>
/// <param name="SectionLabel">Optional cohort section label.</param>
/// <param name="Status">Initial participation status (in_progress).</param>
/// <param name="StartDate">Effective start date of participation.</param>
/// <param name="CreatedAt">Timestamp when created in UTC.</param>
/// <param name="CreatedBy">Operator username or email who initiated registration.</param>
public sealed record RegisterParticipationResponse(
    Guid Id,
    Guid StudentId,
    Guid LearningUnitId,
    Guid AcademicPeriodId,
    string AcademicPeriodLabel,
    string ParticipationType,
    int AttemptNumber,
    string Source,
    string? SectionLabel,
    string Status,
    DateOnly StartDate,
    DateTimeOffset CreatedAt,
    string CreatedBy);
