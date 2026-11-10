namespace Elsasa.Sis.Teaching.Features.TeacherProfiles.UpdateTeacherProfile;

/// <summary>
/// Request payload for updating mutable teacher profile attributes (ADR-004, ADR-005, P2-S05).
/// </summary>
public sealed record UpdateTeacherProfileRequest(
    string? DisplayName,
    string? Email,
    string? Phone,
    string? CampusAffiliation,
    string? Department,
    string? Notes);
