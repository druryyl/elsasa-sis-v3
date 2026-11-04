namespace Elsasa.Sis.Finance.Features.Policies.ApproveFinancialPolicy;

/// <summary>
/// Request payload to formally approve a financial policy draft.
/// </summary>
public sealed record ApproveFinancialPolicyRequest(
    string? ApprovingAuthority = null,
    string? Notes = null);
