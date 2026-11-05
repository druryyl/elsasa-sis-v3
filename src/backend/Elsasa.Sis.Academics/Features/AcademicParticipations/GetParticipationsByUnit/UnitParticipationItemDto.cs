namespace Elsasa.Sis.Academics.Features.AcademicParticipations.GetParticipationsByUnit;

/// <summary>
/// Projection DTO representing a student participation item within a learning unit roster (P2-S06, TD-08).
/// </summary>
/// <param name="Id">The unique identifier of the academic participation record.</param>
/// <param name="StudentId">The unique identifier of the participating student.</param>
/// <param name="StudentNumber">The student registration number, if available.</param>
/// <param name="StudentName">The full name of the participating student, if available.</param>
/// <param name="LearningUnitId">The unique identifier of the learning unit.</param>
/// <param name="AcademicPeriodId">The unique identifier of the academic period.</param>
/// <param name="AcademicPeriodLabel">The display label of the academic period.</param>
/// <param name="ParticipationType">The participation type (standard, repeat, transferred_in, recognized_prior).</param>
/// <param name="AttemptNumber">The attempt count for retakes/repeats (starts at 1).</param>
/// <param name="Source">The registration source (assigned, student_selected, learning_plan).</param>
/// <param name="SectionLabel">The cohort section label (e.g. '4A'), or null if unsectioned.</param>
/// <param name="Status">The lifecycle status (in_progress, paused, withdrawn, completed).</param>
/// <param name="StartDate">The start date of the participation.</param>
/// <param name="EndDate">The end date of the participation, if withdrawn or ended.</param>
/// <param name="CompletionDate">The date on which the unit participation was formally concluded.</param>
/// <param name="SourceInstitution">Originating institution for transfer or recognized credit.</param>
/// <param name="ExternalGradeOrCredit">External grade or credit awarded.</param>
/// <param name="RecognitionBasis">Justification notes for credit recognition.</param>
/// <param name="ApprovedBy">Username of authority who approved credit or conclusion.</param>
/// <param name="StudentLearningPlanId">Optional reference to student learning plan.</param>
/// <param name="Notes">Delivery or administrative notes.</param>
/// <param name="CreatedAt">Timestamp when the participation was created.</param>
/// <param name="CreatedBy">Username who registered the participation.</param>
/// <param name="UpdatedAt">Timestamp of last update, if any.</param>
/// <param name="UpdatedBy">Username who last modified the record, if any.</param>
public sealed record UnitParticipationItemDto(
    Guid Id,
    Guid StudentId,
    string? StudentNumber,
    string? StudentName,
    Guid LearningUnitId,
    Guid AcademicPeriodId,
    string AcademicPeriodLabel,
    string ParticipationType,
    int AttemptNumber,
    string Source,
    string? SectionLabel,
    string Status,
    DateOnly StartDate,
    DateOnly? EndDate,
    DateOnly? CompletionDate,
    string? SourceInstitution,
    string? ExternalGradeOrCredit,
    string? RecognitionBasis,
    string? ApprovedBy,
    Guid? StudentLearningPlanId,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
