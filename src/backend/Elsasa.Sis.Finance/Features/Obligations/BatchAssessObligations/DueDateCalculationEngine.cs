namespace Elsasa.Sis.Finance.Features.Obligations.BatchAssessObligations;

using System.Globalization;

/// <summary>
/// Rule-driven due date derivation engine (FIN-WF-02, TD-04, GAP-004).
/// Computes obligation due dates based on configured charge rule parameters:
/// - days_after_issuance: issue_date.AddDays(int.Parse(due_rule_value))
/// - fixed_day_of_month: Computes specified day in the period's month, clamped to the last day of the month.
/// - fixed_calendar_date: Parses ISO-8601 calendar date.
/// </summary>
public static class DueDateCalculationEngine
{
    public const string DaysAfterIssuance = "days_after_issuance";
    public const string FixedDayOfMonth = "fixed_day_of_month";
    public const string FixedCalendarDate = "fixed_calendar_date";

    /// <summary>
    /// Calculates the effective due date for an obligation.
    /// </summary>
    /// <param name="dueRuleType">The rule strategy (days_after_issuance, fixed_day_of_month, fixed_calendar_date).</param>
    /// <param name="dueRuleValue">The parameter value (e.g., '14', '10', '2026-08-31').</param>
    /// <param name="issueDate">The assessment issuance date.</param>
    /// <param name="periodReferenceDate">Optional reference date from the governing academic/operational period.</param>
    /// <returns>The computed calendar due date.</returns>
    public static DateOnly CalculateDueDate(
        string dueRuleType,
        string dueRuleValue,
        DateOnly issueDate,
        DateOnly? periodReferenceDate = null)
    {
        if (string.IsNullOrWhiteSpace(dueRuleType))
        {
            return issueDate.AddDays(14);
        }

        var normalizedType = dueRuleType.Trim().ToLowerInvariant();

        switch (normalizedType)
        {
            case DaysAfterIssuance:
            {
                if (int.TryParse(dueRuleValue?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var days))
                {
                    return issueDate.AddDays(days);
                }

                return issueDate.AddDays(14);
            }

            case FixedDayOfMonth:
            {
                var refDate = periodReferenceDate ?? issueDate;
                var targetYear = refDate.Year;
                var targetMonth = refDate.Month;
                var daysInMonth = DateTime.DaysInMonth(targetYear, targetMonth);

                var day = int.TryParse(dueRuleValue?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedDay)
                    ? parsedDay
                    : 10;

                var clampedDay = Math.Clamp(day, 1, daysInMonth);
                return new DateOnly(targetYear, targetMonth, clampedDay);
            }

            case FixedCalendarDate:
            {
                if (DateOnly.TryParse(dueRuleValue?.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    return date;
                }

                if (DateTime.TryParse(dueRuleValue?.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
                {
                    return DateOnly.FromDateTime(dateTime);
                }

                return issueDate.AddDays(30);
            }

            default:
                return issueDate.AddDays(14);
        }
    }
}
