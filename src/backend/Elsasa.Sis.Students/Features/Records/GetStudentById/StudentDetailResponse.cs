namespace Elsasa.Sis.Students.Features.Records.GetStudentById;

/// <summary>
/// Authoritative 360-degree student dossier projection response (ADR-004, ADR-005, P4-S06).
/// </summary>
public sealed record StudentDetailResponse(
    Guid Id,
    string StudentNumber,
    Guid ApplicantId,
    string FullName,
    string? PreferredName,
    DateOnly? DateOfBirth,
    string? PlaceOfBirth,
    string? Gender,
    string? Nationality,
    string? GovernmentIdType,
    string? GovernmentIdNumber,
    string? Address,
    string? Phone,
    string? Email,
    bool IsIndependent,
    string CurrentStatus,
    DateOnly EnrollmentDate,
    string? Notes,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy,
    IReadOnlyList<EnrollmentHistoryDto> EnrollmentHistory,
    IReadOnlyList<StudentGuardianRelationshipDto> GuardianRelationships,
    IReadOnlyList<StudentDocumentDto> Documents,
    AdministrativePlacementDto? CurrentPlacement,
    IReadOnlyList<AdministrativePlacementDto> PlacementHistory,
    IReadOnlyList<StudentStatusDto> StatusHistory);

/// <summary>
/// Authoritative administrative placement period projection details.
/// </summary>
public sealed record AdministrativePlacementDto(
    Guid Id,
    string Campus,
    string SchoolLevel,
    string Grade,
    string AcademicYear,
    DateOnly EffectiveDate,
    DateOnly? EndDate,
    bool IsCurrent,
    string? Program,
    string? Stream,
    string? Cohort,
    string? Homeroom,
    string? Reason,
    string? AuthorizedBy,
    string? DecisionReference);

/// <summary>
/// Authoritative operational student status period projection details.
/// </summary>
public sealed record StudentStatusDto(
    Guid Id,
    string Status,
    DateOnly EffectiveDate,
    DateOnly? EndDate,
    bool IsCurrent,
    string? Reason,
    string? AuthorizedBy,
    string? DecisionReference);

/// <summary>
/// Historical enrollment placement and payment record.
/// </summary>
public sealed record EnrollmentHistoryDto(
    DateOnly EnrollmentDate,
    string Campus,
    string Level,
    string AcademicYear,
    string? Program,
    string? Cohort,
    string ApprovedBy,
    bool? PaymentConfirmed,
    string? PaymentReference,
    bool PaymentWaived,
    string? WaiverReference);

/// <summary>
/// Associated guardian relationship details with designated authorities.
/// </summary>
public sealed record StudentGuardianRelationshipDto(
    Guid RelationshipId,
    Guid GuardianId,
    string FullName,
    string RelationshipType,
    string? GovernmentIdType,
    string? GovernmentIdNumber,
    string? Phone,
    string? Email,
    string? Address,
    string? Occupation,
    bool IsPrimaryContact,
    bool IsEmergencyContact,
    bool IsFinancialContact,
    bool IsAuthorizedPickup,
    bool IsActive);

/// <summary>
/// Official student document record with metadata and verification status.
/// </summary>
public sealed record StudentDocumentDto(
    Guid Id,
    string DocumentType,
    string SubmissionStatus,
    DateOnly? SubmittedDate,
    DateOnly? VerifiedDate,
    string? VerifiedBy,
    string? FileName,
    long? FileSizeBytes,
    string? ContentType,
    string? FileReference);
