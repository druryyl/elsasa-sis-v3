namespace Elsasa.Sis.Teaching.Features.TeacherProfiles.ApproveTeacherProfile;

/// <summary>
/// Response payload returned when a teacher profile has been approved by the Teaching Authority (P2-S06).
/// </summary>
public sealed record ApproveTeacherProfileResponse(
    Guid Id,
    string TeacherNumber,
    string Standing,
    string ApprovedBy,
    DateOnly ApprovedDate);
