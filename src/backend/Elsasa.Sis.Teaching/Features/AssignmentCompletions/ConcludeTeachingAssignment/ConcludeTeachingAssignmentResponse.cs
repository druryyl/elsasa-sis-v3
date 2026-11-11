namespace Elsasa.Sis.Teaching.Features.AssignmentCompletions.ConcludeTeachingAssignment;

using Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Response model returned after concluding a teaching assignment (TD-01, TD-04, P4-S13).
/// </summary>
/// <param name="Id">The unique identifier of the completion record.</param>
/// <param name="TeachingAssignmentId">The concluded teaching assignment identifier.</param>
/// <param name="AssignmentNumber">The authoritative assignment number.</param>
/// <param name="CompletionDate">Date of assignment completion.</param>
/// <param name="AcademicConfirmationReceived">Whether final grade submission was confirmed.</param>
/// <param name="AcademicConfirmationSource">Source of academic confirmation.</param>
/// <param name="HandoverConfirmed">Whether instructional handover was confirmed.</param>
/// <param name="UnresolvedItemsStatus">Status of unresolved items ('none', 'resolved', 'escalated').</param>
/// <param name="UnresolvedItemsDescription">Optional description of unresolved academic items.</param>
/// <param name="CapacityReleasedDate">Date when committed capacity was returned to teacher pool.</param>
/// <param name="ReleasedWorkloadHours">Committed workload hours released back to teacher pool.</param>
/// <param name="CompletedBy">Authority/coordinator who concluded the assignment.</param>
/// <param name="Status">Completion status ('completed', 'completed_with_exceptions').</param>
/// <param name="Notes">Optional conclusion notes.</param>
/// <param name="CreatedAt">Timestamp when this completion record was created in UTC.</param>
public sealed record ConcludeTeachingAssignmentResponse(
    Guid Id,
    Guid TeachingAssignmentId,
    string AssignmentNumber,
    DateOnly CompletionDate,
    bool AcademicConfirmationReceived,
    string? AcademicConfirmationSource,
    bool HandoverConfirmed,
    string UnresolvedItemsStatus,
    string? UnresolvedItemsDescription,
    DateOnly CapacityReleasedDate,
    decimal ReleasedWorkloadHours,
    string CompletedBy,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAt)
{
    /// <summary>
    /// Converts this response to a standard <see cref="AssignmentCompletionDto"/>.
    /// </summary>
    public AssignmentCompletionDto ToDto() => new(
        Id,
        TeachingAssignmentId,
        CompletionDate,
        AcademicConfirmationReceived,
        AcademicConfirmationSource,
        HandoverConfirmed,
        UnresolvedItemsStatus,
        UnresolvedItemsDescription,
        CapacityReleasedDate,
        CompletedBy,
        Status,
        Notes,
        CreatedAt);
}
