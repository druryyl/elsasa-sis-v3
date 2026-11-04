namespace Elsasa.Sis.IntegrationTests.Fixtures;

using System.Collections.Concurrent;
using Elsasa.Sis.Contracts.Finance.Events;
using Elsasa.Sis.Contracts.Students.Events;
using MediatR;

/// <summary>
/// In-memory notification handler capturing domain events for integration test verification (ADR-003).
/// </summary>
public sealed class TestEventCollector :
    INotificationHandler<ApplicantAcceptedEvent>,
    INotificationHandler<StudentEnrolledEvent>,
    INotificationHandler<PlacementChangedEvent>,
    INotificationHandler<ObligationAssessedEvent>,
    INotificationHandler<PaymentConfirmedEvent>
{
    private readonly ConcurrentBag<ApplicantAcceptedEvent> _applicantAcceptedEvents = [];
    private readonly ConcurrentBag<StudentEnrolledEvent> _studentEnrolledEvents = [];
    private readonly ConcurrentBag<PlacementChangedEvent> _placementChangedEvents = [];
    private readonly ConcurrentBag<ObligationAssessedEvent> _obligationAssessedEvents = [];
    private readonly ConcurrentBag<PaymentConfirmedEvent> _paymentConfirmedEvents = [];

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
    /// Gets all captured <see cref="ObligationAssessedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<ObligationAssessedEvent> ObligationAssessedEvents => _obligationAssessedEvents;

    /// <summary>
    /// Gets all captured <see cref="PaymentConfirmedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<PaymentConfirmedEvent> PaymentConfirmedEvents => _paymentConfirmedEvents;

    /// <summary>
    /// Clears all captured notifications.
    /// </summary>
    public void Reset()
    {
        _applicantAcceptedEvents.Clear();
        _studentEnrolledEvents.Clear();
        _placementChangedEvents.Clear();
        _obligationAssessedEvents.Clear();
        _paymentConfirmedEvents.Clear();
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

    /// <inheritdoc />
    public Task Handle(ObligationAssessedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _obligationAssessedEvents.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(PaymentConfirmedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _paymentConfirmedEvents.Add(notification);
        return Task.CompletedTask;
    }
}

