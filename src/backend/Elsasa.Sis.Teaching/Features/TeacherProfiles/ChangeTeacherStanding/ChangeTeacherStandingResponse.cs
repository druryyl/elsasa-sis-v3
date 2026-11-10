namespace Elsasa.Sis.Teaching.Features.TeacherProfiles.ChangeTeacherStanding;

/// <summary>
/// Response payload returned when teacher standing has been successfully transitioned (P2-S06).
/// </summary>
public sealed record ChangeTeacherStandingResponse(
    Guid Id,
    string PreviousStanding,
    string Standing,
    string StandingReason,
    string UpdatedBy,
    DateTimeOffset UpdatedAt);
