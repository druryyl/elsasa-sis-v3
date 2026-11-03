namespace Elsasa.Sis.Students.Features.Admission.GetApplicationStatusByNumber;

/// <summary>
/// Sanitized public status projection for self-service applicant tracking (ADR-004, ADR-005).
/// </summary>
public sealed record ApplicationStatusResponse(
    string ApplicationNumber,
    string ApplicantFullName,
    string RequestedEntryLevel,
    string Status,
    DateOnly RegistrationDate,
    IReadOnlyList<PublicDocumentStatusItem> DocumentStatuses,
    PublicDecisionNotice? DecisionNotice);

public sealed record PublicDocumentStatusItem(
    string DocumentType,
    string SubmissionStatus);

public sealed record PublicDecisionNotice(
    string DecisionType,
    DateOnly EffectiveDate);
