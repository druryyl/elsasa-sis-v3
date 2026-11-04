namespace Elsasa.Sis.Finance.Features.Obligations.BatchAssessObligations;

/// <summary>
/// Command payload for executing or previewing a batch obligation assessment billing cycle (FIN-WF-02, UC-FIN-02, SCR-FIN-01, GAP-003, GAP-004, TD-04).
/// </summary>
/// <param name="ChargeRuleId">The unique identifier of the active charge rule to assess.</param>
/// <param name="AcademicPeriodId">The unique identifier of the operational/academic period.</param>
/// <param name="BillingCycleLabel">Human-readable label for the billing cycle (e.g. 'TP 2026/2027 - SPP Agustus').</param>
/// <param name="TargetCampus">Optional filter restricting candidates to a specific campus.</param>
/// <param name="TargetGrade">Optional filter restricting candidates to a specific grade.</param>
/// <param name="Mode">Assessment mode: 'preview' (dry-run candidate evaluation) or 'commit' (bulk generation).</param>
public sealed record BatchAssessObligationsRequest(
    Guid ChargeRuleId,
    Guid AcademicPeriodId,
    string BillingCycleLabel,
    string? TargetCampus = null,
    string? TargetGrade = null,
    string Mode = "preview");
