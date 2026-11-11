namespace Elsasa.Sis.Teaching.Features.TeachingDemands.CreateTeachingDemand;

/// <summary>
/// Response payload returned when a teaching capacity demand has been successfully created (TD-02, P2-S04).
/// </summary>
/// <param name="Id">The unique identifier of the created demand.</param>
/// <param name="DemandCode">The unique human-readable demand code (e.g. 'DEM-2026-0001').</param>
/// <param name="Status">Initial lifecycle status ('received').</param>
/// <param name="SourceDomain">The domain originating the demand.</param>
/// <param name="SourceWorkLabel">Descriptive label of the source work item.</param>
/// <param name="CreatedAt">Timestamp when demand record was created in UTC.</param>
public sealed record CreateTeachingDemandResponse(
    Guid Id,
    string DemandCode,
    string Status,
    string SourceDomain,
    string SourceWorkLabel,
    DateTimeOffset CreatedAt);
