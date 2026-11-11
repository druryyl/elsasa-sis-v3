namespace Elsasa.Sis.Teaching.Features.TeachingDemands.UpdateTeachingDemandStatus;

/// <summary>
/// Request payload to transition the lifecycle status of a teaching demand (TD-02, P2-S04).
/// </summary>
/// <param name="Status">Target status ('accepted_for_staffing', 'returned_for_clarification', 'declined', 'cancelled').</param>
/// <param name="DeclineReason">Mandatory justification when transitioning to 'declined'.</param>
/// <param name="ClarificationNotes">Optional guidance notes when returning for clarification.</param>
public sealed record UpdateTeachingDemandStatusRequest(
    string Status,
    string? DeclineReason = null,
    string? ClarificationNotes = null);
