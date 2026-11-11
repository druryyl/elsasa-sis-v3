namespace Elsasa.Sis.Contracts.Teaching.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published when a formal teaching capacity demand is received (ADR-003).
/// </summary>
/// <param name="DemandId">The unique identifier of the received demand.</param>
/// <param name="DemandCode">The authoritative demand code (e.g. DEM-2026-0001).</param>
/// <param name="SourceDomain">The domain originating the demand ('Academics', 'Students', 'Development').</param>
/// <param name="SourceWorkReference">Opaque reference identifier to the external entity needing staffing.</param>
/// <param name="SourceWorkLabel">Descriptive label of the source work item.</param>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="RequiredCapabilityCode">Required capability taxonomy code.</param>
/// <param name="ExpectedWorkloadHours">Estimated workload hours requested.</param>
/// <param name="RequiredTeachersCount">Number of teachers required.</param>
/// <param name="ReceivedBy">User or system actor who submitted the demand.</param>
/// <param name="OccurredOn">Timestamp when this event occurred in UTC.</param>
public sealed record TeachingDemandReceivedEvent(
    Guid DemandId,
    string DemandCode,
    string SourceDomain,
    Guid SourceWorkReference,
    string SourceWorkLabel,
    Guid AcademicPeriodId,
    string RequiredCapabilityCode,
    decimal ExpectedWorkloadHours,
    int RequiredTeachersCount,
    string ReceivedBy,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TeachingDemandReceivedEvent"/> record with current UTC timestamp.
    /// </summary>
    public TeachingDemandReceivedEvent(
        Guid demandId,
        string demandCode,
        string sourceDomain,
        Guid sourceWorkReference,
        string sourceWorkLabel,
        Guid academicPeriodId,
        string requiredCapabilityCode,
        decimal expectedWorkloadHours,
        int requiredTeachersCount,
        string receivedBy)
        : this(
            demandId,
            demandCode,
            sourceDomain,
            sourceWorkReference,
            sourceWorkLabel,
            academicPeriodId,
            requiredCapabilityCode,
            expectedWorkloadHours,
            requiredTeachersCount,
            receivedBy,
            DateTimeOffset.UtcNow)
    {
    }
}
