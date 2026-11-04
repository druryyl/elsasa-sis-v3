namespace Elsasa.Sis.Finance.Features.Payments.SubmitPayment;

/// <summary>
/// Request payload for self-service payment intake (SCR-SELF-02, UC-FIN-01, TD-04, OQ-004).
/// </summary>
public sealed record SubmitPaymentRequest(
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
