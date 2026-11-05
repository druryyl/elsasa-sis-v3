namespace Elsasa.Sis.Academics.Features.AcademicResults;

/// <summary>
/// Data transfer object representing a published academic result for the student/family self-service workspace (SCR-SELF-03, P4-S10).
/// </summary>
/// <param name="ResultId">The unique identifier of the academic result.</param>
/// <param name="AssessmentId">The unique identifier of the assessment.</param>
/// <param name="AssessmentTitle">The title of the assessment.</param>
/// <param name="AssessmentType">The assessment classification ('formative', 'summative', 'diagnostic', 'portfolio', 'project', 'other').</param>
/// <param name="LearningUnitId">The unique identifier of the owning learning unit.</param>
/// <param name="LearningUnitCode">The code of the learning unit.</param>
/// <param name="LearningUnitTitle">The title or name of the learning unit.</param>
/// <param name="AttemptNumber">The attempt sequence number (1 for initial attempt, 2+ for retakes).</param>
/// <param name="ScoreNumeric">The evaluated numeric score, if applicable.</param>
/// <param name="ScoreMax">The maximum possible score configured for this evaluation.</param>
/// <param name="GradeLetter">The evaluated letter grade (e.g. 'A', 'B+'), if applicable.</param>
/// <param name="GradeLabel">The evaluated descriptor label (e.g. 'Kompeten', 'Sangat Mahir'), if applicable.</param>
/// <param name="NarrativeFeedback">The qualitative formative feedback or remarks.</param>
/// <param name="EvaluationModel">The evaluation model used ('numeric', 'letter_grade', 'pass_fail', 'competency', 'rubric', 'narrative', 'completion').</param>
/// <param name="EvaluatedDate">The date when evaluation occurred.</param>
/// <param name="PublishedDate">The date when the result was officially published to self-service portals.</param>
public sealed record StudentPublishedResultDto(
    Guid ResultId,
    Guid AssessmentId,
    string AssessmentTitle,
    string AssessmentType,
    Guid LearningUnitId,
    string LearningUnitCode,
    string LearningUnitTitle,
    int AttemptNumber,
    decimal? ScoreNumeric,
    decimal? ScoreMax,
    string? GradeLetter,
    string? GradeLabel,
    string? NarrativeFeedback,
    string EvaluationModel,
    DateOnly EvaluatedDate,
    DateOnly? PublishedDate);
