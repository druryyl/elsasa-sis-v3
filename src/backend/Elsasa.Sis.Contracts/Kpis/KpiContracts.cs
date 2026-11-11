namespace Elsasa.Sis.Contracts.Kpis;

/// <summary>
/// Global filter criteria applied across Level 1 Executive and Level 2 Domain KPI dashboards.
/// </summary>
/// <param name="AcademicYearId">Optional academic year identifier; defaults to the active academic year when omitted.</param>
/// <param name="AcademicPeriodId">Optional operational period identifier; defaults to the active semester when omitted.</param>
/// <param name="Campus">Optional campus scope (<c>campus-main</c>, <c>campus-east</c>, or <see langword="null"/> for all campuses).</param>
/// <param name="SchoolLevel">Optional school level scope (<c>ELC</c>, <c>JHLC</c>, <c>HSLC</c>, or <see langword="null"/> for all levels).</param>
public sealed record GlobalKpiFilterDto(
    Guid? AcademicYearId = null,
    Guid? AcademicPeriodId = null,
    string? Campus = null,
    string? SchoolLevel = null);

/// <summary>
/// Resolved period, comparison baseline, and normalized scope context passed to domain KPI providers and responses.
/// </summary>
/// <param name="AcademicYearId">Resolved academic year identifier.</param>
/// <param name="AcademicYearCode">Resolved academic year code (for example, <c>AY-2026-2027</c>).</param>
/// <param name="AcademicPeriodId">Resolved primary operational period identifier.</param>
/// <param name="AcademicPeriodName">Display name of the resolved primary operational period.</param>
/// <param name="PriorAcademicPeriodId">Optional prior comparison semester identifier within the same academic year.</param>
/// <param name="PriorAcademicPeriodName">Display name of the prior comparison semester, if available.</param>
/// <param name="Campus">Normalized campus scope, or <see langword="null"/> for all campuses.</param>
/// <param name="SchoolLevel">Normalized school level scope, or <see langword="null"/> for all levels.</param>
public sealed record ResolvedKpiContextDto(
    Guid AcademicYearId,
    string AcademicYearCode,
    Guid AcademicPeriodId,
    string AcademicPeriodName,
    Guid? PriorAcademicPeriodId,
    string? PriorAcademicPeriodName,
    string? Campus,
    string? SchoolLevel);

/// <summary>
/// Selectable semester period option returned to dashboard filter controls.
/// </summary>
/// <param name="Id">Unique identifier of the operational period.</param>
/// <param name="Code">Operational period code (for example, <c>SEM2-2027</c>).</param>
/// <param name="Name">Human-readable period name.</param>
/// <param name="Status">Lifecycle status (<c>active</c> or <c>closed</c>).</param>
/// <param name="AcademicYearId">Parent academic year identifier.</param>
/// <param name="AcademicYearCode">Parent academic year code.</param>
public sealed record KpiPeriodOptionDto(
    Guid Id,
    string Code,
    string Name,
    string Status,
    Guid AcademicYearId,
    string AcademicYearCode);

/// <summary>
/// Headline or domain KPI metric card with optional period-over-period comparison and Level 4 drill-down route.
/// </summary>
/// <param name="Code">Canonical KPI catalog code (for example, <c>STD-01</c>, <c>FIN-01</c>).</param>
/// <param name="Label">Display title of the KPI metric.</param>
/// <param name="Domain">Owning domain key or display name.</param>
/// <param name="Value">Raw numeric metric value.</param>
/// <param name="FormattedValue">Pre-formatted display value.</param>
/// <param name="Unit">Metric unit (<c>count</c>, <c>percentage</c>, <c>currency</c>, or <c>days</c>).</param>
/// <param name="PriorValue">Optional prior comparison period value.</param>
/// <param name="DeltaPercent">Optional numeric change relative to the prior period.</param>
/// <param name="DeltaLabel">Optional formatted delta caption (for example, <c>+4.2% vs Sem 1</c>).</param>
/// <param name="TrendDirection">Trend direction (<c>up</c>, <c>down</c>, or <c>flat</c>).</param>
/// <param name="Status">Health status (<c>healthy</c>, <c>warning</c>, <c>critical</c>, or <c>neutral</c>).</param>
/// <param name="TargetLabel">Optional target threshold description (for example, <c>Target: &gt;= 90%</c>).</param>
/// <param name="Subtitle">Optional contextual breakdown subtitle.</param>
/// <param name="DrillDownRoute">Target frontend route for Level 4 operational drill-down.</param>
public sealed record KpiMetricCardDto(
    string Code,
    string Label,
    string Domain,
    decimal Value,
    string FormattedValue,
    string Unit,
    decimal? PriorValue,
    decimal? DeltaPercent,
    string? DeltaLabel,
    string TrendDirection,
    string Status,
    string? TargetLabel,
    string? Subtitle,
    string DrillDownRoute);

/// <summary>
/// Single segment or bar within a diagnostic KPI breakdown chart.
/// </summary>
/// <param name="Key">Stable item key.</param>
/// <param name="Label">Display label for the breakdown item.</param>
/// <param name="Count">Item count or quantity.</param>
/// <param name="Percentage">Share percentage (<c>0</c> to <c>100</c>).</param>
/// <param name="Amount">Optional monetary amount associated with the item.</param>
/// <param name="FormattedAmount">Optional formatted monetary string.</param>
/// <param name="Status">Health status (<c>healthy</c>, <c>warning</c>, <c>critical</c>, or <c>neutral</c>).</param>
/// <param name="DrillDownRoute">Target frontend route for operational drill-down.</param>
public sealed record KpiBreakdownItemDto(
    string Key,
    string Label,
    decimal Count,
    decimal Percentage,
    decimal? Amount,
    string? FormattedAmount,
    string Status,
    string DrillDownRoute);

/// <summary>
/// Diagnostic breakdown visualization model rendered on Level 2 domain dashboards.
/// </summary>
/// <param name="Code">Breakdown identifier code.</param>
/// <param name="Title">Chart heading.</param>
/// <param name="Subtitle">Descriptive chart subtitle.</param>
/// <param name="ChartType">Visualization type (<c>horizontal-bar</c>, <c>stacked-bar</c>, or <c>funnel</c>).</param>
/// <param name="Items">Ordered breakdown items.</param>
public sealed record KpiBreakdownDto(
    string Code,
    string Title,
    string Subtitle,
    string ChartType,
    IReadOnlyList<KpiBreakdownItemDto> Items);

/// <summary>
/// Actionable exception signal surfaced in the Executive Needs Attention queue and domain exception panels.
/// </summary>
/// <param name="SignalCode">Canonical attention signal code (for example, <c>FIN_OVERDUE_90_PLUS</c>).</param>
/// <param name="Domain">Owning domain key or display name.</param>
/// <param name="Severity">Signal severity (<c>critical</c> or <c>warning</c>).</param>
/// <param name="Title">Concise exception headline.</param>
/// <param name="Description">Actionable description of the operational condition.</param>
/// <param name="AffectedCount">Number of affected records or entities.</param>
/// <param name="ImpactLabel">Concise impact summary label.</param>
/// <param name="ActionLabel">Call-to-action button label.</param>
/// <param name="DrillDownRoute">Target frontend route for Level 4 operational resolution.</param>
public sealed record AttentionSignalDto(
    string SignalCode,
    string Domain,
    string Severity,
    string Title,
    string Description,
    int AffectedCount,
    string ImpactLabel,
    string ActionLabel,
    string DrillDownRoute);

/// <summary>
/// Domain health summary card displayed on the Level 1 Executive Overview dashboard.
/// </summary>
/// <param name="DomainKey">Domain route/identifier key (<c>students</c>, <c>academics</c>, <c>finance</c>, <c>teaching</c>, <c>operations</c>, or <c>development</c>).</param>
/// <param name="DomainName">Human-readable domain display name.</param>
/// <param name="Status">Health status (<c>healthy</c>, <c>warning</c>, <c>critical</c>, or <c>pending_stage</c>).</param>
/// <param name="StatusLabel">Display status badge label (<c>Healthy</c>, <c>Needs Attention</c>, <c>Critical</c>, or <c>Stage 3 Pending</c>).</param>
/// <param name="HeadlineLabel">Primary metric label.</param>
/// <param name="HeadlineValue">Formatted primary metric value.</param>
/// <param name="SecondaryLabel">Secondary metric label.</param>
/// <param name="SecondaryValue">Formatted secondary metric value.</param>
/// <param name="WarningCount">Active warning signal count for the domain.</param>
/// <param name="CriticalCount">Active critical signal count for the domain.</param>
/// <param name="DashboardRoute">Level 2 domain dashboard route.</param>
/// <param name="IsAccessible">Indicates whether the current caller has permission to drill down into the domain dashboard.</param>
public sealed record DomainHealthSummaryDto(
    string DomainKey,
    string DomainName,
    string Status,
    string StatusLabel,
    string HeadlineLabel,
    string HeadlineValue,
    string SecondaryLabel,
    string SecondaryValue,
    int WarningCount,
    int CriticalCount,
    string DashboardRoute,
    bool IsAccessible = true);

/// <summary>
/// Aggregated summary payload returned by each domain's <see cref="IDomainKpiSummaryProvider"/> for Level 1 executive composition.
/// </summary>
/// <param name="SortOrder">Deterministic display order for executive layout.</param>
/// <param name="HeadlineKpi">The domain's flagship headline KPI card.</param>
/// <param name="DomainHealth">The domain's health summary card.</param>
/// <param name="AttentionSignals">Active operational exception signals raised by the domain.</param>
public sealed record DomainKpiSummaryResult(
    int SortOrder,
    KpiMetricCardDto HeadlineKpi,
    DomainHealthSummaryDto DomainHealth,
    IReadOnlyList<AttentionSignalDto> AttentionSignals);

/// <summary>
/// Response payload for the Level 1 Executive Overview endpoint (<c>GET /api/dashboard/executive</c>).
/// </summary>
/// <param name="GeneratedAtUtc">UTC timestamp when the snapshot was computed.</param>
/// <param name="Context">Resolved period and scope filter context.</param>
/// <param name="AvailablePeriods">Available semester periods for the resolved academic year.</param>
/// <param name="HeadlineKpis">Ordered institutional headline KPI cards.</param>
/// <param name="DomainSummaries">Ordered domain health summary cards.</param>
/// <param name="NeedsAttentionQueue">Prioritized cross-domain attention signals.</param>
public sealed record ExecutiveDashboardResponse(
    DateTimeOffset GeneratedAtUtc,
    ResolvedKpiContextDto Context,
    IReadOnlyList<KpiPeriodOptionDto> AvailablePeriods,
    IReadOnlyList<KpiMetricCardDto> HeadlineKpis,
    IReadOnlyList<DomainHealthSummaryDto> DomainSummaries,
    IReadOnlyList<AttentionSignalDto> NeedsAttentionQueue);

/// <summary>
/// Response payload for Level 2 Domain KPI Dashboard endpoints (<c>GET /api/{domain}/kpis/dashboard</c>).
/// </summary>
/// <param name="DomainKey">Domain identifier key.</param>
/// <param name="DomainName">Human-readable domain name.</param>
/// <param name="GeneratedAtUtc">UTC timestamp when the dashboard metrics were computed.</param>
/// <param name="Context">Resolved period and scope filter context.</param>
/// <param name="AvailablePeriods">Available semester periods for the resolved academic year.</param>
/// <param name="HeadlineKpis">Domain KPI cards with trend comparisons.</param>
/// <param name="Breakdowns">Diagnostic breakdown charts for the domain.</param>
/// <param name="AttentionSignals">Domain-scoped actionable exception signals.</param>
public sealed record DomainKpiDashboardResponse(
    string DomainKey,
    string DomainName,
    DateTimeOffset GeneratedAtUtc,
    ResolvedKpiContextDto Context,
    IReadOnlyList<KpiPeriodOptionDto> AvailablePeriods,
    IReadOnlyList<KpiMetricCardDto> HeadlineKpis,
    IReadOnlyList<KpiBreakdownDto> Breakdowns,
    IReadOnlyList<AttentionSignalDto> AttentionSignals);

/// <summary>
/// Cross-domain summary contract implemented by each operational domain assembly so the API host
/// can compose the Level 1 Executive Overview without cross-domain assembly coupling.
/// </summary>
public interface IDomainKpiSummaryProvider
{
    /// <summary>
    /// Gets the canonical domain key served by this provider (for example, <c>students</c> or <c>finance</c>).
    /// </summary>
    string DomainKey { get; }

    /// <summary>
    /// Computes the domain's flagship headline KPI, health summary, and active attention signals for the resolved filter context.
    /// </summary>
    /// <param name="context">Resolved KPI period and scope context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The domain KPI summary result.</returns>
    Task<DomainKpiSummaryResult> GetSummaryAsync(
        ResolvedKpiContextDto context,
        CancellationToken cancellationToken = default);
}
