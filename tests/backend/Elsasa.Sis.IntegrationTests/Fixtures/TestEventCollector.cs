namespace Elsasa.Sis.IntegrationTests.Fixtures;

using System.Collections.Concurrent;
using Elsasa.Sis.Contracts.Students.Events;
using MediatR;

/// <summary>
/// In-memory notification handler capturing domain events for integration test verification (ADR-003).
/// </summary>
public sealed class TestEventCollector : INotificationHandler<ApplicantAcceptedEvent>
{
    private readonly ConcurrentBag<ApplicantAcceptedEvent> _events = [];

    /// <summary>
    /// Gets all captured <see cref="ApplicantAcceptedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<ApplicantAcceptedEvent> ApplicantAcceptedEvents => _events;

    /// <summary>
    /// Clears all captured notifications.
    /// </summary>
    public void Reset()
    {
        _events.Clear();
    }

    /// <inheritdoc />
    public Task Handle(ApplicantAcceptedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _events.Add(notification);
        return Task.CompletedTask;
    }
}
