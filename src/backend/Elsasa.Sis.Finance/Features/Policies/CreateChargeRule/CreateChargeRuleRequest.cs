namespace Elsasa.Sis.Finance.Features.Policies.CreateChargeRule;

/// <summary>
/// Request payload to establish a concrete charge rule anchored to a governing financial policy (GAP-006, TD-06).
/// </summary>
public sealed record CreateChargeRuleRequest(
    string RuleCode,
    string Name,
    string ChargeType,
    long BaseAmount,
    string CurrencyCode = "IDR",
    string RecurrencePattern = "one_time",
    string DueRuleType = "days_after_issuance",
    string DueRuleValue = "14",
    bool IsMandatory = true,
    string DeterminationMethod = "fixed_amount",
    string? ApplicableGrade = null,
    string? ApplicableProgram = null,
    string? Notes = null);
