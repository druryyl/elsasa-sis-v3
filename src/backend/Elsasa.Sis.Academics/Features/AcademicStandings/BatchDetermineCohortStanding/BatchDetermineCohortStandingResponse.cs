namespace Elsasa.Sis.Academics.Features.AcademicStandings.BatchDetermineCohortStanding;

using Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Response payload for cohort batch academic standing determination (P3-S07).
/// </summary>
/// <param name="TotalProcessed">Count of student standing determinations successfully certified.</param>
/// <param name="Determinations">List of certified student academic standing records.</param>
public sealed record BatchDetermineCohortStandingResponse(
    int TotalProcessed,
    IReadOnlyList<StudentAcademicStandingDto> Determinations);
