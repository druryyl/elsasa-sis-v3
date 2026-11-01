namespace Elsasa.Sis.Contracts.Finance.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published when a payment has been verified and confirmed (ADR-003).
/// </summary>
/// <param name="PaymentId">The unique identifier of the confirmed payment.</param>
/// <param name="StudentId">The unique identifier of the associated student.</param>
/// <param name="Amount">The gross confirmed payment amount.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record PaymentConfirmedEvent(
    Guid PaymentId,
    Guid StudentId,
    decimal Amount,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentConfirmedEvent"/> record with the current UTC timestamp.
    /// </summary>
    /// <param name="paymentId">The unique identifier of the confirmed payment.</param>
    /// <param name="studentId">The unique identifier of the associated student.</param>
    /// <param name="amount">The gross confirmed payment amount.</param>
    public PaymentConfirmedEvent(Guid paymentId, Guid studentId, decimal amount)
        : this(paymentId, studentId, amount, DateTimeOffset.UtcNow)
    {
    }
}
