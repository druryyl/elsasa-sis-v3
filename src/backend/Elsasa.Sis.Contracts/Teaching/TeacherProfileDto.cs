namespace Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Strongly-typed contract DTO representing the complete authoritative faculty dossier of a teacher resource.
/// </summary>
/// <param name="Id">The unique identifier of the teacher profile.</param>
/// <param name="TeacherNumber">The authoritative institutional teacher number.</param>
/// <param name="FullName">The legal full name of the teacher.</param>
/// <param name="DisplayName">Optional professional display name with credentials.</param>
/// <param name="EmployeeRef">Optional opaque employee/HR reference; strictly nullable for non-employees.</param>
/// <param name="NationalIdOrExternalRef">Optional national ID (NIK) or external reference for identity uniqueness.</param>
/// <param name="Email">Optional institutional contact email.</param>
/// <param name="Phone">Optional contact phone number.</param>
/// <param name="TeachingRoleId">The unique identifier of the assigned teaching role.</param>
/// <param name="TeachingRoleName">The display name of the assigned teaching role.</param>
/// <param name="CampusAffiliation">The campus affiliation ('All' or specific campus).</param>
/// <param name="Department">Optional department affiliation.</param>
/// <param name="Standing">The teacher standing state (pending_approval, active, restricted, suspended, inactive).</param>
/// <param name="EffectiveFrom">The effective start date of the teaching relationship.</param>
/// <param name="EffectiveTo">Optional concluding date of the teaching relationship.</param>
/// <param name="StandingReason">Optional explanation for current standing state.</param>
/// <param name="ApprovedBy">Optional identity of the Teaching Authority who approved the profile.</param>
/// <param name="ApprovedDate">Optional date when formal approval was granted.</param>
/// <param name="Notes">Optional administrative notes.</param>
/// <param name="Capabilities">Optional list of potential teaching capabilities held by the teacher.</param>
/// <param name="Qualifications">Optional list of qualification documentary evidence records.</param>
/// <param name="Authorizations">Optional list of teaching authorizations granted to the teacher.</param>
/// <param name="RecentCorrections">Optional list of auditable record corrections associated with the teacher.</param>
public sealed record TeacherProfileDto(
    Guid Id,
    string TeacherNumber,
    string FullName,
    string? DisplayName,
    string? EmployeeRef,
    string? NationalIdOrExternalRef,
    string? Email,
    string? Phone,
    Guid TeachingRoleId,
    string TeachingRoleName,
    string CampusAffiliation,
    string? Department,
    string Standing,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? StandingReason,
    string? ApprovedBy,
    DateOnly? ApprovedDate,
    string? Notes,
    IReadOnlyList<TeachingCapabilityDto>? Capabilities = null,
    IReadOnlyList<QualificationEvidenceDto>? Qualifications = null,
    IReadOnlyList<TeachingAuthorizationDto>? Authorizations = null,
    IReadOnlyList<TeachingRecordCorrectionDto>? RecentCorrections = null);
