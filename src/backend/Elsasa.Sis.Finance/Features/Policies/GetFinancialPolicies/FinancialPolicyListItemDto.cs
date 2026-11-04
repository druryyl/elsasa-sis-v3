namespace Elsasa.Sis.Finance.Features.Policies.GetFinancialPolicies;

/// <summary>
/// Summary DTO representing a financial policy with its active charge rules.
/// </summary>
public sealed record FinancialPolicyListItemDto(
    Guid Id,
    string PolicyCode,
    string Name,
    string Category,
    string BusinessPurpose,
    string? ApplicableContext,
    DateOnly EffectiveDate,
    DateOnly? EndDate,
    string PolicyVersion,
    string Status,
    string? ApprovingAuthority,
    DateOnly? ApprovedDate,
    Guid? SupersedesPolicyId,
    string? Notes,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    IReadOnlyList<PolicyChargeRuleDto> Rules);

/// <summary>
/// Charge rule item embedded within the policy summary projection.
/// </summary>
public sealed record PolicyChargeRuleDto(
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
    string Status);
