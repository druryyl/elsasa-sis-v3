namespace Elsasa.Sis.Finance.Features.Policies.CreateChargeRule;

/// <summary>
/// Response DTO confirming creation and registration of a charge rule.
/// </summary>
public sealed record CreateChargeRuleResponse(
    Guid Id,
    string RuleCode,
    Guid PolicyId,
    string Name,
    string ChargeType,
    bool IsMandatory,
    string DeterminationMethod,
    long BaseAmount,
    string CurrencyCode,
    string RecurrencePattern,
    string DueRuleType,
    string DueRuleValue,
    string? ApplicableGrade,
    string? ApplicableProgram,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
