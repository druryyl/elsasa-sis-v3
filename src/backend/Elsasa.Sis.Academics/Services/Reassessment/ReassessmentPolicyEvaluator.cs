namespace Elsasa.Sis.Academics.Services.Reassessment;

/// <summary>
/// Domain policy literal constants for assessment retake adjudication.
/// </summary>
public static class ReassessmentPolicies
{
    public const string HighestScore = "highest_score";
    public const string CappedAtPassing = "capped_at_passing";
    public const string LatestAttempt = "latest_attempt";
    public const string Average = "average";
    public const string None = "none";
}

/// <summary>
/// Pure C# domain service adjudicating the single operative attempt across multiple retake attempts
/// according to assessment reassessment policy (GAP-004, TD-03).
/// </summary>
public class ReassessmentPolicyEvaluator : IReassessmentPolicyEvaluator
{
    /// <inheritdoc />
    public ReassessmentEvaluationResult Evaluate(
        string reassessmentPolicy,
        decimal? passingScore,
        IReadOnlyList<AttemptScoreRecord> existingAttempts,
        AttemptScoreRecord newAttempt)
    {
        ArgumentNullException.ThrowIfNull(newAttempt);

        // Assemble unified distinct attempt list
        var allAttempts = new List<AttemptScoreRecord>();
        if (existingAttempts != null)
        {
            foreach (var attempt in existingAttempts)
            {
                if (attempt != null && attempt.Id != newAttempt.Id)
                {
                    allAttempts.Add(attempt);
                }
            }
        }
        allAttempts.Add(newAttempt);

        string policy = reassessmentPolicy?.Trim().ToLowerInvariant() ?? ReassessmentPolicies.LatestAttempt;

        return policy switch
        {
            ReassessmentPolicies.HighestScore => EvaluateHighestScore(allAttempts),
            ReassessmentPolicies.CappedAtPassing => EvaluateCappedAtPassing(allAttempts, passingScore),
            ReassessmentPolicies.LatestAttempt => EvaluateLatestAttempt(allAttempts),
            ReassessmentPolicies.Average => EvaluateAverage(allAttempts, newAttempt),
            ReassessmentPolicies.None => EvaluateNone(allAttempts),
            _ => EvaluateLatestAttempt(allAttempts)
        };
    }

    private static ReassessmentEvaluationResult EvaluateHighestScore(List<AttemptScoreRecord> allAttempts)
    {
        var scoredAttempts = allAttempts.Where(a => a.ScoreNumeric.HasValue).ToList();
        if (scoredAttempts.Count == 0)
        {
            // Non-numeric evaluation models fall back to latest attempt
            return EvaluateLatestAttempt(allAttempts);
        }

        // Operative record is the attempt with maximum ScoreNumeric. If tie, latest attempt wins.
        var winner = scoredAttempts
            .OrderByDescending(a => a.ScoreNumeric!.Value)
            .ThenByDescending(a => a.AttemptNumber)
            .ThenByDescending(a => a.EvaluatedDate)
            .First();

        return new ReassessmentEvaluationResult(
            winner.Id,
            winner.ScoreNumeric,
            winner.GradeLetter,
            winner.GradeLabel,
            GetNonOperativeIds(allAttempts, winner.Id));
    }

    private static ReassessmentEvaluationResult EvaluateCappedAtPassing(
        List<AttemptScoreRecord> allAttempts,
        decimal? passingScore)
    {
        var scoredAttempts = allAttempts.Where(a => a.ScoreNumeric.HasValue).ToList();
        if (scoredAttempts.Count == 0)
        {
            // Non-numeric evaluation models fall back to latest attempt
            return EvaluateLatestAttempt(allAttempts);
        }

        // Operative record is the attempt with maximum score, but if score exceeds passingScore, capped at passingScore.
        var winner = scoredAttempts
            .OrderByDescending(a => a.ScoreNumeric!.Value)
            .ThenByDescending(a => a.AttemptNumber)
            .ThenByDescending(a => a.EvaluatedDate)
            .First();

        decimal? operativeScore = winner.ScoreNumeric;
        if (passingScore.HasValue && operativeScore.HasValue && operativeScore.Value > passingScore.Value)
        {
            operativeScore = passingScore.Value;
        }

        return new ReassessmentEvaluationResult(
            winner.Id,
            operativeScore,
            winner.GradeLetter,
            winner.GradeLabel,
            GetNonOperativeIds(allAttempts, winner.Id));
    }

    private static ReassessmentEvaluationResult EvaluateLatestAttempt(List<AttemptScoreRecord> allAttempts)
    {
        // Operative record is the attempt with maximum AttemptNumber.
        var winner = allAttempts
            .OrderByDescending(a => a.AttemptNumber)
            .ThenByDescending(a => a.EvaluatedDate)
            .First();

        return new ReassessmentEvaluationResult(
            winner.Id,
            winner.ScoreNumeric,
            winner.GradeLetter,
            winner.GradeLabel,
            GetNonOperativeIds(allAttempts, winner.Id));
    }

    private static ReassessmentEvaluationResult EvaluateAverage(
        List<AttemptScoreRecord> allAttempts,
        AttemptScoreRecord newAttempt)
    {
        // Arithmetic mean of all attempts is computed and assigned to new attempt; new attempt is operative.
        var scoredAttempts = allAttempts.Where(a => a.ScoreNumeric.HasValue).ToList();
        decimal? averageScore;

        if (scoredAttempts.Count > 0)
        {
            decimal mean = scoredAttempts.Average(a => a.ScoreNumeric!.Value);
            averageScore = Math.Round(mean, 2, MidpointRounding.AwayFromZero);
        }
        else
        {
            averageScore = newAttempt.ScoreNumeric;
        }

        return new ReassessmentEvaluationResult(
            newAttempt.Id,
            averageScore,
            newAttempt.GradeLetter,
            newAttempt.GradeLabel,
            GetNonOperativeIds(allAttempts, newAttempt.Id));
    }

    private static ReassessmentEvaluationResult EvaluateNone(List<AttemptScoreRecord> allAttempts)
    {
        // Attempt 1 is always operative; retakes are recorded but flagged is_final = false.
        var attempt1 = allAttempts.FirstOrDefault(a => a.AttemptNumber == 1)
                       ?? allAttempts.OrderBy(a => a.AttemptNumber).ThenBy(a => a.EvaluatedDate).First();

        return new ReassessmentEvaluationResult(
            attempt1.Id,
            attempt1.ScoreNumeric,
            attempt1.GradeLetter,
            attempt1.GradeLabel,
            GetNonOperativeIds(allAttempts, attempt1.Id));
    }

    private static List<Guid> GetNonOperativeIds(List<AttemptScoreRecord> allAttempts, Guid operativeId)
    {
        return allAttempts
            .Where(a => a.Id != operativeId)
            .Select(a => a.Id)
            .Distinct()
            .ToList();
    }
}
