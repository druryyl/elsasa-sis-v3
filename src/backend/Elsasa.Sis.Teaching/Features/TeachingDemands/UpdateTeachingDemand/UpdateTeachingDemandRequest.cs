namespace Elsasa.Sis.Teaching.Features.TeachingDemands.UpdateTeachingDemand;

/// <summary>
/// Request payload to update teaching capacity demand parameters while in mutable status (TD-02, P2-S04).
/// </summary>
/// <param name="SourceWorkLabel">Descriptive human-readable label of the source work item.</param>
/// <param name="RequiredCapabilityCode">Required teaching capability taxonomy code.</param>
/// <param name="RequiredAssignmentRoleId">Required assignment role identifier.</param>
/// <param name="ExpectedWorkloadHours">Estimated workload hours needed to satisfy this demand.</param>
/// <param name="RequestedStartDate">Requested start date for teaching delivery.</param>
/// <param name="RequestedEndDate">Requested end date for teaching delivery.</param>
/// <param name="WorkloadUnit">Measurement unit for workload (default: 'Teaching Hours').</param>
/// <param name="RequiresAssessmentAuthority">Indicates whether staffing requires grading/assessment rights.</param>
/// <param name="CampusScope">Campus boundary for staffing delivery.</param>
/// <param name="RequiredTeachersCount">Number of teachers required to satisfy this demand.</param>
/// <param name="ClarificationNotes">Optional notes addressing clarification requests.</param>
public sealed record UpdateTeachingDemandRequest(
    string SourceWorkLabel,
    string RequiredCapabilityCode,
    Guid RequiredAssignmentRoleId,
    decimal ExpectedWorkloadHours,
    DateOnly RequestedStartDate,
    DateOnly RequestedEndDate,
    string? WorkloadUnit = null,
    bool RequiresAssessmentAuthority = true,
    string? CampusScope = null,
    int RequiredTeachersCount = 1,
    string? ClarificationNotes = null);
