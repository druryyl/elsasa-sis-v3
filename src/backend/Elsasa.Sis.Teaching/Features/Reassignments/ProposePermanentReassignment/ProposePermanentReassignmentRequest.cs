namespace Elsasa.Sis.Teaching.Features.Reassignments.ProposePermanentReassignment;

/// <summary>
/// Command request to propose a permanent teaching responsibility reassignment (TD-01, TD-04, P4-S12).
/// </summary>
/// <param name="OriginalAssignmentId">The assignment being permanently transferred.</param>
/// <param name="NewTeacherId">The incoming replacement teacher.</param>
/// <param name="EffectiveDate">Prospective date when reassignment becomes active.</param>
/// <param name="Reason">Administrative justification for permanent transfer.</param>
/// <param name="RequiresHandover">Whether syllabus/materials handover is mandated. Default true.</param>
/// <param name="ReassignmentType">Type of transfer (default 'permanent_transfer').</param>
public sealed record ProposePermanentReassignmentRequest(
    Guid OriginalAssignmentId,
    Guid NewTeacherId,
    DateOnly EffectiveDate,
    string Reason,
    bool RequiresHandover = true,
    string? ReassignmentType = null);
