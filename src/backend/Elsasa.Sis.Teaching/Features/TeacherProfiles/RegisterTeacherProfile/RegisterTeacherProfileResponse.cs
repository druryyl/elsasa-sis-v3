namespace Elsasa.Sis.Teaching.Features.TeacherProfiles.RegisterTeacherProfile;

/// <summary>
/// Response payload returned when a candidate teacher profile has been registered (ADR-004, ADR-005, P2-S05).
/// </summary>
public sealed record RegisterTeacherProfileResponse(
    Guid Id,
    string TeacherNumber,
    string FullName,
    string Standing);
