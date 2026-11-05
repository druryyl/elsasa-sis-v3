namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Strongly-typed contract DTO representing a completed learning participation for a student.
/// </summary>
/// <param name="ParticipationId">The unique identifier of the academic participation record.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="LearningUnitId">The unique identifier of the completed learning unit.</param>
/// <param name="LearningUnitCode">The institutional code of the completed learning unit.</param>
/// <param name="LearningUnitName">The display name of the completed learning unit.</param>
/// <param name="CreditWeight">The credit or weight value awarded, or null if unweighted.</param>
/// <param name="CompletionDate">The date on which participation in the unit was concluded or completed.</param>
/// <param name="ParticipationType">The type of participation (standard, repeat, transferred_in, recognized_prior).</param>
public sealed record CompletedParticipationDto(
    Guid ParticipationId,
    Guid StudentId,
    Guid LearningUnitId,
    string LearningUnitCode,
    string LearningUnitName,
    decimal? CreditWeight,
    DateOnly CompletionDate,
    string ParticipationType);
