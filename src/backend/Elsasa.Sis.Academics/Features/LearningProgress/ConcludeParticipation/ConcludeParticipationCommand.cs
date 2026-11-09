namespace Elsasa.Sis.Academics.Features.LearningProgress.ConcludeParticipation;

/// <summary>
/// Command payload for formally concluding learning unit participation and updating learning progress (P2-S04, UC-ACD-12).
/// </summary>
/// <param name="ParticipationId">The unique identifier of the academic participation record.</param>
/// <param name="CompletionDate">Optional explicit completion date; defaults to current date if omitted.</param>
/// <param name="Notes">Optional conclusion notes.</param>
public sealed record ConcludeParticipationCommand(
    Guid ParticipationId,
    DateOnly? CompletionDate = null,
    string? Notes = null);
