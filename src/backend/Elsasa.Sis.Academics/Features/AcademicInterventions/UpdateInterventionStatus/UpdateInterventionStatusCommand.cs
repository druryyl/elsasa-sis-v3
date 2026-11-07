namespace Elsasa.Sis.Academics.Features.AcademicInterventions.UpdateInterventionStatus;

/// <summary>
/// Command record for updating the lifecycle status of an academic intervention (UC-ACD-11, P4-S10, TD-04).
/// </summary>
/// <param name="Id">The unique identifier of the academic intervention record.</param>
/// <param name="Status">The new lifecycle status ('planned', 'in_progress', 'completed', 'cancelled').</param>
/// <param name="OutcomeNotes">The outcome summary narrative or resolution notes.</param>
public sealed record UpdateInterventionStatusCommand(
    Guid Id,
    string Status,
    string? OutcomeNotes = null);
