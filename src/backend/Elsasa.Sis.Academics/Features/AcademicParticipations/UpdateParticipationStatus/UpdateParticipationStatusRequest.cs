namespace Elsasa.Sis.Academics.Features.AcademicParticipations.UpdateParticipationStatus;

/// <summary>
/// Request payload for transitioning an academic participation status (P2-S05, GAP-08, UC-ACD-11).
/// </summary>
/// <param name="NewStatus">Target status: in_progress, paused, or withdrawn.</param>
/// <param name="Notes">Optional notes regarding the status transition.</param>
public sealed record UpdateParticipationStatusRequest(
    string NewStatus,
    string? Notes = null);
