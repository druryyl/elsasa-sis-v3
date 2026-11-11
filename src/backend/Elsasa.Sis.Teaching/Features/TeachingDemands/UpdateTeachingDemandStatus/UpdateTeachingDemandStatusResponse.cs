namespace Elsasa.Sis.Teaching.Features.TeachingDemands.UpdateTeachingDemandStatus;

/// <summary>
/// Response payload returned when a teaching demand's lifecycle status has been updated (TD-02, P2-S04).
/// </summary>
/// <param name="Id">The unique identifier of the demand.</param>
/// <param name="DemandCode">Authoritative demand code.</param>
/// <param name="PreviousStatus">Lifecycle status prior to transition.</param>
/// <param name="NewStatus">New active lifecycle status.</param>
/// <param name="DeclineReason">Recorded decline reason, if applicable.</param>
/// <param name="ClarificationNotes">Recorded clarification notes, if applicable.</param>
/// <param name="UpdatedAt">Timestamp of update in UTC.</param>
public sealed record UpdateTeachingDemandStatusResponse(
    Guid Id,
    string DemandCode,
    string PreviousStatus,
    string NewStatus,
    string? DeclineReason,
    string? ClarificationNotes,
    DateTimeOffset UpdatedAt);
