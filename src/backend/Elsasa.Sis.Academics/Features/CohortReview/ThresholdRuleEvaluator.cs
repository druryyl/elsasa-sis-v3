namespace Elsasa.Sis.Academics.Features.CohortReview;

using System.Text.RegularExpressions;

/// <summary>
/// Domain rule evaluator executing the Completion &amp; Passing Threshold Rule for academic standing and progression (TD-03, UC-ACD-17, P3-S06).
/// Evaluates on-demand standing classifications (good_standing, conditional_standing, not_in_good_standing)
/// and progression recommendations (promotion_eligible, conditional_promotion, retention_recommended).
/// </summary>
public sealed class ThresholdRuleEvaluator
{
    public const string DefaultPolicyReference = "SK-AKAD/2026/POLICY-01";

    public const string GoodStanding = "good_standing";
    public const string ConditionalStanding = "conditional_standing";
    public const string NotInGoodStanding = "not_in_good_standing";

    public const string PromotionEligible = "promotion_eligible";
    public const string ConditionalPromotion = "conditional_promotion";
    public const string RetentionRecommended = "retention_recommended";

    public const decimal AttendanceThresholdPercentage = 75.0m;

    /// <summary>
    /// Computes the proposed academic standing based on learning unit passing counts and attendance presence rate.
    /// </summary>
    /// <param name="totalUnits">Total learning units evaluated.</param>
    /// <param name="completedUnits">Total learning units completed/passed.</param>
    /// <param name="failedUnitNames">Collection of unit names that are below passing threshold or require remediation.</param>
    /// <param name="attendanceRate">Student attendance presence rate in percent (0 - 100).</param>
    /// <returns>Proposed standing and optional pre-populated condition requirements text.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Evaluator service designed for instance dependency injection")]
    public (string ProposedStanding, string? Conditions) EvaluateStanding(
        int totalUnits,
        int completedUnits,
        IReadOnlyList<string> failedUnitNames,
        decimal attendanceRate)
    {
        ArgumentNullException.ThrowIfNull(failedUnitNames);

        var failedCount = failedUnitNames.Count;
        var hasChronicNonAttendance = attendanceRate < AttendanceThresholdPercentage;

        // Not in good standing: >2 units failed or chronic non-attendance (<75%)
        if (failedCount > 2 || hasChronicNonAttendance)
        {
            return (NotInGoodStanding, null);
        }

        // Conditional standing: 1-2 units below passing threshold or requiring remediation
        if (failedCount >= 1 && failedCount <= 2)
        {
            var conditions = GenerateConditionText(failedUnitNames);
            return (ConditionalStanding, conditions);
        }

        // Good standing: All units passed, no unaddressed failing results, attendance >= 75%
        return (GoodStanding, null);
    }

    /// <summary>
    /// Computes the proposed progression recommendation based on standing, grade level, and evidence metrics.
    /// </summary>
    /// <param name="standing">The active or proposed academic standing.</param>
    /// <param name="fromLevel">The student's current grade level.</param>
    /// <param name="totalUnits">Total units evaluated.</param>
    /// <param name="completedUnits">Units successfully passed/completed.</param>
    /// <param name="failedUnitNames">Names of failing learning units.</param>
    /// <param name="attendanceRate">Attendance presence rate percentage.</param>
    /// <param name="conditions">Condition requirements text attached to standing (if any).</param>
    /// <returns>Progression recommendation, recommended destination level, conditions, and evidence summary.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Evaluator service designed for instance dependency injection")]
    public (string ProposedRecommendation, string? RecommendedToLevel, string? Conditions, string SupportingEvidence) EvaluateProgression(
        string standing,
        string fromLevel,
        int totalUnits,
        int completedUnits,
        IReadOnlyList<string> failedUnitNames,
        decimal attendanceRate,
        string? conditions = null)
    {
        ArgumentNullException.ThrowIfNull(failedUnitNames);

        var normalizedStanding = standing?.Trim().ToLowerInvariant() ?? NotInGoodStanding;
        var nextLevel = CalculateNextGradeLevel(fromLevel);
        var effectiveConditions = conditions ?? GenerateConditionText(failedUnitNames);

        switch (normalizedStanding)
        {
            case GoodStanding:
            {
                var evidence = $"Good Standing; {completedUnits}/{totalUnits} units completed; {attendanceRate:0.00}% attendance";
                return (PromotionEligible, nextLevel, null, evidence);
            }

            case ConditionalStanding:
            {
                var unitsSummary = failedUnitNames.Count > 0
                    ? $"{failedUnitNames.Count} unit(s) below threshold ({string.Join(", ", failedUnitNames)})"
                    : "Remediation requirements active";
                var evidence = $"Conditional Standing; {unitsSummary}; {attendanceRate:0.00}% attendance";
                return (ConditionalPromotion, nextLevel, effectiveConditions, evidence);
            }

            case NotInGoodStanding:
            default:
            {
                string evidence;
                if (attendanceRate < AttendanceThresholdPercentage)
                {
                    evidence = $"Not In Good Standing; Chronic non-attendance ({attendanceRate:0.00}% < 75%); {completedUnits}/{totalUnits} units completed";
                }
                else
                {
                    var unitsSummary = failedUnitNames.Count > 0
                        ? $"{failedUnitNames.Count} unit(s) failed ({string.Join(", ", failedUnitNames)})"
                        : "Academic requirements unfulfilled";
                    evidence = $"Not In Good Standing; {unitsSummary}; {attendanceRate:0.00}% attendance";
                }

                // Recommended destination is null or current grade level for retention
                return (RetentionRecommended, null, null, evidence);
            }
        }
    }

    /// <summary>
    /// Generates standard conditional remediation requirements text based on failing unit titles.
    /// E.g.: "Must complete Mathematics remediation during holiday packet".
    /// </summary>
    public static string? GenerateConditionText(IReadOnlyList<string> failedUnitNames)
    {
        if (failedUnitNames == null || failedUnitNames.Count == 0)
        {
            return null;
        }

        if (failedUnitNames.Count == 1)
        {
            return $"Must complete {failedUnitNames[0]} remediation during holiday packet";
        }

        if (failedUnitNames.Count == 2)
        {
            return $"Must complete {failedUnitNames[0]} and {failedUnitNames[1]} remediation during holiday packet";
        }

        return $"Must complete remediation for {string.Join(", ", failedUnitNames)} during holiday packet";
    }

    /// <summary>
    /// Calculates the next sequential grade level given a current grade level string.
    /// Supports "Grade X", "Kelas X", numeric "X", and sets "Graduated" for Grade 12 or above.
    /// </summary>
    public static string? CalculateNextGradeLevel(string? fromLevel)
    {
        if (string.IsNullOrWhiteSpace(fromLevel))
        {
            return null;
        }

        var trimmed = fromLevel.Trim();
        var match = Regex.Match(trimmed, @"\d+");
        if (!match.Success)
        {
            return trimmed;
        }

        if (int.TryParse(match.Value, out var gradeNum))
        {
            if (gradeNum >= 12)
            {
                return "Graduated";
            }

            var nextNum = gradeNum + 1;
            return trimmed.Remove(match.Index, match.Length).Insert(match.Index, nextNum.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return trimmed;
    }
}
