namespace Elsasa.Sis.Teaching.Features.Substitutions.ArrangeEmergencySubstitution;

/// <summary>
/// Response payload after arranging an emergency substitution (TD-05, P4-S11).
/// </summary>
/// <param name="Id">The unique identifier of the substitution.</param>
/// <param name="SubstitutionNumber">The authoritative unique substitution number (e.g. 'SUB-2026-0001').</param>
/// <param name="OriginalAssignmentId">The assignment being temporarily covered.</param>
/// <param name="OriginalTeacherId">The absent primary teacher identifier.</param>
/// <param name="SubstituteTeacherId">The covering substitute teacher identifier.</param>
/// <param name="CoverageStartDate">Start date of coverage.</param>
/// <param name="CoverageEndDate">End date of coverage.</param>
/// <param name="CoverageScopeType">Scope of coverage ('full' or 'partial').</param>
/// <param name="PartialScopeDetails">Optional particulars when scope is partial.</param>
/// <param name="Reason">Reason for absence.</param>
/// <param name="WorkloadImpactHours">Temporary workload effort transferred to substitute.</param>
/// <param name="Status">Substitution lifecycle status ('approved' for direct delegation, 'proposed' for escalated exception).</param>
/// <param name="HandoverNotes">Optional instructional notes.</param>
/// <param name="ApprovedBy">Coordinator or 'PENDING'.</param>
/// <param name="ApprovedDate">Date when substitution was approved or proposed.</param>
/// <param name="IsDelegatedApproval">True if directly committed under Pre-Authorized Pool Delegation (Resolution 3.8).</param>
/// <param name="RequiresAuthorityApproval">True if escalated to Teaching Authority due to authorization gap or workload ceiling breach.</param>
/// <param name="CapacityConflictId">ID of the blocking capacity conflict raised if escalated, otherwise null.</param>
/// <param name="CreatedAt">Timestamp when created in UTC.</param>
public sealed record ArrangeEmergencySubstitutionResponse(
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
    bool IsDelegatedApproval,
    bool RequiresAuthorityApproval,
    Guid? CapacityConflictId,
    DateTimeOffset CreatedAt);
