namespace Elsasa.Sis.Finance.Features.Payers.CreatePayer;

/// <summary>
/// Response DTO produced after successfully registering a payer identity.
/// </summary>
/// <param name="Id">The unique identifier of the created payer.</param>
/// <param name="PayerNumber">Canonical sequential payer code (e.g. PYR-2026-0001).</param>
/// <param name="PayerName">Full legal name or corporate moniker.</param>
/// <param name="PayerType">Payer classification (parent_guardian, sponsor, embassy, foundation, etc.).</param>
public sealed record CreatePayerResponse(
    Guid Id,
    string PayerNumber,
    string PayerName,
    string PayerType);
