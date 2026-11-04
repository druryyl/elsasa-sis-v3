namespace Elsasa.Sis.Contracts.Finance.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published when a financial obligation has been assessed for a student (GAP-008, TD-02, TD-07, ADR-003, ADR-009).
/// </summary>
/// <param name="ObligationId">The unique identifier of the assessed financial obligation.</param>
/// <param name="StudentId">The unique identifier of the student liability holder.</param>
/// <param name="Amount">The assessed monetary amount in IDR integer representation (BIGINT).</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record ObligationAssessedEvent(
    Guid ObligationId,
    Guid StudentId,
    long Amount,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObligationAssessedEvent"/> record with the current UTC timestamp.
    /// </summary>
    /// <param name="obligationId">The unique identifier of the assessed financial obligation.</param>
    /// <param name="studentId">The unique identifier of the student liability holder.</param>
    /// <param name="amount">The assessed monetary amount in IDR integer representation (BIGINT).</param>
    public ObligationAssessedEvent(Guid obligationId, Guid studentId, long amount)
        : this(obligationId, studentId, amount, DateTimeOffset.UtcNow)
    {
    }

    /// <summary>
    /// Backward-compatible convenience constructor accepting a decimal monetary amount converted to integer IDR.
    /// </summary>
    /// <param name="obligationId">The unique identifier of the assessed financial obligation.</param>
    /// <param name="studentId">The unique identifier of the student liability holder.</param>
    /// <param name="amount">The assessed monetary amount as decimal.</param>
    public ObligationAssessedEvent(Guid obligationId, Guid studentId, decimal amount)
        : this(obligationId, studentId, (long)amount, DateTimeOffset.UtcNow)
    {
    }
}
