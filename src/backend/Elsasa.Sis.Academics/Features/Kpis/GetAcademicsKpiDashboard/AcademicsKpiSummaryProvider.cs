namespace Elsasa.Sis.Academics.Features.Kpis.GetAcademicsKpiDashboard;

using Elsasa.Sis.Contracts.Kpis;

/// <summary>
/// Cross-domain KPI summary provider for the Academics domain consumed by Level 1 executive orchestration.
/// </summary>
public sealed class AcademicsKpiSummaryProvider(
    GetAcademicsKpiDashboardHandler dashboardHandler) : IDomainKpiSummaryProvider
{
    /// <inheritdoc />
    public string DomainKey => "academics";

    /// <inheritdoc />
    public async Task<DomainKpiSummaryResult> GetSummaryAsync(
        ResolvedKpiContextDto context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var computed = await dashboardHandler.ComputeMetricsAsync(context, cancellationToken);
        var aca01 = computed.HeadlineKpis.First(k => k.Code == "ACA-01");
        var aca03 = computed.HeadlineKpis.First(k => k.Code == "ACA-03");

        var warningCount = computed.AttentionSignals.Count(s =>
            string.Equals(s.Severity, "warning", StringComparison.OrdinalIgnoreCase));
        var criticalCount = computed.AttentionSignals.Count(s =>
            string.Equals(s.Severity, "critical", StringComparison.OrdinalIgnoreCase));

        var healthStatus = DetermineHealthStatus(aca01.Status, warningCount, criticalCount);
        var statusLabel = healthStatus switch
        {
            "critical" => "Critical",
            "warning" => "Needs Attention",
            _ => "Healthy"
        };

        var domainHealth = new DomainHealthSummaryDto(
            DomainKey: DomainKey,
            DomainName: "Academics",
            Status: healthStatus,
            StatusLabel: statusLabel,
            HeadlineLabel: "Good Academic Standing",
            HeadlineValue: aca01.FormattedValue,
            SecondaryLabel: "Period Attendance",
            SecondaryValue: aca03.FormattedValue,
            WarningCount: warningCount,
            CriticalCount: criticalCount,
            DashboardRoute: "/dashboard/academics",
            IsAccessible: true);

        return new DomainKpiSummaryResult(
            SortOrder: 2,
            HeadlineKpi: aca01,
            DomainHealth: domainHealth,
            AttentionSignals: computed.AttentionSignals);
    }

    private static string DetermineHealthStatus(string headlineStatus, int warningCount, int criticalCount)
    {
        if (criticalCount > 0 || string.Equals(headlineStatus, "critical", StringComparison.OrdinalIgnoreCase))
        {
            return "critical";
        }

        if (warningCount > 0 || string.Equals(headlineStatus, "warning", StringComparison.OrdinalIgnoreCase))
        {
            return "warning";
        }

        return "healthy";
    }
}
