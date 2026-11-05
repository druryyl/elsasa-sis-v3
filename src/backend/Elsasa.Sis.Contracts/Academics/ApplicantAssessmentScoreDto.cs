namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Strongly-typed contract DTO representing an applicant's assessment score for admission evaluation (ADR-001, GAP-07).
/// </summary>
/// <param name="ResultId">The unique identifier of the academic result.</param>
/// <param name="AssessmentId">The unique identifier of the assessment.</param>
/// <param name="AssessmentTitle">The title of the assessment.</param>
/// <param name="ApplicantId">The unique identifier of the applicant.</param>
/// <param name="ScoreNumeric">The numeric score achieved, or null if non-numeric.</param>
/// <param name="ScoreMax">The maximum possible score for the assessment.</param>
/// <param name="GradeLetter">The letter grade achieved, or null.</param>
/// <param name="GradeLabel">Descriptive grade label or qualitative descriptor.</param>
/// <param name="EvaluationModel">The evaluation model used ('numeric', 'letter_grade', 'rubric', etc.).</param>
/// <param name="ResultStatus">The lifecycle status of the result ('provisional', 'approved', 'published').</param>
/// <param name="EvaluatedBy">Username or identifier of the evaluator.</param>
/// <param name="EvaluatedDate">The date and time when the score was evaluated.</param>
public sealed record ApplicantAssessmentScoreDto(
    Guid ResultId,
    Guid AssessmentId,
    string AssessmentTitle,
    Guid ApplicantId,
    decimal? ScoreNumeric,
    decimal? ScoreMax,
    string? GradeLetter,
    string? GradeLabel,
    string EvaluationModel,
    string ResultStatus,
    string EvaluatedBy,
    DateTime EvaluatedDate);
