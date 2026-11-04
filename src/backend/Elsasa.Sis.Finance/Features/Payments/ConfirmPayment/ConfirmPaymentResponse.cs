namespace Elsasa.Sis.Finance.Features.Payments.ConfirmPayment;

/// <summary>
/// Response payload returned after confirming a submitted payment in the verification queue.
/// </summary>
public sealed record ConfirmPaymentResponse(
    Guid PaymentId,
    string PaymentNumber,
    string ReceiptNumber,
    string ConfirmationStatus,
    long Amount,
    DateTimeOffset ConfirmationDate,
    string ConfirmedBy,
    Guid? StudentId,
    Guid? ApplicantId);
