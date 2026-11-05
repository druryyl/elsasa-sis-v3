namespace Elsasa.Sis.Academics.Features.AcademicParticipations.UpdateParticipationStatus;

/// <summary>
/// Response payload for academic participation status transition (P2-S05).
/// </summary>
/// <param name="Id">The unique identifier of the academic participation.</param>
/// <param name="StudentId">Target student identifier.</param>
/// <param name="LearningUnitId">Target learning unit identifier.</param>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="AcademicPeriodLabel">Human-readable label of the academic period.</param>
/// <param name="ParticipationType">Participation type (standard, repeat, transferred_in, recognized_prior).</param>
/// <param name="AttemptNumber">Attempt counter.</param>
/// <param name="SectionLabel">Optional cohort section label.</param>
/// <param name="Status">Updated participation status.</param>
/// <param name="StartDate">Effective start date of participation.</param>
/// <param name="EndDate">End date of participation if withdrawn or concluded.</param>
/// <param name="CompletionDate">Completion date if concluded or recognized.</param>
/// <param name="Notes">Administrative or academic notes.</param>
/// <param name="UpdatedAt">Timestamp when updated in UTC.</param>
/// <param name="UpdatedBy">Operator identity who updated status.</param>
public sealed record UpdateParticipationStatusResponse(
    Guid Id,
    Guid StudentId,
    Guid LearningUnitId,
    Guid AcademicPeriodId,
    string AcademicPeriodLabel,
    string ParticipationType,
    int AttemptNumber,
    string? SectionLabel,
    string Status,
    DateOnly StartDate,
    DateOnly? EndDate,
    DateOnly? CompletionDate,
    string? Notes,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
