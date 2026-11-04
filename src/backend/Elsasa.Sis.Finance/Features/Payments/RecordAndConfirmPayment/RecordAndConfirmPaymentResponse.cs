namespace Elsasa.Sis.Finance.Features.Payments.RecordAndConfirmPayment;

/// <summary>
/// Response payload emitted after counter payment recording and atomic sequential receipt issuance.
/// </summary>
public sealed record RecordAndConfirmPaymentResponse(
    Guid PaymentId,
    string PaymentNumber,
    string ReceiptNumber,
    string ConfirmationStatus,
    long Amount,
    string PaymentMethod,
    DateOnly PaymentDate,
    DateTimeOffset ConfirmationDate,
    string ConfirmedBy,
    Guid PayerId,
    Guid StudentAccountId,
    Guid? StudentId,
    Guid? ApplicantId);
