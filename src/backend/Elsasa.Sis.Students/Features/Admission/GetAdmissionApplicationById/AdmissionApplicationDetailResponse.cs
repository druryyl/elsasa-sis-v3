namespace Elsasa.Sis.Students.Features.Admission.GetAdmissionApplicationById;

/// <summary>
/// Complete admission dossier projection including applicant bio, placement, contacts, documents, assessment evidence, and decision (ADR-004, ADR-005).
/// </summary>
public sealed record AdmissionApplicationDetailResponse(
    Guid Id,
    string ApplicationNumber,
    Guid ApplicantId,
    Guid AdmissionPeriodId,
    Guid AcademicYearId,
    string ApplicationType,
    string RequestedEntryLevel,
    string? RequestedCampus,
    string? RequestedProgram,
    DateOnly RegistrationDate,
    string Status,
    DateTimeOffset? SubmittedAt,
    string? Notes,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy,
    ApplicantDetailBio Applicant,
    IReadOnlyList<AdmissionContactDetail> Contacts,
    IReadOnlyList<ApplicantDocumentDetail> Documents,
    IReadOnlyList<AssessmentEvidenceDetail> AssessmentEvidence,
    AdmissionDecisionDetail? Decision);

public sealed record ApplicantDetailBio(
    Guid Id,
    string FullName,
    string? PreferredName,
    DateOnly? DateOfBirth,
    string? PlaceOfBirth,
    string? Gender,
    string? Nationality,
    string? GovernmentIdType,
    string? GovernmentIdNumber,
    string? Email,
    string? Phone,
    string? Address,
    string? PreviousSchoolName,
    string? PreviousSchoolAddress,
    string? Notes);

public sealed record AdmissionContactDetail(
    Guid Id,
    Guid ApplicationId,
    string FullName,
    string Relationship,
    string? Phone,
    string? Email,
    string? Address,
    bool IsPrimary,
    DateTimeOffset CreatedAt,
    string? CreatedBy);

public sealed record ApplicantDocumentDetail(
    Guid Id,
    Guid ApplicationId,
    string DocumentType,
    string SubmissionStatus,
    DateOnly? SubmittedDate,
    DateOnly? VerifiedDate,
    string? VerifiedBy,
    string? FileReference,
    string? FileName,
    long? FileSizeBytes,
    string? ContentType,
    string? Notes,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);

public sealed record AssessmentEvidenceDetail(
    Guid Id,
    Guid ApplicationId,
    string AssessmentType,
    string? Score,
    string Source,
    DateOnly? AssessmentDate,
    string? Notes,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);

public sealed record AdmissionDecisionDetail(
    Guid Id,
    Guid ApplicationId,
    string DecisionType,
    DateOnly DecisionDate,
    DateOnly EffectiveDate,
    string DecidedBy,
    string? Reason,
    string? SupportingReference,
    Guid? DeferredAdmissionPeriodId,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
