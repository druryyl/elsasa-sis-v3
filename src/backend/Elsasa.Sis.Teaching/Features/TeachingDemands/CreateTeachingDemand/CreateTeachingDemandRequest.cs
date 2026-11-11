namespace Elsasa.Sis.Teaching.Features.TeachingDemands.CreateTeachingDemand;

/// <summary>
/// Request payload to ingest a new teaching capacity demand from an academic, student cohort, or extracurricular source (TD-02, P2-S04).
/// </summary>
/// <param name="SourceDomain">The domain originating the demand (e.g. 'Academics', 'Students', 'Development').</param>
/// <param name="SourceWorkReference">Opaque reference identifier to the external entity needing staffing.</param>
/// <param name="SourceWorkLabel">Descriptive human-readable label of the source work item.</param>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="RequiredCapabilityCode">Required teaching capability taxonomy code.</param>
/// <param name="RequiredAssignmentRoleId">Required assignment role identifier.</param>
/// <param name="ExpectedWorkloadHours">Estimated workload hours needed to satisfy this demand.</param>
/// <param name="RequestedStartDate">Requested start date for teaching delivery.</param>
/// <param name="RequestedEndDate">Requested end date for teaching delivery.</param>
/// <param name="WorkloadUnit">Measurement unit for workload (default: 'Teaching Hours').</param>
/// <param name="RequiresAssessmentAuthority">Indicates whether staffing requires grading/assessment rights (default: true).</param>
/// <param name="CampusScope">Campus boundary for staffing delivery (default: 'Kampus Utama').</param>
/// <param name="RequiredTeachersCount">Number of teachers required to satisfy this demand (default: 1).</param>
/// <param name="DemandCode">Optional custom demand code. When null or whitespace, generated automatically.</param>
/// <param name="ClarificationNotes">Optional initial notes or context for the staffing coordinator.</param>
public sealed record CreateTeachingDemandRequest(
    string SourceDomain,
    Guid SourceWorkReference,
    string SourceWorkLabel,
    Guid AcademicPeriodId,
    string RequiredCapabilityCode,
    Guid RequiredAssignmentRoleId,
    decimal ExpectedWorkloadHours,
    DateOnly RequestedStartDate,
    DateOnly RequestedEndDate,
    string? WorkloadUnit = null,
    bool RequiresAssessmentAuthority = true,
    string? CampusScope = null,
    int RequiredTeachersCount = 1,
    string? DemandCode = null,
    string? ClarificationNotes = null);
