namespace Elsasa.Sis.Teaching.Features.Substitutions.ApproveSubstitutionException;

/// <summary>
/// Response payload after approving an escalated emergency substitution exception (TD-05, P4-S11).
/// </summary>
public sealed record ApproveSubstitutionExceptionResponse(
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
    DateTimeOffset UpdatedAt);
