namespace Elsasa.Sis.Finance.Features.Payments.RejectPayment;

/// <summary>
/// Request payload for cashier payment rejection (SCR-FIN-02, TD-04).
/// </summary>
public sealed record RejectPaymentRequest(
    string RejectionReason,
    string? Notes = null);
