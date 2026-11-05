namespace Elsasa.Sis.Academics.Features.AcademicParticipations.RecognizePriorCredit;

/// <summary>
/// Request payload for recognizing transfer or prior learning credit (P2-S05, GAP-08, UC-ACD-11).
/// </summary>
/// <param name="StudentId">Target student identifier.</param>
/// <param name="LearningUnitId">Target learning unit identifier.</param>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="AcademicPeriodLabel">Human-readable label of the academic period.</param>
/// <param name="ParticipationType">Participation type: transferred_in or recognized_prior.</param>
/// <param name="SourceInstitution">Source institution name if transferred.</param>
/// <param name="ExternalGradeOrCredit">External grade, credit points, or score earned.</param>
/// <param name="RecognitionBasis">Basis, evaluation, or justification documentation.</param>
/// <param name="StartDate">Start date of the recognized study.</param>
/// <param name="CompletionDate">Date study or prior learning was completed.</param>
/// <param name="StudentLearningPlanId">Optional student learning plan identifier.</param>
/// <param name="Notes">Optional administrative notes.</param>
public sealed record RecognizePriorCreditRequest(
    Guid StudentId,
    Guid LearningUnitId,
    Guid AcademicPeriodId,
    string AcademicPeriodLabel,
    string ParticipationType,
    string? SourceInstitution = null,
    string? ExternalGradeOrCredit = null,
    string? RecognitionBasis = null,
    DateOnly StartDate = default,
    DateOnly CompletionDate = default,
    Guid? StudentLearningPlanId = null,
    string? Notes = null,
    string? ApprovedBy = null,
    DateOnly? ApprovalDate = null);
