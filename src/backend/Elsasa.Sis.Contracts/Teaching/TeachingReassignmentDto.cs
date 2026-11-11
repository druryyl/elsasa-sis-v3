namespace Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Strongly-typed contract DTO representing a permanent transfer of teaching responsibility to a new teacher.
/// </summary>
/// <param name="Id">The unique identifier of the reassignment.</param>
/// <param name="ReassignmentNumber">The authoritative unique reassignment identifier (e.g. 'REA-2026-0003').</param>
/// <param name="OriginalAssignmentId">The assignment being transferred.</param>
/// <param name="PredecessorTeacherId">The outgoing teacher.</param>
/// <param name="NewTeacherId">The incoming replacement teacher.</param>
/// <param name="ReplacementAssignmentId">Optional link to new replacement assignment created upon approval.</param>
/// <param name="EffectiveDate">The prospective date when reassignment becomes active.</param>
/// <param name="ReassignmentType">Type of transfer (e.g. 'permanent_replacement', 'restructuring').</param>
/// <param name="Reason">Administrative justification for permanent transfer.</param>
/// <param name="RequiresHandover">Indicates whether syllabus/materials handover is mandated.</param>
/// <param name="HandoverStatus">Handover progression status ('pending', 'completed', 'waived').</param>
/// <param name="HandoverCompletedDate">Optional date when handover was verified.</param>
/// <param name="HandoverNotes">Optional syllabus handover commentary.</param>
/// <param name="Status">Reassignment status ('proposed', 'approved', 'executed', 'cancelled').</param>
/// <param name="ApprovedBy">Teaching Authority who approved the transfer.</param>
/// <param name="ApprovedDate">Date when approval was granted.</param>
/// <param name="CreatedAt">Timestamp when this reassignment was recorded in UTC.</param>
public sealed record TeachingReassignmentDto(
    Guid Id,
    string ReassignmentNumber,
    Guid OriginalAssignmentId,
    Guid PredecessorTeacherId,
    Guid NewTeacherId,
    Guid? ReplacementAssignmentId,
    DateOnly EffectiveDate,
    string ReassignmentType,
    string Reason,
    bool RequiresHandover,
    string HandoverStatus,
    DateOnly? HandoverCompletedDate,
    string? HandoverNotes,
    string Status,
    string ApprovedBy,
    DateOnly ApprovedDate,
    DateTimeOffset CreatedAt);
