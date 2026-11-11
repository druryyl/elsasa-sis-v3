namespace Elsasa.Sis.Contracts.Teaching.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published when a temporary teaching substitution is committed (ADR-003).
/// </summary>
/// <param name="SubstitutionId">The unique identifier of the substitution.</param>
/// <param name="SubstitutionNumber">The authoritative substitution number.</param>
/// <param name="OriginalAssignmentId">The covered assignment identifier.</param>
/// <param name="OriginalTeacherId">The absent primary teacher identifier.</param>
/// <param name="SubstituteTeacherId">The covering substitute teacher identifier.</param>
/// <param name="CoverageStartDate">Start date of coverage.</param>
/// <param name="CoverageEndDate">End date of coverage.</param>
/// <param name="WorkloadImpactHours">Workload effort transferred to substitute.</param>
/// <param name="ApprovedBy">Coordinator or Authority who approved the substitution.</param>
/// <param name="IsDelegatedApproval">Indicates whether approved via Pre-Authorized Pool Delegation (Resolution 3.8).</param>
/// <param name="OccurredOn">Timestamp when this event occurred in UTC.</param>
public sealed record TeachingSubstitutionAssignedEvent(
    Guid SubstitutionId,
    string SubstitutionNumber,
    Guid OriginalAssignmentId,
    Guid OriginalTeacherId,
    Guid SubstituteTeacherId,
    DateOnly CoverageStartDate,
    DateOnly CoverageEndDate,
    decimal WorkloadImpactHours,
    string ApprovedBy,
    bool IsDelegatedApproval,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TeachingSubstitutionAssignedEvent"/> record with current UTC timestamp.
    /// </summary>
    public TeachingSubstitutionAssignedEvent(
        Guid substitutionId,
        string substitutionNumber,
        Guid originalAssignmentId,
        Guid originalTeacherId,
        Guid substituteTeacherId,
        DateOnly coverageStartDate,
        DateOnly coverageEndDate,
        decimal workloadImpactHours,
        string approvedBy,
        bool isDelegatedApproval)
        : this(
            substitutionId,
            substitutionNumber,
            originalAssignmentId,
            originalTeacherId,
            substituteTeacherId,
            coverageStartDate,
            coverageEndDate,
            workloadImpactHours,
            approvedBy,
            isDelegatedApproval,
            DateTimeOffset.UtcNow)
    {
    }
}
