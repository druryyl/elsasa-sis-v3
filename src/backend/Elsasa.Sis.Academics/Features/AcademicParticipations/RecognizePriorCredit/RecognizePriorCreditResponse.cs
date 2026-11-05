namespace Elsasa.Sis.Academics.Features.AcademicParticipations.RecognizePriorCredit;

/// <summary>
/// Response payload for recognized transfer or prior learning credit (P2-S05).
/// </summary>
/// <param name="Id">The unique identifier of the created academic participation record.</param>
/// <param name="StudentId">Target student identifier.</param>
/// <param name="LearningUnitId">Target learning unit identifier.</param>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="AcademicPeriodLabel">Human-readable label of the academic period.</param>
/// <param name="ParticipationType">Participation type (transferred_in or recognized_prior).</param>
/// <param name="AttemptNumber">Attempt counter (always 1 for recognized credit).</param>
/// <param name="Status">Participation status (completed).</param>
/// <param name="StartDate">Start date of the credited study.</param>
/// <param name="EndDate">End date of the credited study.</param>
/// <param name="CompletionDate">Date completion occurred.</param>
/// <param name="SourceInstitution">Source institution name.</param>
/// <param name="ExternalGradeOrCredit">External grade or credit score.</param>
/// <param name="RecognitionBasis">Documentation or justification basis.</param>
/// <param name="ApprovedBy">Academic authority identity who approved recognition.</param>
/// <param name="StudentLearningPlanId">Optional student learning plan reference.</param>
/// <param name="Notes">Administrative notes.</param>
/// <param name="CreatedAt">Timestamp when created in UTC.</param>
/// <param name="CreatedBy">Operator identity who recorded entry.</param>
public sealed record RecognizePriorCreditResponse(
    Guid Id,
    Guid StudentId,
    Guid LearningUnitId,
    Guid AcademicPeriodId,
    string AcademicPeriodLabel,
    string ParticipationType,
    int AttemptNumber,
    string Status,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly CompletionDate,
    string? SourceInstitution,
    string? ExternalGradeOrCredit,
    string? RecognitionBasis,
    string ApprovedBy,
    Guid? StudentLearningPlanId,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy);
