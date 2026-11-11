namespace Elsasa.Sis.Teaching.Features.Workload.GetWorkloadCommitments;

/// <summary>
/// Strongly-typed response DTO representing a faculty member's materialized capacity snapshot joined with teacher profile and policy information (TD-04, P3-S09).
/// </summary>
/// <param name="Id">The unique identifier of the workload commitment snapshot.</param>
/// <param name="TeacherProfileId">The teacher profile identifier.</param>
/// <param name="TeacherNumber">The unique teacher employee/identification number.</param>
/// <param name="FullName">The full name of the teacher.</param>
/// <param name="DisplayName">Optional display name of the teacher.</param>
/// <param name="CampusAffiliation">Campus affiliation of the teacher.</param>
/// <param name="Standing">Current standing of the teacher ('active', etc.).</param>
/// <param name="AcademicPeriodId">The academic period identifier.</param>
/// <param name="WorkloadPolicyId">The workload policy governing capacity limits.</param>
/// <param name="PolicyCode">The unique code of the governing workload policy.</param>
/// <param name="PolicyName">The descriptive name of the governing workload policy.</param>
/// <param name="MaxStandardCapacity">Maximum standard weekly capacity threshold from active policy.</param>
/// <param name="WarningThreshold">Warning threshold where allocations approach limit.</param>
/// <param name="ProhibitedThreshold">Prohibited ceiling requiring formal authority exception approval.</param>
/// <param name="PlannedWorkloadHours">Hours associated with proposed assignments.</param>
/// <param name="CommittedWorkloadHours">Hours associated with approved active assignments.</param>
/// <param name="MaxCapacityLimitHours">Maximum capacity limit recorded in snapshot.</param>
/// <param name="RemainingCapacityHours">Remaining capacity hours (Max - Committed).</param>
/// <param name="Status">Workload threshold status ('normal', 'under_allocated', 'approaching_limit', 'overloaded_with_exception', 'breached').</param>
/// <param name="ActiveAssignmentCount">Number of active approved assignments.</param>
/// <param name="ApprovedExceptionCount">Number of formal approved workload exceptions.</param>
/// <param name="AsOfTimestamp">Timestamp when this snapshot was last synchronized or recalculated.</param>
/// <param name="CreatedAt">Timestamp when this snapshot record was created in UTC.</param>
public sealed record TeacherWorkloadCommitmentDto(
    Guid Id,
    Guid TeacherProfileId,
    string TeacherNumber,
    string FullName,
    string? DisplayName,
    string? CampusAffiliation,
    string Standing,
    Guid AcademicPeriodId,
    Guid WorkloadPolicyId,
    string? PolicyCode,
    string? PolicyName,
    decimal MaxStandardCapacity,
    decimal WarningThreshold,
    decimal ProhibitedThreshold,
    decimal PlannedWorkloadHours,
    decimal CommittedWorkloadHours,
    decimal MaxCapacityLimitHours,
    decimal RemainingCapacityHours,
    string Status,
    int ActiveAssignmentCount,
    int ApprovedExceptionCount,
    DateTimeOffset AsOfTimestamp,
    DateTimeOffset CreatedAt);
