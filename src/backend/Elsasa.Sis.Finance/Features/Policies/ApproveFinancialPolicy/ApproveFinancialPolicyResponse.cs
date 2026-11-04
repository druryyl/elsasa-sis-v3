namespace Elsasa.Sis.Finance.Features.Policies.ApproveFinancialPolicy;

/// <summary>
/// Response DTO confirming formal policy approval and transition to active status.
/// </summary>
public sealed record ApproveFinancialPolicyResponse(
    Guid Id,
    string PolicyCode,
    string Name,
    string Status,
    string ApprovingAuthority,
    DateOnly ApprovedDate,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
