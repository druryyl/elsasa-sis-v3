namespace Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Strongly-typed contract DTO representing an authoritative teaching assignment connecting an eligible faculty member
/// to an academic or advisory responsibility.
/// </summary>
/// <param name="Id">The unique identifier of the teaching assignment.</param>
/// <param name="AssignmentNumber">The authoritative unique assignment identifier (e.g. 'ASN-2026-0045').</param>
/// <param name="TeachingDemandId">The originating teaching demand identifier.</param>
/// <param name="TeacherProfileId">The assigned teacher profile identifier.</param>
/// <param name="AssignmentRoleId">The assigned role identifier.</param>
/// <param name="AssignmentRoleName">Optional display name of the assigned role.</param>
/// <param name="AssignmentScope">Scope category ('learning_unit', 'cohort_group', 'individual_student', 'extracurricular_group').</param>
/// <param name="ScopeReferenceId">The external scope reference identifier.</param>
/// <param name="ScopeLabel">Human-readable descriptive label for the assigned scope.</param>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="EffectiveStartDate">The date on which this assignment takes effect.</param>
/// <param name="EffectiveEndDate">The date on which this assignment concludes.</param>
/// <param name="WorkloadCommitmentValue">Weekly committed workload effort.</param>
/// <param name="WorkloadUnit">Measurement unit for workload (default: 'Teaching Hours').</param>
/// <param name="Status">Assignment lifecycle status ('proposed', 'approved', 'rejected', 'withdrawn', 'returned_for_revision', 'suspended', 'cancelled', 'completed').</param>
/// <param name="ApprovedBy">Optional identity of authority who approved the assignment.</param>
/// <param name="ApprovedDate">Optional date when approval was granted.</param>
/// <param name="TeacherAcknowledged">Indicates whether the assigned teacher acknowledged this assignment.</param>
/// <param name="TeacherAcknowledgedDate">Optional date when teacher acknowledged.</param>
/// <param name="PredecessorAssignmentId">Optional link to predecessor assignment if created via permanent reassignment.</param>
/// <param name="Notes">Optional administrative notes.</param>
/// <param name="CreatedAt">Timestamp when this assignment record was created in UTC.</param>
public sealed record TeachingAssignmentDto(
    Guid Id,
    string AssignmentNumber,
    Guid TeachingDemandId,
    Guid TeacherProfileId,
    Guid AssignmentRoleId,
    string? AssignmentRoleName,
    string AssignmentScope,
    Guid ScopeReferenceId,
    string ScopeLabel,
    Guid AcademicPeriodId,
    DateOnly EffectiveStartDate,
    DateOnly EffectiveEndDate,
    decimal WorkloadCommitmentValue,
    string WorkloadUnit,
    string Status,
    string? ApprovedBy,
    DateOnly? ApprovedDate,
    bool TeacherAcknowledged,
    DateOnly? TeacherAcknowledgedDate,
    Guid? PredecessorAssignmentId,
    string? Notes,
    DateTimeOffset CreatedAt);
