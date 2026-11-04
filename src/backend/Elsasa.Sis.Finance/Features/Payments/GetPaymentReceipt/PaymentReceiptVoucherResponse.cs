namespace Elsasa.Sis.Finance.Features.Payments.GetPaymentReceipt;

/// <summary>
/// Official school letterhead and branding details on receipt vouchers.
/// </summary>
public sealed record SchoolHeaderDto(
    string SchoolName,
    string SchoolAddress,
    string ContactInfo,
    string DocumentTitle);

/// <summary>
/// Payer details on receipt voucher.
/// </summary>
public sealed record ReceiptPayerDto(
    Guid Id,
    string PayerNumber,
    string PayerName,
    string? IdentificationNumber,
    string? Email,
    string? PhoneNumber);

/// <summary>
/// Student / applicant beneficiary details on receipt voucher.
/// </summary>
public sealed record ReceiptCandidateDto(
    Guid? StudentId,
    string? StudentNumber,
    string? StudentFullName,
    Guid? ApplicantId,
    string AccountNumber);

/// <summary>
/// Line-item allocation distribution on receipt voucher.
/// </summary>
public sealed record ReceiptAllocationLineDto(
    string AllocationNumber,
    string ObligationNumber,
    string ChargeType,
    string Description,
    long Amount);

/// <summary>
/// Cashier counter verification and authorization signature metadata.
/// </summary>
public sealed record CashierSignatureDto(
    string ConfirmedBy,
    DateTimeOffset ConfirmationDate,
    string Title,
    string OfficialStampText);

/// <summary>
/// Complete structured receipt voucher projection suitable for counter printout or PDF rendering (SCR-FIN-02, SCR-SELF-02, TD-04, OQ-004).
/// </summary>
public sealed record PaymentReceiptVoucherResponse(
    SchoolHeaderDto Header,
    Guid PaymentId,
    string PaymentNumber,
    string ReceiptNumber,
    DateOnly PaymentDate,
    DateTimeOffset ReceiptIssuedAt,
    string PaymentMethod,
    string? ExternalReference,
    long Amount,
    string CurrencyCode,
    string AmountInWords,
    ReceiptPayerDto Payer,
    ReceiptCandidateDto Candidate,
    IReadOnlyList<ReceiptAllocationLineDto> Allocations,
    CashierSignatureDto Cashier,
    string? Notes);
