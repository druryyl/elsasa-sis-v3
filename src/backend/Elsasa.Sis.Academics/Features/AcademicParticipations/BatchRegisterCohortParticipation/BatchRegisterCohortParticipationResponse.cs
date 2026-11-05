namespace Elsasa.Sis.Academics.Features.AcademicParticipations.BatchRegisterCohortParticipation;

/// <summary>
/// Response payload for cohort batch academic participation registration (P2-S04).
/// </summary>
/// <param name="RegisteredCount">Count of successfully registered new participations.</param>
/// <param name="SkippedCount">Count of skipped registrations that already exist.</param>
public sealed record BatchRegisterCohortParticipationResponse(
    int RegisteredCount,
    int SkippedCount);
