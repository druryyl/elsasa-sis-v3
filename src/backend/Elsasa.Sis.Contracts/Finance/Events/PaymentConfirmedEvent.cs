namespace Elsasa.Sis.Contracts.Finance.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published when a payment has been verified and confirmed (ADR-003).
/// </summary>
/// <param name="PaymentId">The unique identifier of the confirmed payment.</param>
/// <param name="StudentId">The optional unique identifier of the associated enrolled student.</param>
/// <param name="ApplicantId">The optional unique identifier of the associated pre-enrollment applicant.</param>
/// <param name="Amount">The gross confirmed payment amount in IDR.</param>
/// <param name="ReceiptNumber">The sequential official receipt voucher number (e.g., REC-2026-000001).</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record PaymentConfirmedEvent(
    Guid PaymentId,
    Guid? StudentId,
    Guid? ApplicantId,
    long Amount,
    string ReceiptNumber,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentConfirmedEvent"/> record with the current UTC timestamp.
    /// </summary>
    /// <param name="paymentId">The unique identifier of the confirmed payment.</param>
    /// <param name="studentId">The optional unique identifier of the associated enrolled student.</param>
    /// <param name="applicantId">The optional unique identifier of the associated pre-enrollment applicant.</param>
    /// <param name="amount">The gross confirmed payment amount in IDR.</param>
    /// <param name="receiptNumber">The sequential official receipt voucher number.</param>
    public PaymentConfirmedEvent(
        Guid paymentId,
        Guid? studentId,
        Guid? applicantId,
        long amount,
        string receiptNumber)
        : this(paymentId, studentId, applicantId, amount, receiptNumber, DateTimeOffset.UtcNow)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentConfirmedEvent"/> record for backward compatibility.
    /// </summary>
    public PaymentConfirmedEvent(Guid paymentId, Guid studentId, decimal amount)
        : this(paymentId, studentId, null, (long)amount, string.Empty, DateTimeOffset.UtcNow)
    {
    }
}
