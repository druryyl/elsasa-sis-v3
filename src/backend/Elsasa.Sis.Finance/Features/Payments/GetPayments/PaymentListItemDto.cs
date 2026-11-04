namespace Elsasa.Sis.Finance.Features.Payments.GetPayments;

/// <summary>
/// Data transfer object representing a payment record in worklists and verification queues (SCR-FIN-02, TD-04).
/// </summary>
public sealed record PaymentListItemDto(
    Guid Id,
    string PaymentNumber,
    string? ReceiptNumber,
    Guid StudentAccountId,
    string AccountNumber,
    Guid? StudentId,
    string? StudentNumber,
    string? StudentFullName,
    Guid? ApplicantId,
    Guid PayerId,
    string PayerNumber,
    string PayerName,
    long PaymentAmount,
    string CurrencyCode,
    string PaymentMethod,
    string? ExternalReference,
    DateOnly PaymentDate,
    DateTimeOffset SubmissionDate,
    string ConfirmationStatus,
    DateTimeOffset? ConfirmationDate,
    string? ConfirmedBy,
    string? RejectionReason,
    string? Notes,
    long TotalAllocated,
    long UnallocatedAmount);
