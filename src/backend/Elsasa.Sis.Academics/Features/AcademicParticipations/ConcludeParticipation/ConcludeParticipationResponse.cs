namespace Elsasa.Sis.Academics.Features.AcademicParticipations.ConcludeParticipation;

/// <summary>
/// Response payload for concluded academic participation (P2-S05, UC-ACD-12).
/// </summary>
/// <param name="Id">The unique identifier of the concluded academic participation.</param>
/// <param name="StudentId">Target student identifier.</param>
/// <param name="LearningUnitId">Target learning unit identifier.</param>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="Status">Updated participation status (completed).</param>
/// <param name="StartDate">Start date of the participation.</param>
/// <param name="EndDate">End date of the participation.</param>
/// <param name="CompletionDate">Recorded completion date.</param>
/// <param name="Notes">Administrative or completion notes.</param>
/// <param name="UpdatedAt">Timestamp when updated in UTC.</param>
/// <param name="UpdatedBy">Operator identity who concluded the participation.</param>
public sealed record ConcludeParticipationResponse(
    Guid Id,
    Guid StudentId,
    Guid LearningUnitId,
    Guid AcademicPeriodId,
    string Status,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly CompletionDate,
    string? Notes,
    DateTimeOffset UpdatedAt,
    string UpdatedBy);
