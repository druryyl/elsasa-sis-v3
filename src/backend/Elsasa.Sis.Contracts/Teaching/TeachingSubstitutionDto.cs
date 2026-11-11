namespace Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Strongly-typed contract DTO representing a temporary substitute teacher coverage arrangement.
/// </summary>
/// <param name="Id">The unique identifier of the substitution.</param>
/// <param name="SubstitutionNumber">The authoritative unique substitution identifier (e.g. 'SUB-2026-0015').</param>
/// <param name="OriginalAssignmentId">The assignment being temporarily covered.</param>
/// <param name="OriginalTeacherId">The absent primary teacher.</param>
/// <param name="SubstituteTeacherId">The covering substitute teacher.</param>
/// <param name="CoverageStartDate">Start date of temporary coverage.</param>
/// <param name="CoverageEndDate">End date of temporary coverage.</param>
/// <param name="CoverageScopeType">Scope of coverage ('full', 'partial').</param>
/// <param name="PartialScopeDetails">Optional specifics when scope is partial.</param>
/// <param name="Reason">Reason for absence (e.g. 'medical_leave', 'official_duty').</param>
/// <param name="WorkloadImpactHours">Temporary workload effort transferred to substitute.</param>
/// <param name="Status">Substitution lifecycle status ('proposed', 'approved', 'active', 'completed', 'cancelled').</param>
/// <param name="HandoverNotes">Optional instructional notes from primary teacher.</param>
/// <param name="ApprovedBy">Coordinator (under Resolution 3.8 delegation) or Authority who approved the arrangement.</param>
/// <param name="ApprovedDate">Date when substitution was approved.</param>
/// <param name="CreatedAt">Timestamp when this substitution was created in UTC.</param>
public sealed record TeachingSubstitutionDto(
    Guid Id,
    string SubstitutionNumber,
    Guid OriginalAssignmentId,
    Guid OriginalTeacherId,
    Guid SubstituteTeacherId,
    DateOnly CoverageStartDate,
    DateOnly CoverageEndDate,
    string CoverageScopeType,
    string? PartialScopeDetails,
    string Reason,
    decimal WorkloadImpactHours,
    string Status,
    string? HandoverNotes,
    string ApprovedBy,
    DateOnly ApprovedDate,
    DateTimeOffset CreatedAt);
