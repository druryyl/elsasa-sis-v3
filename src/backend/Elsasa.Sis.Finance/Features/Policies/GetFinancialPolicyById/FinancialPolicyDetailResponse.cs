namespace Elsasa.Sis.Finance.Features.Policies.GetFinancialPolicyById;

/// <summary>
/// Detailed DTO representing a financial policy aggregate dossier including all associated charge rules.
/// </summary>
public sealed record FinancialPolicyDetailResponse(
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
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy,
    IReadOnlyList<ChargeRuleDetailDto> Rules);

/// <summary>
/// Detailed charge rule item within a policy dossier.
/// </summary>
public sealed record ChargeRuleDetailDto(
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
    string? CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
