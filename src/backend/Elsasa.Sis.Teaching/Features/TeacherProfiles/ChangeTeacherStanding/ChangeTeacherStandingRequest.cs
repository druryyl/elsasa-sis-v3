namespace Elsasa.Sis.Teaching.Features.TeacherProfiles.ChangeTeacherStanding;

/// <summary>
/// Request payload for transitioning teacher profile standing (P2-S06).
/// </summary>
public sealed record ChangeTeacherStandingRequest(
    string Standing,
    string StandingReason);
