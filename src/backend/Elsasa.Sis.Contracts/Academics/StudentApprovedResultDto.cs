namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Strongly-typed contract DTO representing an operative approved academic result for a student (ADR-001).
/// </summary>
/// <param name="ResultId">The unique identifier of the academic result.</param>
/// <param name="AssessmentId">The unique identifier of the assessment.</param>
/// <param name="AssessmentTitle">The title of the assessment.</param>
/// <param name="LearningUnitId">The unique identifier of the owning learning unit.</param>
/// <param name="LearningUnitTitle">The title or name of the learning unit.</param>
/// <param name="ScoreNumeric">The numeric score achieved, or null if non-numeric.</param>
/// <param name="ScoreMax">The maximum possible score for the assessment.</param>
/// <param name="GradeLetter">The letter grade achieved, or null if unassigned.</param>
/// <param name="GradeLabel">Descriptive grade label or qualitative descriptor.</param>
/// <param name="NarrativeFeedback">Teacher qualitative evaluation remarks.</param>
/// <param name="EvaluatedDate">The date and time when the result was evaluated.</param>
public sealed record StudentApprovedResultDto(
    Guid ResultId,
    Guid AssessmentId,
    string AssessmentTitle,
    Guid LearningUnitId,
    string LearningUnitTitle,
    decimal? ScoreNumeric,
    decimal? ScoreMax,
    string? GradeLetter,
    string? GradeLabel,
    string? NarrativeFeedback,
    DateTime EvaluatedDate);
