namespace Elsasa.Sis.Finance.Features.Payers.GetPayerById;

/// <summary>
/// Detailed response container for single payer inspection including linked payments summary (SCR-FIN-02, TD-03).
/// </summary>
/// <param name="Id">The unique identifier of the payer.</param>
/// <param name="PayerNumber">Canonical sequential payer code (e.g. PYR-2026-0001).</param>
/// <param name="PayerName">Full legal name or corporate moniker.</param>
/// <param name="PayerType">Classification (parent_guardian, sponsor, embassy, foundation, etc.).</param>
/// <param name="IdentificationNumber">Optional NIK, NPWP, or entity registration code.</param>
/// <param name="Email">Optional contact email address.</param>
/// <param name="PhoneNumber">Optional contact telephone/mobile number.</param>
/// <param name="BillingAddress">Optional physical or postal billing address.</param>
/// <param name="BankAccountName">Optional account holder name for refund routing.</param>
/// <param name="BankAccountNumber">Optional bank account number.</param>
/// <param name="BankName">Optional banking institution moniker.</param>
/// <param name="Notes">Optional administrative notes or cashier comments.</param>
/// <param name="CreatedAt">Timestamp when this record was created.</param>
/// <param name="CreatedBy">User or system agent that registered the payer.</param>
/// <param name="UpdatedAt">Optional timestamp when this record was last modified.</param>
/// <param name="UpdatedBy">Optional user or system agent that updated the payer.</param>
/// <param name="TotalPaymentsCount">Total count of payment transactions associated with this payer.</param>
/// <param name="TotalPaidAmount">Sum of gross confirmed payments in IDR.</param>
/// <param name="Payments">List of linked payment summary records.</param>
public sealed record GetPayerByIdResponse(
    Guid Id,
    string PayerNumber,
    string PayerName,
    string PayerType,
    string? IdentificationNumber,
    string? Email,
    string? PhoneNumber,
    string? BillingAddress,
    string? BankAccountName,
    string? BankAccountNumber,
    string? BankName,
    string? Notes,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy,
    int TotalPaymentsCount,
    long TotalPaidAmount,
    IReadOnlyList<PayerPaymentSummaryDto> Payments);
