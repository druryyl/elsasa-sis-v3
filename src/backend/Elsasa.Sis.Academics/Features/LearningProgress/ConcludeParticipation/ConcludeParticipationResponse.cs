namespace Elsasa.Sis.Academics.Features.LearningProgress.ConcludeParticipation;

/// <summary>
/// Response payload for participation conclusion (P2-S04, UC-ACD-12).
/// </summary>
/// <param name="ParticipationId">The unique identifier of the academic participation record.</param>
/// <param name="ProgressId">The unique identifier of the linked learning progress record.</param>
/// <param name="StudentId">Target student identifier.</param>
/// <param name="LearningUnitId">Target learning unit identifier.</param>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="ParticipationStatus">Updated participation status ('completed').</param>
/// <param name="ProgressStatus">Updated progress status ('completed').</param>
/// <param name="CompletionDate">Recorded completion date.</param>
/// <param name="Notes">Conclusion notes or explanatory comments.</param>
/// <param name="ConcludedAt">Conclusion timestamp.</param>
/// <param name="ConcludedBy">User identity who concluded participation.</param>
public sealed record ConcludeParticipationResponse(
    Guid ParticipationId,
    Guid ProgressId,
    Guid StudentId,
    Guid LearningUnitId,
    Guid AcademicPeriodId,
    string ParticipationStatus,
    string ProgressStatus,
    DateOnly CompletionDate,
    string? Notes,
    DateTimeOffset ConcludedAt,
    string ConcludedBy);
