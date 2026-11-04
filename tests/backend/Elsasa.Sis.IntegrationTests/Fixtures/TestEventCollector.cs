namespace Elsasa.Sis.IntegrationTests.Fixtures;

using System.Collections.Concurrent;
using Elsasa.Sis.Contracts.Students.Events;
using MediatR;

/// <summary>
/// In-memory notification handler capturing domain events for integration test verification (ADR-003).
/// </summary>
public sealed class TestEventCollector :
    INotificationHandler<ApplicantAcceptedEvent>,
    INotificationHandler<StudentEnrolledEvent>,
    INotificationHandler<PlacementChangedEvent>
{
    private readonly ConcurrentBag<ApplicantAcceptedEvent> _applicantAcceptedEvents = [];
    private readonly ConcurrentBag<StudentEnrolledEvent> _studentEnrolledEvents = [];
    private readonly ConcurrentBag<PlacementChangedEvent> _placementChangedEvents = [];

    /// <summary>
    /// Gets all captured <see cref="ApplicantAcceptedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<ApplicantAcceptedEvent> ApplicantAcceptedEvents => _applicantAcceptedEvents;

    /// <summary>
    /// Gets all captured <see cref="StudentEnrolledEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<StudentEnrolledEvent> StudentEnrolledEvents => _studentEnrolledEvents;

    /// <summary>
    /// Gets all captured <see cref="PlacementChangedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<PlacementChangedEvent> PlacementChangedEvents => _placementChangedEvents;

    /// <summary>
    /// Clears all captured notifications.
    /// </summary>
    public void Reset()
    {
        _applicantAcceptedEvents.Clear();
        _studentEnrolledEvents.Clear();
        _placementChangedEvents.Clear();
    }

    /// <inheritdoc />
    public Task Handle(ApplicantAcceptedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _applicantAcceptedEvents.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(StudentEnrolledEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _studentEnrolledEvents.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(PlacementChangedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _placementChangedEvents.Add(notification);
        return Task.CompletedTask;
    }
}
