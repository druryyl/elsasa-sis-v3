namespace Elsasa.Sis.Finance.Features.Policies.CreateFinancialPolicy;

/// <summary>
/// Response DTO produced after successfully drafting a financial policy.
/// </summary>
public sealed record CreateFinancialPolicyResponse(
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
    Guid? SupersedesPolicyId,
    string? Notes,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
