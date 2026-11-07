namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Strongly-typed contract DTO representing an immutable approval decision for a student learning plan.
/// </summary>
/// <param name="Id">The unique identifier of the approval record.</param>
/// <param name="PlanVersion">The version of the student learning plan at the time of approval.</param>
/// <param name="ApprovedBy">Username or identifier of the approving academic authority.</param>
/// <param name="ApprovedDate">The date when approval was granted.</param>
/// <param name="ApprovalType">The approval type ('initial_approval' or 'revision_approval').</param>
/// <param name="Notes">Optional governance or commendation notes recorded with approval.</param>
public sealed record SlpApprovalDto(
    Guid Id,
    int PlanVersion,
    string ApprovedBy,
    DateOnly ApprovedDate,
    string ApprovalType,
    string? Notes);
