namespace Elsasa.Sis.Finance.Features.Policies.CreateFinancialPolicy;

/// <summary>
/// Request payload to establish a new financial policy draft (GAP-006, TD-06, ADR-004).
/// </summary>
public sealed record CreateFinancialPolicyRequest(
    string PolicyCode,
    string Name,
    string Category,
    string BusinessPurpose,
    string? ApplicableContext,
    DateOnly EffectiveDate,
    DateOnly? EndDate = null,
    string? PolicyVersion = "1.0",
    Guid? SupersedesPolicyId = null,
    string? Notes = null);
