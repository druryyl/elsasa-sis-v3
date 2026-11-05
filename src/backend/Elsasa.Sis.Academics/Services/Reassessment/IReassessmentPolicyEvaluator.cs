namespace Elsasa.Sis.Academics.Services.Reassessment;

/// <summary>
/// Represents an attempt record scored for an assessment, used as input for reassessment policy adjudication (GAP-004, TD-03).
/// </summary>
/// <param name="Id">The unique identifier of the academic result record.</param>
/// <param name="AttemptNumber">The sequence number of the attempt (1 for initial attempt, 2+ for retakes).</param>
/// <param name="ScoreNumeric">The numeric score achieved in this attempt, if applicable.</param>
/// <param name="GradeLetter">The letter grade achieved in this attempt, if applicable.</param>
/// <param name="GradeLabel">The competency or narrative label achieved in this attempt, if applicable.</param>
/// <param name="EvaluatedDate">The date when this attempt was evaluated.</param>
public record AttemptScoreRecord(
    Guid Id,
    int AttemptNumber,
    decimal? ScoreNumeric,
    string? GradeLetter,
    string? GradeLabel,
    DateTime EvaluatedDate);

/// <summary>
/// Represents the result of adjudicating an operative outcome across multiple assessment attempts.
/// </summary>
/// <param name="OperativeResultId">The ID of the single attempt record adjudicated as operative (is_final = true).</param>
/// <param name="OperativeScoreNumeric">The final effective numeric score calculated for the operative attempt.</param>
/// <param name="OperativeGradeLetter">The final effective letter grade for the operative attempt.</param>
/// <param name="OperativeGradeLabel">The final effective grade label for the operative attempt.</param>
/// <param name="NonOperativeResultIds">The IDs of all prior or subsequent attempts adjudicated as non-operative (is_final = false).</param>
public record ReassessmentEvaluationResult(
    Guid OperativeResultId,
    decimal? OperativeScoreNumeric,
    string? OperativeGradeLetter,
    string? OperativeGradeLabel,
    IReadOnlyList<Guid> NonOperativeResultIds);

/// <summary>
/// Pure domain service adjudicating the operative result across multiple retake attempts
/// according to assessment reassessment policy (GAP-004, TD-03).
/// </summary>
public interface IReassessmentPolicyEvaluator
{
    /// <summary>
    /// Evaluates existing attempts alongside a new candidate attempt, returning the single operative attempt
    /// and operative score values according to the configured reassessment policy.
    /// </summary>
    /// <param name="reassessmentPolicy">The policy string ('highest_score', 'capped_at_passing', 'latest_attempt', 'average', 'none').</param>
    /// <param name="passingScore">The passing threshold (KKM) configured on the assessment, if any.</param>
    /// <param name="existingAttempts">The collection of previously evaluated attempts for the student and assessment.</param>
    /// <param name="newAttempt">The candidate attempt being recorded.</param>
    /// <returns>The evaluation result indicating which attempt is operative and its effective values.</returns>
    ReassessmentEvaluationResult Evaluate(
        string reassessmentPolicy,
        decimal? passingScore,
        IReadOnlyList<AttemptScoreRecord> existingAttempts,
        AttemptScoreRecord newAttempt);
}
