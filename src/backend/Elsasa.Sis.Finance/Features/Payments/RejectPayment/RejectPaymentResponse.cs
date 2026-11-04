namespace Elsasa.Sis.Finance.Features.Payments.RejectPayment;

/// <summary>
/// Response payload returned after rejecting a payment submission in the verification queue.
/// </summary>
public sealed record RejectPaymentResponse(
    Guid PaymentId,
    string PaymentNumber,
    string ConfirmationStatus,
    string RejectionReason,
    DateTimeOffset UpdatedAt,
    string UpdatedBy);
