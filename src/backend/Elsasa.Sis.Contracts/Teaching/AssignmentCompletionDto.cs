namespace Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Strongly-typed contract DTO representing formal conclusion of a teaching assignment and capacity release.
/// </summary>
/// <param name="Id">The unique identifier of the completion record.</param>
/// <param name="TeachingAssignmentId">The concluded teaching assignment.</param>
/// <param name="CompletionDate">Date of assignment completion.</param>
/// <param name="AcademicConfirmationReceived">Indicates whether Academics confirmed final grade submission.</param>
/// <param name="AcademicConfirmationSource">Reference to confirming system actor or document.</param>
/// <param name="HandoverConfirmed">Indicates whether instructional handover was confirmed.</param>
/// <param name="UnresolvedItemsStatus">Status of open items ('none', 'resolved', 'escalated').</param>
/// <param name="UnresolvedItemsDescription">Optional description of unresolved academic items.</param>
/// <param name="CapacityReleasedDate">Date when committed capacity was returned to teacher pool.</param>
/// <param name="CompletedBy">Coordinator who executed the conclusion.</param>
/// <param name="Status">Completion status ('completed', 'completed_with_exceptions').</param>
/// <param name="Notes">Optional conclusion notes.</param>
/// <param name="CreatedAt">Timestamp when this completion record was created in UTC.</param>
public sealed record AssignmentCompletionDto(
    Guid Id,
    Guid TeachingAssignmentId,
    DateOnly CompletionDate,
    bool AcademicConfirmationReceived,
    string? AcademicConfirmationSource,
    bool HandoverConfirmed,
    string UnresolvedItemsStatus,
    string? UnresolvedItemsDescription,
    DateOnly CapacityReleasedDate,
    string CompletedBy,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAt);
