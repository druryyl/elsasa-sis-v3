namespace Elsasa.Sis.Academics.Features.AcademicInterventions.UpdateInterventionStatus;

/// <summary>
/// Request payload for updating the status of an academic intervention (UC-ACD-11, P4-S10, TD-04).
/// </summary>
/// <param name="Status">The new lifecycle status ('planned', 'in_progress', 'completed', 'cancelled').</param>
/// <param name="OutcomeNotes">Optional outcome narrative, mandatory when completing an intervention.</param>
public sealed record UpdateInterventionStatusRequest(
    string Status,
    string? OutcomeNotes = null);
