namespace Elsasa.Sis.Teaching.Features.Kpis.GetTeachingKpiDashboard;

using Elsasa.Sis.Contracts.Kpis;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Cross-domain summary provider exposing Teaching domain headline KPI (<c>TCH-01</c>),
/// domain health status, and active operational exception signals to the Level 1 Executive Overview.
/// </summary>
public sealed class TeachingKpiSummaryProvider(
    ISqlConnectionFactory connectionFactory) : IDomainKpiSummaryProvider
{
    /// <inheritdoc />
    public string DomainKey => "teaching";

    /// <inheritdoc />
    public async Task<DomainKpiSummaryResult> GetSummaryAsync(
        ResolvedKpiContextDto context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var snapshot = await TeachingKpiQueries.QueryAsync(connectionFactory, context, cancellationToken);
        var headlineKpi = snapshot.HeadlineKpis[0];

        var warningCount = snapshot.AttentionSignals.Count(s => s.Severity == "warning");
        var criticalCount = snapshot.AttentionSignals.Count(s => s.Severity == "critical");

        var domainStatus = criticalCount > 0 || headlineKpi.Status == "critical"
            ? "critical"
            : warningCount > 0 || headlineKpi.Status == "warning"
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
            DomainName: "Teaching",
            Status: domainStatus,
            StatusLabel: statusLabel,
            HeadlineLabel: "Demand Coverage",
            HeadlineValue: headlineKpi.FormattedValue,
            SecondaryLabel: "Open Demands / Breaches",
            SecondaryValue: $"{snapshot.OpenDemandsCount} open · {snapshot.BreachedWorkloadCount} breached",
            WarningCount: warningCount,
            CriticalCount: criticalCount,
            DashboardRoute: "/dashboard/teaching",
            IsAccessible: true);

        return new DomainKpiSummaryResult(
            SortOrder: 4,
            HeadlineKpi: headlineKpi,
            DomainHealth: domainHealth,
            AttentionSignals: snapshot.AttentionSignals);
    }
}
