namespace Elsasa.Sis.Students.Features.Kpis.GetStudentsKpiDashboard;

using Elsasa.Sis.Contracts.Kpis;

/// <summary>
/// Exposes Students domain headline KPI (<c>STD-01</c>), health summary, and active exception signals
/// to the Level 1 Executive Dashboard via <see cref="IDomainKpiSummaryProvider"/>.
/// </summary>
public sealed class StudentsKpiSummaryProvider(
    GetStudentsKpiDashboardHandler handler) : IDomainKpiSummaryProvider
{
    /// <inheritdoc />
    public string DomainKey => "students";

    /// <inheritdoc />
    public async Task<DomainKpiSummaryResult> GetSummaryAsync(
        ResolvedKpiContextDto context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var snapshot = await handler.ComputeSnapshotAsync(context, cancellationToken);
        var signals = snapshot.AttentionSignals;

        var warningCount = signals.Count(s => string.Equals(s.Severity, "warning", StringComparison.OrdinalIgnoreCase));
        var criticalCount = signals.Count(s => string.Equals(s.Severity, "critical", StringComparison.OrdinalIgnoreCase));

        var hasCriticalMetric = snapshot.HeadlineKpis.Any(k => string.Equals(k.Status, "critical", StringComparison.OrdinalIgnoreCase));
        var hasWarningMetric = snapshot.HeadlineKpis.Any(k => string.Equals(k.Status, "warning", StringComparison.OrdinalIgnoreCase));

        string healthStatus;
        string statusLabel;
        if (criticalCount > 0 || hasCriticalMetric)
        {
            healthStatus = "critical";
            statusLabel = "Critical";
        }
        else if (warningCount > 0 || hasWarningMetric)
        {
            healthStatus = "warning";
            statusLabel = "Needs Attention";
        }
        else
        {
            healthStatus = "healthy";
            statusLabel = "Healthy";
        }

        var domainHealth = new DomainHealthSummaryDto(
            DomainKey: DomainKey,
            DomainName: "Students",
            Status: healthStatus,
            StatusLabel: statusLabel,
            HeadlineLabel: "Active Students",
            HeadlineValue: snapshot.Std01.FormattedValue,
            SecondaryLabel: "Attrition Rate",
            SecondaryValue: snapshot.Std06.FormattedValue,
            WarningCount: warningCount,
            CriticalCount: criticalCount,
            DashboardRoute: "/dashboard/students",
            IsAccessible: true);

        return new DomainKpiSummaryResult(
            SortOrder: 1,
            HeadlineKpi: snapshot.Std01,
            DomainHealth: domainHealth,
            AttentionSignals: signals);
    }
}
