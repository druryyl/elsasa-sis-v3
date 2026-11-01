namespace Elsasa.Sis.Contracts.Common;

using MediatR;

/// <summary>
/// Marker interface for in-memory domain event contracts published across domains via MediatR (ADR-003).
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Gets the UTC timestamp when the domain event occurred.
    /// </summary>
    DateTimeOffset OccurredOn { get; }
}
