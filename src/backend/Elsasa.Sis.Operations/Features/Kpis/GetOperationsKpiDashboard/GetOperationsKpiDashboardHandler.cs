namespace Elsasa.Sis.Operations.Features.Kpis.GetOperationsKpiDashboard;

using System.Globalization;
using Dapper;
using Elsasa.Sis.Contracts.Kpis;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Data.SqlClient;

/// <summary>
/// Query handler computing Level 2 Operations KPI dashboard metrics, breakdowns,
/// and operational attention signals via parameterized Dapper <c>QueryMultipleAsync</c> (P2-S03).
/// </summary>
public sealed class GetOperationsKpiDashboardHandler(
    ISqlConnectionFactory connectionFactory,
    IKpiPeriodResolver periodResolver)
{
    private const string _dashboardMetricsSql = """
        -- 1. School calendars readiness (OPS-01: 40 pts)
        SELECT
            COUNT(1) AS TotalCalendars,
            ISNULL(SUM(CASE WHEN sc.publication_status = 'published' THEN 1 ELSE 0 END), 0) AS PublishedCalendars
        FROM operations.school_calendars sc
        WHERE sc.academic_year_id = @AcademicYearId
          AND (@Campus IS NULL OR sc.scope_type = 'institution' OR sc.scope_reference IN ('All', 'All Campuses', @Campus));

        -- 2. Active operational periods readiness (OPS-01: 30 pts)
        SELECT
            COUNT(1) AS ActivePeriods
        FROM operations.operational_periods op
        WHERE op.academic_year_id = @AcademicYearId
          AND op.status = 'active'
          AND (@Campus IS NULL OR op.campus_context IN ('All', 'All Campuses', @Campus));

        -- 3. Calendar amendments downstream impact review (OPS-01: 30 pts, OPS-AMENDMENT-REVIEW, OPS_UNREVIEWED_AMENDMENT)
        SELECT
            COUNT(1) AS TotalAmendments,
            ISNULL(SUM(CASE WHEN ca.downstream_impact_reviewed = 1 THEN 1 ELSE 0 END), 0) AS ReviewedAmendments,
            ISNULL(SUM(CASE WHEN ca.downstream_impact_reviewed = 0 THEN 1 ELSE 0 END), 0) AS UnreviewedAmendments
        FROM operations.calendar_amendments ca
        INNER JOIN operations.school_calendars sc
            ON sc.id = ca.calendar_id
        WHERE sc.academic_year_id = @AcademicYearId
          AND (@Campus IS NULL OR sc.scope_type = 'institution' OR sc.scope_reference IN ('All', 'All Campuses', @Campus));

        -- 4. School closure days grouped by reason category (OPS-02, OPS-CLOSURE-DAYS)
        SELECT
            cl.reason_category AS ReasonCategory,
            COUNT(1) AS ClosureCount,
            ISNULL(SUM(DATEDIFF(DAY, cl.start_date, cl.end_date) + 1), 0) AS ClosureDays
        FROM operations.school_closures cl
        WHERE cl.academic_year_id = @AcademicYearId
          AND cl.status IN ('authorized', 'extended', 'lifted')
          AND (@Campus IS NULL OR cl.affected_campus IN ('All', 'All Campuses', @Campus))
        GROUP BY cl.reason_category
        ORDER BY ClosureDays DESC, cl.reason_category ASC;

        -- 5. Active school closures currently in effect (OPS_ACTIVE_CLOSURE)
        SELECT
            COUNT(1) AS ActiveClosureCount,
            ISNULL(SUM(DATEDIFF(DAY, cl.start_date, cl.end_date) + 1), 0) AS ActiveClosureDays
        FROM operations.school_closures cl
        WHERE cl.academic_year_id = @AcademicYearId
          AND cl.status IN ('authorized', 'extended')
          AND CAST(SYSUTCDATETIME() AS DATE) BETWEEN cl.start_date AND cl.end_date
          AND (@Campus IS NULL OR cl.affected_campus IN ('All', 'All Campuses', @Campus));
        """;

    /// <summary>
    /// Resolves period context and computes the full Level 2 Operations KPI dashboard response.
    /// </summary>
    /// <param name="filter">Incoming global KPI filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Operations domain KPI dashboard payload.</returns>
    public async Task<Result<DomainKpiDashboardResponse>> HandleAsync(
        GlobalKpiFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var context = await periodResolver.ResolveContextAsync(filter, cancellationToken);
        var availablePeriods = await periodResolver.GetAvailablePeriodsAsync(context.AcademicYearId, cancellationToken);
        var computed = await ComputeMetricsAsync(context, cancellationToken);

        return Result.Success(new DomainKpiDashboardResponse(
            DomainKey: "operations",
            DomainName: "Operations",
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Context: context,
            AvailablePeriods: availablePeriods,
            HeadlineKpis: computed.HeadlineKpis,
            Breakdowns: computed.Breakdowns,
            AttentionSignals: computed.AttentionSignals));
    }

    internal async Task<OperationsComputedMetrics> ComputeMetricsAsync(
        ResolvedKpiContextDto context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await using var connection = (SqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);

        var parameters = new
        {
            context.AcademicYearId,
            context.Campus
        };

        await using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            _dashboardMetricsSql,
            parameters,
            cancellationToken: cancellationToken));

        var calendarsRow = await multi.ReadSingleAsync<CalendarsSummaryRow>();
        var periodsRow = await multi.ReadSingleAsync<PeriodsSummaryRow>();
        var amendmentsRow = await multi.ReadSingleAsync<AmendmentsSummaryRow>();
        var closureRows = (await multi.ReadAsync<ClosureReasonSummaryRow>()).AsList();
        var activeClosureRow = await multi.ReadSingleAsync<ActiveClosureSummaryRow>();

        // OPS-01: Calendar & Period Readiness Index (40 pts published calendar + 30 pts active period + 30 pts reviewed amendments)
        var hasInfrastructure = calendarsRow.TotalCalendars > 0 || periodsRow.ActivePeriods > 0;
        var calendarScore = calendarsRow.PublishedCalendars > 0 ? 40.0m : 0.0m;
        var periodScore = periodsRow.ActivePeriods > 0 ? 30.0m : 0.0m;
        var amendmentReviewedRatio = amendmentsRow.TotalAmendments > 0
            ? 1.0m - ((decimal)amendmentsRow.UnreviewedAmendments / amendmentsRow.TotalAmendments)
            : 1.0m;
        var amendmentScore = hasInfrastructure ? 30.0m * amendmentReviewedRatio : 0.0m;

        var ops01Value = hasInfrastructure
            ? Math.Round(calendarScore + periodScore + amendmentScore, 1, MidpointRounding.AwayFromZero)
            : 0.0m;
        var ops01Status = !hasInfrastructure
            ? "neutral"
            : ops01Value >= 90.0m
                ? "healthy"
                : ops01Value >= 75.0m
                    ? "warning"
                    : "critical";

        var ops01Card = new KpiMetricCardDto(
            Code: "OPS-01",
            Label: "Calendar & Period Readiness Index",
            Domain: "operations",
            Value: ops01Value,
            FormattedValue: FormatPercent(ops01Value),
            Unit: "percentage",
            PriorValue: null,
            DeltaPercent: null,
            DeltaLabel: null,
            TrendDirection: "flat",
            Status: ops01Status,
            TargetLabel: "Target: >= 90%",
            Subtitle: $"{calendarsRow.PublishedCalendars} Published Cal / {periodsRow.ActivePeriods} Active Period / {amendmentsRow.UnreviewedAmendments} Unreviewed Amend",
            DrillDownRoute: "/operations/calendar?tab=amendments");

        // OPS-02: Unplanned School Closure Days
        var totalClosureDays = closureRows.Sum(r => r.ClosureDays);
        var totalClosures = closureRows.Sum(r => r.ClosureCount);
        var ops02Status = totalClosureDays <= 2
            ? "healthy"
            : totalClosureDays <= 5
                ? "warning"
                : "critical";

        var ops02Subtitle = totalClosures > 0
            ? $"{totalClosures} Closure Event(s) / {activeClosureRow.ActiveClosureCount} Active"
            : "0 Closure Events Recorded";

        var ops02Card = new KpiMetricCardDto(
            Code: "OPS-02",
            Label: "Unplanned School Closure Days",
            Domain: "operations",
            Value: totalClosureDays,
            FormattedValue: FormatDays(totalClosureDays),
            Unit: "days",
            PriorValue: null,
            DeltaPercent: null,
            DeltaLabel: null,
            TrendDirection: "flat",
            Status: ops02Status,
            TargetLabel: "Target: <= 2 days",
            Subtitle: ops02Subtitle,
            DrillDownRoute: "/operations/calendar?tab=closures");

        // Diagnostic Breakdown 1: Calendar Amendment Review Status
        var amendmentBreakdown = new KpiBreakdownDto(
            Code: "OPS-AMENDMENT-REVIEW",
            Title: "Calendar Amendment Review Status",
            Subtitle: "Downstream impact review completion for published calendar amendments",
            ChartType: "stacked-bar",
            Items:
            [
                new KpiBreakdownItemDto(
                    Key: "reviewed",
                    Label: "Impact Reviewed",
                    Count: amendmentsRow.ReviewedAmendments,
                    Percentage: ComputePercentage(amendmentsRow.ReviewedAmendments, amendmentsRow.TotalAmendments),
                    Amount: null,
                    FormattedAmount: null,
                    Status: "healthy",
                    DrillDownRoute: "/operations/calendar?tab=amendments&reviewed=true"),
                new KpiBreakdownItemDto(
                    Key: "unreviewed",
                    Label: "Pending Impact Review",
                    Count: amendmentsRow.UnreviewedAmendments,
                    Percentage: ComputePercentage(amendmentsRow.UnreviewedAmendments, amendmentsRow.TotalAmendments),
                    Amount: null,
                    FormattedAmount: null,
                    Status: amendmentsRow.UnreviewedAmendments > 0 ? "warning" : "healthy",
                    DrillDownRoute: "/operations/calendar?tab=amendments&reviewed=false")
            ]);

        // Diagnostic Breakdown 2: School Closure Days by Reason Category
        var closureBreakdownItems = closureRows
            .Select(row => new KpiBreakdownItemDto(
                Key: row.ReasonCategory,
                Label: FormatClosureReasonLabel(row.ReasonCategory),
                Count: row.ClosureDays,
                Percentage: ComputePercentage(row.ClosureDays, totalClosureDays),
                Amount: null,
                FormattedAmount: null,
                Status: DetermineClosureReasonStatus(row.ReasonCategory, row.ClosureDays),
                DrillDownRoute: "/operations/calendar?tab=closures"))
            .ToList();

        var closureBreakdown = new KpiBreakdownDto(
            Code: "OPS-CLOSURE-DAYS",
            Title: "School Closure Days by Reason",
            Subtitle: "Total closure days across authorized, extended, and lifted declarations",
            ChartType: "horizontal-bar",
            Items: closureBreakdownItems);

        // Operational Attention Signals
        var signals = new List<AttentionSignalDto>();

        if (activeClosureRow.ActiveClosureCount >= 1)
        {
            signals.Add(new AttentionSignalDto(
                SignalCode: "OPS_ACTIVE_CLOSURE",
                Domain: "operations",
                Severity: "critical",
                Title: "Active School Closures",
                Description: $"{activeClosureRow.ActiveClosureCount} school closure(s) are currently active across the selected campus scope.",
                AffectedCount: activeClosureRow.ActiveClosureCount,
                ImpactLabel: $"{activeClosureRow.ActiveClosureDays} closure day(s)",
                ActionLabel: "Manage Closures",
                DrillDownRoute: "/operations/calendar?tab=closures"));
        }

        if (amendmentsRow.UnreviewedAmendments >= 1)
        {
            signals.Add(new AttentionSignalDto(
                SignalCode: "OPS_UNREVIEWED_AMENDMENT",
                Domain: "operations",
                Severity: "warning",
                Title: "Calendar Amendments Pending Downstream Review",
                Description: $"{amendmentsRow.UnreviewedAmendments} calendar amendment(s) have not completed downstream impact review.",
                AffectedCount: amendmentsRow.UnreviewedAmendments,
                ImpactLabel: $"{amendmentsRow.UnreviewedAmendments} unreviewed amendment(s)",
                ActionLabel: "Review Amendments",
                DrillDownRoute: "/operations/calendar?tab=amendments&reviewed=false"));
        }

        var orderedSignals = signals
            .OrderBy(s => string.Equals(s.Severity, "critical", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ToList();

        return new OperationsComputedMetrics(
            HeadlineKpis: [ops01Card, ops02Card],
            Breakdowns: [amendmentBreakdown, closureBreakdown],
            AttentionSignals: orderedSignals);
    }

    private static decimal ComputePercentage(int numerator, int denominator)
    {
        if (denominator <= 0)
        {
            return 0.0m;
        }

        return Math.Round((decimal)numerator * 100.0m / denominator, 1, MidpointRounding.AwayFromZero);
    }

    private static string FormatPercent(decimal value)
        => string.Create(CultureInfo.InvariantCulture, $"{value:0.0}%");

    private static string FormatDays(int days)
        => days == 1
            ? "1 day"
            : string.Create(CultureInfo.InvariantCulture, $"{days} days");

    private static string FormatClosureReasonLabel(string reasonCategory) => reasonCategory switch
    {
        "weather_disaster" => "Weather / Natural Disaster",
        "infrastructure_hazard" => "Infrastructure Hazard",
        "health_safety" => "Health & Safety",
        "government_mandate" => "Government Mandate",
        "institutional_policy" => "Institutional Policy",
        "emergency" => "Emergency",
        _ => reasonCategory
    };

    private static string DetermineClosureReasonStatus(string reasonCategory, int closureDays)
    {
        if (closureDays > 5 || string.Equals(reasonCategory, "emergency", StringComparison.OrdinalIgnoreCase))
        {
            return "critical";
        }

        if (string.Equals(reasonCategory, "institutional_policy", StringComparison.OrdinalIgnoreCase))
        {
            return "neutral";
        }

        return "warning";
    }

    internal sealed record OperationsComputedMetrics(
        IReadOnlyList<KpiMetricCardDto> HeadlineKpis,
        IReadOnlyList<KpiBreakdownDto> Breakdowns,
        IReadOnlyList<AttentionSignalDto> AttentionSignals);

    private sealed record CalendarsSummaryRow(
        int TotalCalendars,
        int PublishedCalendars);

    private sealed record PeriodsSummaryRow(
        int ActivePeriods);

    private sealed record AmendmentsSummaryRow(
        int TotalAmendments,
        int ReviewedAmendments,
        int UnreviewedAmendments);

    private sealed record ClosureReasonSummaryRow(
        string ReasonCategory,
        int ClosureCount,
        int ClosureDays);

    private sealed record ActiveClosureSummaryRow(
        int ActiveClosureCount,
        int ActiveClosureDays);
}
