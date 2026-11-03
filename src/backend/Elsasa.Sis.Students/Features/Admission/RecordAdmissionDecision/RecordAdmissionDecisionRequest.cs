namespace Elsasa.Sis.Students.Features.Admission.RecordAdmissionDecision;

/// <summary>
/// Request payload for recording an official admission decision on an application (ADR-005).
/// </summary>
/// <param name="DecisionType">Outcome: accepted, rejected, waitlisted, withdrawn, expired, deferred.</param>
/// <param name="DecisionDate">Date the decision was formally reached.</param>
/// <param name="EffectiveDate">Date the decision takes effect.</param>
/// <param name="Reason">Optional justification or explanation for the decision outcome.</param>
/// <param name="SupportingReference">Optional reference identifier to committee minutes or policy decree.</param>
/// <param name="DeferredAdmissionPeriodId">Target operational period identifier if the decision is deferred.</param>
public sealed record RecordAdmissionDecisionRequest(
    string DecisionType,
    DateOnly DecisionDate,
    DateOnly EffectiveDate,
    string? Reason = null,
    string? SupportingReference = null,
    Guid? DeferredAdmissionPeriodId = null);
