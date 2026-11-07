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
    INotificationHandler<CourseworkSubmittedEvent>,
    INotificationHandler<AcademicResultApprovedEvent>,
    INotificationHandler<AcademicResultPublishedEvent>,
    INotificationHandler<AcademicResultCorrectedEvent>,
    INotificationHandler<ReportCardPublishedEvent>,
    INotificationHandler<TranscriptIssuedEvent>,
    INotificationHandler<LearningProgressUpdatedEvent>,
    INotificationHandler<AcademicStandingDeterminedEvent>,
    INotificationHandler<PromotionRecommendationIssuedEvent>,
    INotificationHandler<AcademicInterventionInitiatedEvent>,
    INotificationHandler<AcademicInterventionConcludedEvent>,
    INotificationHandler<AcademicAchievementConferredEvent>,
    INotificationHandler<AcademicStandingCorrectedEvent>,
    INotificationHandler<PromotionRecommendationCorrectedEvent>
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
    private readonly ConcurrentBag<AcademicResultApprovedEvent> _academicResultApprovedEvents = [];
    private readonly ConcurrentBag<AcademicResultPublishedEvent> _academicResultPublishedEvents = [];
    private readonly ConcurrentBag<AcademicResultCorrectedEvent> _academicResultCorrectedEvents = [];
    private readonly ConcurrentBag<ReportCardPublishedEvent> _reportCardPublishedEvents = [];
    private readonly ConcurrentBag<TranscriptIssuedEvent> _transcriptIssuedEvents = [];
    private readonly ConcurrentBag<LearningProgressUpdatedEvent> _learningProgressUpdatedEvents = [];
    private readonly ConcurrentBag<AcademicStandingDeterminedEvent> _academicStandingDeterminedEvents = [];
    private readonly ConcurrentBag<PromotionRecommendationIssuedEvent> _promotionRecommendationIssuedEvents = [];
    private readonly ConcurrentBag<AcademicInterventionInitiatedEvent> _academicInterventionInitiatedEvents = [];
    private readonly ConcurrentBag<AcademicInterventionConcludedEvent> _academicInterventionConcludedEvents = [];
    private readonly ConcurrentBag<AcademicAchievementConferredEvent> _academicAchievementConferredEvents = [];
    private readonly ConcurrentBag<AcademicStandingCorrectedEvent> _academicStandingCorrectedEvents = [];
    private readonly ConcurrentBag<PromotionRecommendationCorrectedEvent> _promotionRecommendationCorrectedEvents = [];

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
    /// Gets all captured <see cref="AcademicResultApprovedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<AcademicResultApprovedEvent> AcademicResultApprovedEvents => _academicResultApprovedEvents;

    /// <summary>
    /// Gets all captured <see cref="AcademicResultPublishedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<AcademicResultPublishedEvent> AcademicResultPublishedEvents => _academicResultPublishedEvents;

    /// <summary>
    /// Gets all captured <see cref="AcademicResultCorrectedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<AcademicResultCorrectedEvent> AcademicResultCorrectedEvents => _academicResultCorrectedEvents;

    /// <summary>
    /// Gets all captured <see cref="ReportCardPublishedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<ReportCardPublishedEvent> ReportCardPublishedEvents => _reportCardPublishedEvents;

    /// <summary>
    /// Gets all captured <see cref="TranscriptIssuedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<TranscriptIssuedEvent> TranscriptIssuedEvents => _transcriptIssuedEvents;

    /// <summary>
    /// Gets all captured <see cref="LearningProgressUpdatedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<LearningProgressUpdatedEvent> LearningProgressUpdatedEvents => _learningProgressUpdatedEvents;

    /// <summary>
    /// Gets all captured <see cref="AcademicStandingDeterminedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<AcademicStandingDeterminedEvent> AcademicStandingDeterminedEvents => _academicStandingDeterminedEvents;

    /// <summary>
    /// Gets all captured <see cref="PromotionRecommendationIssuedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<PromotionRecommendationIssuedEvent> PromotionRecommendationIssuedEvents => _promotionRecommendationIssuedEvents;

    /// <summary>
    /// Gets all captured <see cref="AcademicInterventionInitiatedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<AcademicInterventionInitiatedEvent> AcademicInterventionInitiatedEvents => _academicInterventionInitiatedEvents;

    /// <summary>
    /// Gets all captured <see cref="AcademicInterventionConcludedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<AcademicInterventionConcludedEvent> AcademicInterventionConcludedEvents => _academicInterventionConcludedEvents;

    /// <summary>
    /// Gets all captured <see cref="AcademicAchievementConferredEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<AcademicAchievementConferredEvent> AcademicAchievementConferredEvents => _academicAchievementConferredEvents;

    /// <summary>
    /// Gets all captured <see cref="AcademicStandingCorrectedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<AcademicStandingCorrectedEvent> AcademicStandingCorrectedEvents => _academicStandingCorrectedEvents;

    /// <summary>
    /// Gets all captured <see cref="PromotionRecommendationCorrectedEvent"/> notifications.
    /// </summary>
    public IReadOnlyCollection<PromotionRecommendationCorrectedEvent> PromotionRecommendationCorrectedEvents => _promotionRecommendationCorrectedEvents;

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
        _academicResultApprovedEvents.Clear();
        _academicResultPublishedEvents.Clear();
        _academicResultCorrectedEvents.Clear();
        _reportCardPublishedEvents.Clear();
        _transcriptIssuedEvents.Clear();
        _learningProgressUpdatedEvents.Clear();
        _academicStandingDeterminedEvents.Clear();
        _promotionRecommendationIssuedEvents.Clear();
        _academicInterventionInitiatedEvents.Clear();
        _academicInterventionConcludedEvents.Clear();
        _academicAchievementConferredEvents.Clear();
        _academicStandingCorrectedEvents.Clear();
        _promotionRecommendationCorrectedEvents.Clear();
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

    /// <inheritdoc />
    public Task Handle(AcademicResultApprovedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _academicResultApprovedEvents.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(AcademicResultPublishedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _academicResultPublishedEvents.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(AcademicResultCorrectedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _academicResultCorrectedEvents.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(ReportCardPublishedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _reportCardPublishedEvents.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(TranscriptIssuedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _transcriptIssuedEvents.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(LearningProgressUpdatedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _learningProgressUpdatedEvents.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(AcademicStandingDeterminedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _academicStandingDeterminedEvents.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(PromotionRecommendationIssuedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _promotionRecommendationIssuedEvents.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(AcademicInterventionInitiatedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _academicInterventionInitiatedEvents.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(AcademicInterventionConcludedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _academicInterventionConcludedEvents.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(AcademicAchievementConferredEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _academicAchievementConferredEvents.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(AcademicStandingCorrectedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _academicStandingCorrectedEvents.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(PromotionRecommendationCorrectedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _promotionRecommendationCorrectedEvents.Add(notification);
        return Task.CompletedTask;
    }
}
