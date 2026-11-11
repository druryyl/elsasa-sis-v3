namespace Elsasa.Sis.Teaching.Features.TeachingDemands.UpdateTeachingDemand;

/// <summary>
/// Response payload returned when a teaching demand has been updated (TD-02, P2-S04).
/// </summary>
/// <param name="Id">The unique identifier of the demand.</param>
/// <param name="DemandCode">Authoritative demand code.</param>
/// <param name="Status">Demand status following update.</param>
/// <param name="UpdatedAt">Timestamp of update in UTC.</param>
public sealed record UpdateTeachingDemandResponse(
    Guid Id,
    string DemandCode,
    string Status,
    DateTimeOffset UpdatedAt);
