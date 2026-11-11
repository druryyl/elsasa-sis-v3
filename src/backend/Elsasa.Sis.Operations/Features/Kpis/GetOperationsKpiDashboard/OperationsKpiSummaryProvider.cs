namespace Elsasa.Sis.Operations.Features.Kpis.GetOperationsKpiDashboard;

using Elsasa.Sis.Contracts.Kpis;

/// <summary>
/// Cross-domain KPI summary provider for the Operations domain consumed by Level 1 executive orchestration.
/// </summary>
public sealed class OperationsKpiSummaryProvider(
    GetOperationsKpiDashboardHandler dashboardHandler) : IDomainKpiSummaryProvider
{
    /// <inheritdoc />
    public string DomainKey => "operations";

    /// <inheritdoc />
    public async Task<DomainKpiSummaryResult> GetSummaryAsync(
        ResolvedKpiContextDto context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var computed = await dashboardHandler.ComputeMetricsAsync(context, cancellationToken);
        var ops01 = computed.HeadlineKpis.First(k => k.Code == "OPS-01");
        var ops02 = computed.HeadlineKpis.First(k => k.Code == "OPS-02");

        var warningCount = computed.AttentionSignals.Count(s =>
            string.Equals(s.Severity, "warning", StringComparison.OrdinalIgnoreCase));
        var criticalCount = computed.AttentionSignals.Count(s =>
            string.Equals(s.Severity, "critical", StringComparison.OrdinalIgnoreCase));

        var healthStatus = DetermineHealthStatus(ops01.Status, ops02.Status, warningCount, criticalCount);
        var statusLabel = healthStatus switch
        {
            "critical" => "Critical",
            "warning" => "Needs Attention",
            _ => "Healthy"
        };

        var domainHealth = new DomainHealthSummaryDto(
            DomainKey: DomainKey,
            DomainName: "Operations",
            Status: healthStatus,
            StatusLabel: statusLabel,
            HeadlineLabel: "Calendar Readiness",
            HeadlineValue: ops01.FormattedValue,
            SecondaryLabel: "Closure Days",
            SecondaryValue: ops02.FormattedValue,
            WarningCount: warningCount,
            CriticalCount: criticalCount,
            DashboardRoute: "/dashboard/operations",
            IsAccessible: true);

        return new DomainKpiSummaryResult(
            SortOrder: 5,
            HeadlineKpi: ops01,
            DomainHealth: domainHealth,
            AttentionSignals: computed.AttentionSignals);
    }

    private static string DetermineHealthStatus(
        string ops01Status,
        string ops02Status,
        int warningCount,
        int criticalCount)
    {
        if (criticalCount > 0
            || string.Equals(ops01Status, "critical", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ops02Status, "critical", StringComparison.OrdinalIgnoreCase))
        {
            return "critical";
        }

        if (warningCount > 0
            || string.Equals(ops01Status, "warning", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ops02Status, "warning", StringComparison.OrdinalIgnoreCase))
        {
            return "warning";
        }

        return "healthy";
    }
}
