namespace Elsasa.Sis.Academics.Features.AcademicParticipations.ConcludeParticipation;

/// <summary>
/// Request payload for concluding an active learning unit participation (P2-S05, GAP-08, UC-ACD-12).
/// </summary>
/// <param name="CompletionDate">The date when learning was completed.</param>
/// <param name="Notes">Optional administrative or completion notes.</param>
public sealed record ConcludeParticipationRequest(
    DateOnly CompletionDate,
    string? Notes = null);
