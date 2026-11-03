namespace Elsasa.Sis.Students.Features.Admission.RecordAdmissionDecision;

/// <summary>
/// Response payload after recording an official admission decision (ADR-005).
/// </summary>
/// <param name="DecisionId">Unique identifier of the recorded decision.</param>
/// <param name="ApplicationId">Unique identifier of the admission application.</param>
/// <param name="DecisionType">Outcome recorded (accepted, rejected, waitlisted, withdrawn, expired, deferred).</param>
/// <param name="EffectiveDate">Date the decision takes effect.</param>
public sealed record RecordAdmissionDecisionResponse(
    Guid DecisionId,
    Guid ApplicationId,
    string DecisionType,
    DateOnly EffectiveDate);
