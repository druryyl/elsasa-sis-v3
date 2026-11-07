namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Strongly-typed contract DTO representing an academic plan adjustment (accommodation, substitution, or exemption).
/// </summary>
/// <param name="Id">The unique identifier of the academic plan adjustment entry.</param>
/// <param name="AdjustmentType">The adjustment category ('accommodation', 'substitution', 'exemption').</param>
/// <param name="Description">Detailed specification of the adjustment.</param>
/// <param name="Rationale">Pedagogical, developmental, or medical justification for the adjustment.</param>
/// <param name="EffectiveDate">The date when this adjustment becomes effective.</param>
/// <param name="EndDate">Optional date when this adjustment ceases to apply.</param>
/// <param name="ApprovedBy">Username or identifier of the approving academic authority, if approved.</param>
/// <param name="ApprovedDate">The date when this adjustment was formally approved, if approved.</param>
/// <param name="Notes">Optional administrative or contextual notes.</param>
public sealed record AcademicPlanAdjustmentDto(
    Guid Id,
    string AdjustmentType,
    string Description,
    string Rationale,
    DateOnly EffectiveDate,
    DateOnly? EndDate,
    string? ApprovedBy,
    DateOnly? ApprovedDate,
    string? Notes);
