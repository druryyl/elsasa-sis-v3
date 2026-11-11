namespace Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Strongly-typed contract DTO representing a materialized capacity monitoring snapshot per teacher and academic period.
/// </summary>
/// <param name="Id">The unique identifier of the workload commitment snapshot.</param>
/// <param name="TeacherProfileId">The teacher profile identifier.</param>
/// <param name="AcademicPeriodId">The academic period identifier.</param>
/// <param name="WorkloadPolicyId">The workload policy governing capacity limits.</param>
/// <param name="PlannedWorkloadHours">Hours associated with proposed assignments.</param>
/// <param name="CommittedWorkloadHours">Hours associated with approved active assignments.</param>
/// <param name="MaxCapacityLimitHours">Maximum standard weekly capacity threshold from active policy.</param>
/// <param name="RemainingCapacityHours">Calculated remaining capacity (Max - Committed).</param>
/// <param name="Status">Workload threshold status ('normal', 'under_allocated', 'approaching_limit', 'overloaded_with_exception', 'breached').</param>
/// <param name="ActiveAssignmentCount">Number of active approved assignments.</param>
/// <param name="ApprovedExceptionCount">Number of formal approved workload exceptions.</param>
/// <param name="AsOfTimestamp">Timestamp when this snapshot was last synchronized or recalculated.</param>
/// <param name="CreatedAt">Timestamp when this snapshot record was created in UTC.</param>
public sealed record WorkloadCommitmentDto(
    Guid Id,
    Guid TeacherProfileId,
    Guid AcademicPeriodId,
    Guid WorkloadPolicyId,
    decimal PlannedWorkloadHours,
    decimal CommittedWorkloadHours,
    decimal MaxCapacityLimitHours,
    decimal RemainingCapacityHours,
    string Status,
    int ActiveAssignmentCount,
    int ApprovedExceptionCount,
    DateTimeOffset AsOfTimestamp,
    DateTimeOffset CreatedAt);
