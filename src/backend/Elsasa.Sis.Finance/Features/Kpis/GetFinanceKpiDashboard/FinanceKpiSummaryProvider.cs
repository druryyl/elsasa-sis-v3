namespace Elsasa.Sis.Finance.Features.Kpis.GetFinanceKpiDashboard;

using Elsasa.Sis.Contracts.Kpis;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Cross-domain summary provider exposing Finance headline KPI (<c>FIN-01</c>),
/// domain health status, and active exception signals for the Level 1 Executive Overview.
/// </summary>
public sealed class FinanceKpiSummaryProvider(
    ISqlConnectionFactory connectionFactory) : IDomainKpiSummaryProvider
{
    /// <inheritdoc />
    public string DomainKey => "finance";

    /// <inheritdoc />
    public async Task<DomainKpiSummaryResult> GetSummaryAsync(
        ResolvedKpiContextDto context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var snapshot = await FinanceKpiMetricsCalculator.ComputeAsync(connectionFactory, context, cancellationToken);

        var fin01 = snapshot.HeadlineKpis.First(k => k.Code == "FIN-01");
        var fin02 = snapshot.HeadlineKpis.First(k => k.Code == "FIN-02");
        var fin03 = snapshot.HeadlineKpis.First(k => k.Code == "FIN-03");

        var criticalCount = snapshot.AttentionSignals.Count(s => s.Severity == "critical");
        var warningCount = snapshot.AttentionSignals.Count(s => s.Severity == "warning");

        var domainStatus = (criticalCount > 0 || fin01.Status == "critical" || fin03.Status == "critical")
            ? "critical"
            : (warningCount > 0 || fin01.Status == "warning" || fin02.Status == "warning" || fin03.Status == "warning")
                ? "warning"
                : "healthy";

        var statusLabel = domainStatus switch
        {
            "critical" => "Critical",
            "warning" => "Needs Attention",
            _ => "Healthy"
        };

        var domainHealth = new DomainHealthSummaryDto(
            DomainKey: DomainKey,
            DomainName: "Finance",
            Status: domainStatus,
            StatusLabel: statusLabel,
            HeadlineLabel: "Collection Rate",
            HeadlineValue: fin01.FormattedValue,
            SecondaryLabel: "Outstanding",
            SecondaryValue: fin02.FormattedValue,
            WarningCount: warningCount,
            CriticalCount: criticalCount,
            DashboardRoute: "/dashboard/finance",
            IsAccessible: true);

        return new DomainKpiSummaryResult(
            SortOrder: 3,
            HeadlineKpi: fin01,
            DomainHealth: domainHealth,
            AttentionSignals: snapshot.AttentionSignals);
    }
}
