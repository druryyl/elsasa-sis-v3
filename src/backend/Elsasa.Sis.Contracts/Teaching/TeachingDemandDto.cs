namespace Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Strongly-typed contract DTO representing a formal teaching capacity request originating from
/// Academics, Students, or Development.
/// </summary>
/// <param name="Id">The unique identifier of the teaching demand.</param>
/// <param name="DemandCode">The unique human-readable demand code (e.g. 'DEM-2026-0001').</param>
/// <param name="SourceDomain">The originating domain ('Academics', 'Students', 'Development').</param>
/// <param name="SourceWorkReference">Opaque identifier of the external entity requiring staffing.</param>
/// <param name="SourceWorkLabel">Descriptive label of the source work item (e.g. 'Matematika SD Kelas 4').</param>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="RequiredCapabilityCode">Required teaching capability taxonomy code.</param>
/// <param name="RequiredAssignmentRoleId">Required assignment role identifier.</param>
/// <param name="RequiredAssignmentRoleName">Optional display name of the required assignment role.</param>
/// <param name="ExpectedWorkloadHours">Estimated workload hours needed to satisfy this demand.</param>
/// <param name="WorkloadUnit">Measurement unit for workload (default: 'Teaching Hours').</param>
/// <param name="RequiresAssessmentAuthority">Indicates whether staffing requires grading/assessment rights.</param>
/// <param name="RequestedStartDate">Requested start date for teaching delivery.</param>
/// <param name="RequestedEndDate">Requested end date for teaching delivery.</param>
/// <param name="CampusScope">Campus boundary for staffing delivery (e.g. 'Kampus Utama').</param>
/// <param name="RequiredTeachersCount">Number of teachers required to satisfy this demand.</param>
/// <param name="Status">Demand lifecycle status ('received', 'accepted_for_staffing', 'returned_for_clarification', 'changed', 'cancelled', 'declined', 'staffed').</param>
/// <param name="RequestedBy">Identity of requesting user or system actor.</param>
/// <param name="RequestedAt">Timestamp when demand was submitted in UTC.</param>
/// <param name="DeclineReason">Optional justification if demand was declined.</param>
/// <param name="ClarificationNotes">Optional notes when returned for clarification.</param>
/// <param name="CreatedAt">Timestamp when demand record was created in UTC.</param>
public sealed record TeachingDemandDto(
    Guid Id,
    string DemandCode,
    string SourceDomain,
    Guid SourceWorkReference,
    string SourceWorkLabel,
    Guid AcademicPeriodId,
    string RequiredCapabilityCode,
    Guid RequiredAssignmentRoleId,
    string? RequiredAssignmentRoleName,
    decimal ExpectedWorkloadHours,
    string WorkloadUnit,
    bool RequiresAssessmentAuthority,
    DateOnly RequestedStartDate,
    DateOnly RequestedEndDate,
    string CampusScope,
    int RequiredTeachersCount,
    string Status,
    string RequestedBy,
    DateTimeOffset RequestedAt,
    string? DeclineReason,
    string? ClarificationNotes,
    DateTimeOffset CreatedAt);
