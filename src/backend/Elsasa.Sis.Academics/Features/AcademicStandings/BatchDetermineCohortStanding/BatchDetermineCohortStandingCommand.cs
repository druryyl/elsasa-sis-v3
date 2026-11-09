namespace Elsasa.Sis.Academics.Features.AcademicStandings.BatchDetermineCohortStanding;

/// <summary>
/// Command payload for certifying cohort batch academic standing determinations (P3-S07, UC-ACD-17, TD-03).
/// </summary>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="Items">Collection of student academic standing determination items.</param>
public sealed record BatchDetermineCohortStandingCommand(
    Guid AcademicPeriodId,
    IReadOnlyList<StandingItemDto> Items);
