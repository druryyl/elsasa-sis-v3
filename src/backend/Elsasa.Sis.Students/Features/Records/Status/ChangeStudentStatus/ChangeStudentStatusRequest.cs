namespace Elsasa.Sis.Students.Features.Records.Status.ChangeStudentStatus;

/// <summary>
/// Command payload for authoritative operational student status change (GAP-005, TD-05).
/// </summary>
/// <param name="Status">Target student status ('active' or 'enrolled' in Stage 1).</param>
/// <param name="EffectiveDate">Effective date of the status transition.</param>
/// <param name="Reason">Optional contextual justification for the status change.</param>
/// <param name="AuthorizedBy">Authorized personnel username or identifier.</param>
/// <param name="DecisionReference">Official decree or decision reference code.</param>
public sealed record ChangeStudentStatusRequest(
    string Status,
    DateOnly EffectiveDate,
    string? Reason,
    string? AuthorizedBy,
    string? DecisionReference);
