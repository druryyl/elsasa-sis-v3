namespace Elsasa.Sis.IntegrationTests.Fixtures;

using System.Collections.Concurrent;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Contracts.Finance.Events;
using Elsasa.Sis.Contracts.Students.Events;
using MediatR;

/// <summary>
/// In-memory notification handler capturing domain events for integration test verification (ADR-003, P6-S16).
/// </summary>
public sealed class TestEventCollector :
    INotificationHandler<ApplicantAcceptedEvent>,
    INotificationHandler<StudentEnrolledEvent>,
    INotificationHandler<PlacementChangedEvent>,
    INotificationHandler<ObligationAssessedEvent>,
    INotificationHandler<PaymentConfirmedEvent>,
    INotificationHandler<LearningStructureActivatedEvent>,
    INotificationHandler<AcademicParticipationRegisteredEvent>,
    INotificationHandler<ParticipationConcludedEvent>,
    INotificationHandler<LearningActivityConductedEvent>,
    INotificationHandler<AttendanceRecordedEvent>,
    INotificationHandler<AttendanceCorrectedEvent>,
    INotificationHandler<AssignmentReleasedEvent>,
    INotificationHandler<CourseworkSubmittedEvent>
{
    private readonly ConcurrentBag<ApplicantAcceptedEvent> _applicantAcceptedEvents = [];
    private readonly ConcurrentBag<StudentEnrolledEvent> _studentEnrolledEvents = [];
    private readonly ConcurrentBag<PlacementChangedEvent> _placementChangedEvents = [];
    private readonly ConcurrentBag<ObligationAssessedEvent> _obligationAssessedEvents = [];
    private readonly ConcurrentBag<PaymentConfirmedEvent> _paymentConfirmedEvents = [];
    private readonly ConcurrentBag<LearningStructureActivatedEvent> _learningStructureActivatedEvents = [];
    private readonly ConcurrentBag<AcademicParticipationRegisteredEvent> _academicParticipationRegisteredEvents = [];
    private readonly ConcurrentBag<ParticipationConcludedEvent> _participationConcludedEvents = [];
    private readonly ConcurrentBag<LearningActivityConductedEvent> _learningActivityConductedEvents = [];
    private readonly ConcurrentBag<AttendanceRecordedEvent> _attendanceRecordedEvents = [];
    private readonly ConcurrentBag<AttendanceCorrectedEvent> _attendanceCorrectedEvents = [];
    private readonly ConcurrentBag<AssignmentReleasedEvent> _assignmentReleasedEvents = [];
    private readonly ConcurrentBag<CourseworkSubmittedEvent> _courseworkSubmittedEvents = [];

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
    /// Gets all captured <see cref="LearningStructureActivatedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<LearningStructureActivatedEvent> LearningStructureActivatedEvents => _learningStructureActivatedEvents;

    /// <summary>
    /// Gets all captured <see cref="AcademicParticipationRegisteredEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<AcademicParticipationRegisteredEvent> AcademicParticipationRegisteredEvents => _academicParticipationRegisteredEvents;

    /// <summary>
    /// Gets all captured <see cref="ParticipationConcludedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<ParticipationConcludedEvent> ParticipationConcludedEvents => _participationConcludedEvents;

    /// <summary>
    /// Gets all captured <see cref="LearningActivityConductedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<LearningActivityConductedEvent> LearningActivityConductedEvents => _learningActivityConductedEvents;

    /// <summary>
    /// Gets all captured <see cref="AttendanceRecordedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<AttendanceRecordedEvent> AttendanceRecordedEvents => _attendanceRecordedEvents;

    /// <summary>
    /// Gets all captured <see cref="AttendanceCorrectedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<AttendanceCorrectedEvent> AttendanceCorrectedEvents => _attendanceCorrectedEvents;

    /// <summary>
    /// Gets all captured <see cref="AssignmentReleasedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<AssignmentReleasedEvent> AssignmentReleasedEvents => _assignmentReleasedEvents;

    /// <summary>
    /// Gets all captured <see cref="CourseworkSubmittedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<CourseworkSubmittedEvent> CourseworkSubmittedEvents => _courseworkSubmittedEvents;

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
        _learningStructureActivatedEvents.Clear();
        _academicParticipationRegisteredEvents.Clear();
        _participationConcludedEvents.Clear();
        _learningActivityConductedEvents.Clear();
        _attendanceRecordedEvents.Clear();
        _attendanceCorrectedEvents.Clear();
        _assignmentReleasedEvents.Clear();
        _courseworkSubmittedEvents.Clear();
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

    /// <inheritdoc />
    public Task Handle(LearningStructureActivatedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _learningStructureActivatedEvents.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(AcademicParticipationRegisteredEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _academicParticipationRegisteredEvents.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(ParticipationConcludedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _participationConcludedEvents.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(LearningActivityConductedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _learningActivityConductedEvents.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(AttendanceRecordedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _attendanceRecordedEvents.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(AttendanceCorrectedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _attendanceCorrectedEvents.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(AssignmentReleasedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _assignmentReleasedEvents.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(CourseworkSubmittedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _courseworkSubmittedEvents.Add(notification);
        return Task.CompletedTask;
    }
}
