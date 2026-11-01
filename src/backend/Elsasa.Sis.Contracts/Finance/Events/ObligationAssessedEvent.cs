namespace Elsasa.Sis.Contracts.Finance.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published when a financial obligation has been assessed for a student (ADR-003).
/// </summary>
/// <param name="ObligationId">The unique identifier of the assessed financial obligation.</param>
/// <param name="StudentId">The unique identifier of the student liability holder.</param>
/// <param name="Amount">The assessed monetary amount.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record ObligationAssessedEvent(
    Guid ObligationId,
    Guid StudentId,
    decimal Amount,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObligationAssessedEvent"/> record with the current UTC timestamp.
    /// </summary>
    /// <param name="obligationId">The unique identifier of the assessed financial obligation.</param>
    /// <param name="studentId">The unique identifier of the student liability holder.</param>
    /// <param name="amount">The assessed monetary amount.</param>
    public ObligationAssessedEvent(Guid obligationId, Guid studentId, decimal amount)
        : this(obligationId, studentId, amount, DateTimeOffset.UtcNow)
    {
    }
}
