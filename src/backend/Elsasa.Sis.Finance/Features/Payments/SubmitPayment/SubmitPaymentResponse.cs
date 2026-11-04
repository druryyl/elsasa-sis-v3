namespace Elsasa.Sis.Finance.Features.Payments.SubmitPayment;

/// <summary>
/// Response payload emitted after self-service payment submission.
/// </summary>
public sealed record SubmitPaymentResponse(
    Guid PaymentId,
    string PaymentNumber,
    string ConfirmationStatus,
    long Amount,
    string PaymentMethod,
    DateOnly PaymentDate,
    DateTimeOffset SubmissionDate,
    Guid PayerId,
    Guid StudentAccountId,
    Guid? StudentId,
    Guid? ApplicantId);
