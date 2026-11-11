namespace Elsasa.Sis.Teaching.Features.AssignmentCompletions.ConcludeTeachingAssignment;

/// <summary>
/// Request model for concluding an approved teaching assignment and releasing committed capacity (TD-01, TD-04, P4-S13).
/// </summary>
/// <param name="CompletionDate">Date of formal assignment completion.</param>
/// <param name="AcademicConfirmationReceived">Whether final grade submissions and academic deliverables are confirmed.</param>
/// <param name="AcademicConfirmationSource">Source of academic confirmation (e.g. Dean confirmation or grade report link).</param>
/// <param name="HandoverConfirmed">Whether instructional handover and materials return are confirmed.</param>
/// <param name="UnresolvedItemsStatus">Status of unresolved items ('none', 'minor_pending', 'escalated_defect', 'resolved', 'escalated').</param>
/// <param name="UnresolvedItemsDescription">Optional description of unresolved academic matters.</param>
/// <param name="Notes">Optional notes regarding assignment conclusion.</param>
public sealed record ConcludeTeachingAssignmentRequest(
    DateOnly CompletionDate,
    bool AcademicConfirmationReceived,
    string? AcademicConfirmationSource,
    bool HandoverConfirmed,
    string? UnresolvedItemsStatus,
    string? UnresolvedItemsDescription,
    string? Notes);
