namespace Elsasa.Sis.Teaching.Features.TeacherProfiles.RegisterTeacherProfile;

/// <summary>
/// Request payload to register a candidate teacher profile into pending_approval standing (ADR-004, ADR-005, P2-S05).
/// </summary>
public sealed record RegisterTeacherProfileRequest(
    string? TeacherNumber,
    string FullName,
    string? DisplayName,
    string? EmployeeRef,
    string? NationalIdOrExternalRef,
    string? Email,
    string? Phone,
    Guid TeachingRoleId,
    string CampusAffiliation,
    string? Department,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo = null,
    string? Notes = null);
