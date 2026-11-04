namespace Elsasa.Sis.Finance.Features.Payments.RecordAndConfirmPayment;

/// <summary>
/// Request payload for cashier counter payment recording with atomic confirmation (SCR-FIN-02, UC-FIN-04, TD-04, OQ-004).
/// </summary>
public sealed record RecordAndConfirmPaymentRequest(
    Guid? StudentId,
    Guid? ApplicantId,
    Guid? StudentAccountId,
    long Amount,
    string PaymentMethod,
    DateOnly? PaymentDate,
    string? ExternalReference,
    string PayerName,
    string? PayerIdentificationNumber = null,
    string? PayerEmail = null,
    string? PayerPhone = null,
    string? PayerType = null,
    Guid? PayerId = null,
    string? Notes = null);
