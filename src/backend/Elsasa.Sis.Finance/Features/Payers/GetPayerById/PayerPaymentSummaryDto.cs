namespace Elsasa.Sis.Finance.Features.Payers.GetPayerById;

/// <summary>
/// Summary DTO of a payment record linked to a specific payer.
/// </summary>
/// <param name="Id">The unique identifier of the payment.</param>
/// <param name="PaymentNumber">Sequential business reference (e.g. PAY-2026-000001).</param>
/// <param name="StudentAccountId">Unique identifier of the target student financial account.</param>
/// <param name="StudentId">Optional enrolled student identifier.</param>
/// <param name="ApplicantId">Optional pre-enrollment admission applicant identifier.</param>
/// <param name="PaymentAmount">Gross payment value received in IDR.</param>
/// <param name="CurrencyCode">Currency code (typically 'IDR').</param>
/// <param name="PaymentMethod">Payment method used (e.g. 'bank_transfer', 'cash', 'pos_card').</param>
/// <param name="ExternalReference">Optional bank transaction reference or deposit slip number.</param>
/// <param name="PaymentDate">Effective date of the financial transaction.</param>
/// <param name="ConfirmationStatus">Status in verification queue ('submitted', 'confirmed', 'rejected', 'reversed').</param>
/// <param name="ReceiptNumber">Optional official sequential receipt number (e.g. REC-2026-000001) if confirmed.</param>
/// <param name="SubmissionDate">Timestamp when this payment record was submitted.</param>
public sealed record PayerPaymentSummaryDto(
    Guid Id,
    string PaymentNumber,
    Guid StudentAccountId,
    Guid? StudentId,
    Guid? ApplicantId,
    long PaymentAmount,
    string CurrencyCode,
    string PaymentMethod,
    string? ExternalReference,
    DateOnly PaymentDate,
    string ConfirmationStatus,
    string? ReceiptNumber,
    DateTimeOffset SubmissionDate);
