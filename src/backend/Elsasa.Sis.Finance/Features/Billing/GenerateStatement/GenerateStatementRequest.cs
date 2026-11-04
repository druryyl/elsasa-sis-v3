namespace Elsasa.Sis.Finance.Features.Billing.GenerateStatement;

/// <summary>
/// Command payload for compiling a periodic financial statement across a specified date range (FIN-WF-04, UC-FIN-03, GAP-005, TD-05).
/// </summary>
/// <param name="StudentId">Target student identity UUID.</param>
/// <param name="BillingPeriodStart">Start date of statement activity.</param>
/// <param name="BillingPeriodEnd">End date of statement activity.</param>
/// <param name="Notes">Optional statement notes.</param>
public sealed record GenerateStatementRequest(
    Guid StudentId,
    DateOnly BillingPeriodStart,
    DateOnly BillingPeriodEnd,
    string? Notes = null);
