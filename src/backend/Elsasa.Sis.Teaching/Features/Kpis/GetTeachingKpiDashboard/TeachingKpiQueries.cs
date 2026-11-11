namespace Elsasa.Sis.Teaching.Features.Kpis.GetTeachingKpiDashboard;

using System.Globalization;
using Dapper;
using Elsasa.Sis.Contracts.Kpis;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Authorization policy constants for Teaching KPI endpoints.
/// </summary>
internal static class TeachingKpiPolicies
{
    public const string DashboardView = "Policies.Teaching.Kpis.View";
}

/// <summary>
/// Shared Dapper query and KPI computation logic for the Teaching KPI dashboard and summary provider.
/// </summary>
internal static class TeachingKpiQueries
{
    private static readonly (string Key, string Label, string Status)[] _workloadBands =
    [
        ("under_allocated", "Under Allocated", "neutral"),
        ("normal", "Normal", "healthy"),
        ("approaching_limit", "Approaching Limit", "warning"),
        ("overloaded_with_exception", "Overloaded (Exception)", "warning"),
        ("breached", "Breached", "critical")
    ];

    private const string _dashboardSql = """
        SELECT
            COUNT(CASE WHEN td.academic_period_id = @AcademicPeriodId THEN 1 END) AS CurrentTotalDemands,
            COUNT(CASE WHEN td.academic_period_id = @AcademicPeriodId AND td.status = 'staffed' THEN 1 END) AS CurrentStaffedDemands,
            COUNT(CASE WHEN td.academic_period_id = @AcademicPeriodId AND td.status IN ('received', 'accepted_for_staffing', 'changed') THEN 1 END) AS CurrentOpenDemands,
            ISNULL(SUM(CASE WHEN td.academic_period_id = @AcademicPeriodId AND td.status IN ('received', 'accepted_for_staffing', 'changed') THEN td.expected_workload_hours ELSE 0 END), 0.00) AS CurrentOpenHours,
            COUNT(CASE WHEN @PriorAcademicPeriodId IS NOT NULL AND td.academic_period_id = @PriorAcademicPeriodId THEN 1 END) AS PriorTotalDemands,
            COUNT(CASE WHEN @PriorAcademicPeriodId IS NOT NULL AND td.academic_period_id = @PriorAcademicPeriodId AND td.status = 'staffed' THEN 1 END) AS PriorStaffedDemands
        FROM teaching.teaching_demands td
        WHERE (td.academic_period_id = @AcademicPeriodId OR (@PriorAcademicPeriodId IS NOT NULL AND td.academic_period_id = @PriorAcademicPeriodId))
          AND td.status IN ('received', 'accepted_for_staffing', 'changed', 'staffed')
          AND (@Campus IS NULL OR td.campus_scope = @Campus OR td.campus_scope = 'All');

        SELECT
            td.required_capability_code AS CapabilityCode,
            COUNT(1) AS DemandCount,
            ISNULL(SUM(td.expected_workload_hours), 0.00) AS TotalHours
        FROM teaching.teaching_demands td
        WHERE td.academic_period_id = @AcademicPeriodId
          AND td.status IN ('received', 'accepted_for_staffing', 'changed')
          AND (@Campus IS NULL OR td.campus_scope = @Campus OR td.campus_scope = 'All')
        GROUP BY td.required_capability_code
        ORDER BY COUNT(1) DESC, td.required_capability_code ASC;

        SELECT
            wc.status AS BandStatus,
            COUNT(1) AS TeacherCount
        FROM teaching.workload_commitments wc
        INNER JOIN teaching.teacher_profiles tp ON tp.id = wc.teacher_profile_id
        WHERE wc.academic_period_id = @AcademicPeriodId
          AND tp.standing = 'active'
          AND (@Campus IS NULL OR tp.campus_affiliation = @Campus OR tp.campus_affiliation = 'All')
        GROUP BY wc.status;

        SELECT
            COUNT(DISTINCT CASE WHEN cc.severity = 'blocking' THEN cc.id END) AS BlockingConflicts,
            COUNT(DISTINCT CASE WHEN cc.severity = 'advisory' THEN cc.id END) AS AdvisoryConflicts
        FROM teaching.capacity_conflicts cc
        INNER JOIN teaching.teacher_profiles tp ON tp.id = cc.teacher_profile_id
        LEFT JOIN teaching.teaching_assignments ta ON ta.id = cc.teaching_assignment_id
        LEFT JOIN teaching.teaching_demands td ON td.id = cc.teaching_demand_id
        WHERE cc.status = 'unresolved'
          AND (@Campus IS NULL OR tp.campus_affiliation = @Campus OR tp.campus_affiliation = 'All')
          AND (
              ta.academic_period_id = @AcademicPeriodId
              OR td.academic_period_id = @AcademicPeriodId
              OR EXISTS (
                  SELECT 1
                  FROM teaching.workload_commitments wc
                  WHERE wc.teacher_profile_id = cc.teacher_profile_id
                    AND wc.academic_period_id = @AcademicPeriodId
              )
          );

        SELECT COUNT(1)
        FROM teaching.qualification_evidence qe
        INNER JOIN teaching.teacher_profiles tp ON tp.id = qe.teacher_profile_id
        WHERE qe.verification_state = 'submitted'
          AND (@Campus IS NULL OR tp.campus_affiliation = @Campus OR tp.campus_affiliation = 'All');
        """;

    public static async Task<TeachingKpiSnapshot> QueryAsync(
        ISqlConnectionFactory connectionFactory,
        ResolvedKpiContextDto context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(context);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var parameters = new
        {
            context.AcademicPeriodId,
            context.PriorAcademicPeriodId,
            context.Campus
        };

        await using var grid = await connection.QueryMultipleAsync(new CommandDefinition(
            _dashboardSql,
            parameters,
            cancellationToken: cancellationToken));

        var demandSummary = await grid.ReadSingleOrDefaultAsync<DemandSummaryRow>()
            ?? new DemandSummaryRow(0, 0, 0, 0m, 0, 0);
        var openByCapability = (await grid.ReadAsync<OpenDemandCapabilityRow>()).AsList();
        var workloadBands = (await grid.ReadAsync<WorkloadBandRow>()).AsList();
        var conflictSummary = await grid.ReadSingleOrDefaultAsync<ConflictSummaryRow>()
            ?? new ConflictSummaryRow(0, 0);
        var pendingQualifications = await grid.ReadSingleOrDefaultAsync<int>();

        var bandCounts = workloadBands
            .GroupBy(b => b.BandStatus, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TeacherCount), StringComparer.OrdinalIgnoreCase);

        var tch01 = BuildTch01Card(demandSummary, context);
        var tch02 = BuildTch02Card(demandSummary);
        var tch03 = BuildTch03Card(bandCounts, out var breachedCount, out var overloadedCount);
        var tch04 = BuildTch04Card(conflictSummary, pendingQualifications);

        var breakdowns = new List<KpiBreakdownDto>
        {
            BuildWorkloadBreakdown(bandCounts),
            BuildOpenDemandsBreakdown(openByCapability, demandSummary.CurrentOpenDemands),
            BuildConflictsBreakdown(conflictSummary, pendingQualifications)
        };

        var signals = BuildAttentionSignals(
            context,
            demandSummary.CurrentOpenDemands,
            demandSummary.CurrentOpenHours,
            conflictSummary.BlockingConflicts,
            breachedCount,
            overloadedCount);

        return new TeachingKpiSnapshot(
            HeadlineKpis: [tch01, tch02, tch03, tch04],
            Breakdowns: breakdowns,
            AttentionSignals: signals,
            OpenDemandsCount: demandSummary.CurrentOpenDemands,
            BreachedWorkloadCount: breachedCount);
    }

    private static KpiMetricCardDto BuildTch01Card(
        DemandSummaryRow demandSummary,
        ResolvedKpiContextDto context)
    {
        var hasCurrentDemands = demandSummary.CurrentTotalDemands > 0;
        var currentRate = hasCurrentDemands
            ? Math.Round(demandSummary.CurrentStaffedDemands * 100m / demandSummary.CurrentTotalDemands, 1)
            : 0m;

        decimal? priorRate = null;
        decimal? deltaPercent = null;
        string? deltaLabel = null;
        var trendDirection = "flat";

        if (context.PriorAcademicPeriodId.HasValue && demandSummary.PriorTotalDemands > 0)
        {
            priorRate = Math.Round(demandSummary.PriorStaffedDemands * 100m / demandSummary.PriorTotalDemands, 1);
            if (hasCurrentDemands)
            {
                var delta = Math.Round(currentRate - priorRate.Value, 1);
                deltaPercent = delta;
                var sign = delta > 0 ? "+" : string.Empty;
                var priorLabel = string.IsNullOrWhiteSpace(context.PriorAcademicPeriodName)
                    ? "Sem 1"
                    : context.PriorAcademicPeriodName;
                deltaLabel = $"{sign}{delta.ToString("0.0", CultureInfo.InvariantCulture)}% vs {priorLabel}";
                trendDirection = delta > 0 ? "up" : (delta < 0 ? "down" : "flat");
            }
        }

        var status = !hasCurrentDemands
            ? "neutral"
            : currentRate >= 95m
                ? "healthy"
                : currentRate >= 85m
                    ? "warning"
                    : "critical";

        return new KpiMetricCardDto(
            Code: "TCH-01",
            Label: "Instructional Demand Coverage Rate",
            Domain: "Teaching",
            Value: currentRate,
            FormattedValue: $"{currentRate.ToString("0.0", CultureInfo.InvariantCulture)}%",
            Unit: "percentage",
            PriorValue: priorRate,
            DeltaPercent: deltaPercent,
            DeltaLabel: deltaLabel,
            TrendDirection: trendDirection,
            Status: status,
            TargetLabel: "Target: >= 95%",
            Subtitle: $"{demandSummary.CurrentStaffedDemands} of {demandSummary.CurrentTotalDemands} demands staffed",
            DrillDownRoute: "/teaching/assignments?status=staffed");
    }

    private static KpiMetricCardDto BuildTch02Card(DemandSummaryRow demandSummary)
    {
        var openCount = demandSummary.CurrentOpenDemands;
        var status = openCount == 0
            ? "healthy"
            : openCount <= 2
                ? "warning"
                : "critical";

        return new KpiMetricCardDto(
            Code: "TCH-02",
            Label: "Open Unstaffed Teaching Demands",
            Domain: "Teaching",
            Value: openCount,
            FormattedValue: openCount.ToString(CultureInfo.InvariantCulture),
            Unit: "count",
            PriorValue: null,
            DeltaPercent: null,
            DeltaLabel: null,
            TrendDirection: "flat",
            Status: status,
            TargetLabel: "Target: 0 open",
            Subtitle: $"{demandSummary.CurrentOpenHours.ToString("0.#", CultureInfo.InvariantCulture)} hrs unstaffed",
            DrillDownRoute: "/teaching/assignments?status=unstaffed");
    }

    private static KpiMetricCardDto BuildTch03Card(
        IReadOnlyDictionary<string, int> bandCounts,
        out int breachedCount,
        out int overloadedCount)
    {
        breachedCount = bandCounts.GetValueOrDefault("breached", 0);
        overloadedCount = bandCounts.GetValueOrDefault("overloaded_with_exception", 0);
        var approachingCount = bandCounts.GetValueOrDefault("approaching_limit", 0);
        var totalTracked = bandCounts.Values.Sum();

        var status = totalTracked == 0
            ? "neutral"
            : breachedCount >= 1
                ? "critical"
                : overloadedCount >= 1
                    ? "warning"
                    : "healthy";

        var drillDownRoute = breachedCount >= 1
            ? "/teaching/workload?status=breached"
            : overloadedCount >= 1
                ? "/teaching/workload?status=overloaded_with_exception"
                : "/teaching/workload";

        return new KpiMetricCardDto(
            Code: "TCH-03",
            Label: "Teacher Workload Policy Breaches",
            Domain: "Teaching",
            Value: breachedCount,
            FormattedValue: breachedCount.ToString(CultureInfo.InvariantCulture),
            Unit: "count",
            PriorValue: null,
            DeltaPercent: null,
            DeltaLabel: null,
            TrendDirection: "flat",
            Status: status,
            TargetLabel: "Target: 0 breached",
            Subtitle: $"{overloadedCount} w/ exception · {approachingCount} approaching · {totalTracked} tracked",
            DrillDownRoute: drillDownRoute);
    }

    private static KpiMetricCardDto BuildTch04Card(
        ConflictSummaryRow conflictSummary,
        int pendingQualifications)
    {
        var unresolvedTotal = conflictSummary.BlockingConflicts + conflictSummary.AdvisoryConflicts;
        var status = conflictSummary.BlockingConflicts >= 1
            ? "critical"
            : conflictSummary.AdvisoryConflicts >= 1 || pendingQualifications >= 1
                ? "warning"
                : "healthy";

        var drillDownRoute = conflictSummary.BlockingConflicts >= 1
            ? "/teaching/workload?tab=conflicts&severity=blocking"
            : "/teaching/workload?tab=conflicts";

        return new KpiMetricCardDto(
            Code: "TCH-04",
            Label: "Unresolved Capacity & Qualification Conflicts",
            Domain: "Teaching",
            Value: unresolvedTotal,
            FormattedValue: unresolvedTotal.ToString(CultureInfo.InvariantCulture),
            Unit: "count",
            PriorValue: null,
            DeltaPercent: null,
            DeltaLabel: null,
            TrendDirection: "flat",
            Status: status,
            TargetLabel: "Target: 0 unresolved",
            Subtitle: $"{conflictSummary.BlockingConflicts} blocking · {conflictSummary.AdvisoryConflicts} advisory · {pendingQualifications} pending quals",
            DrillDownRoute: drillDownRoute);
    }

    private static KpiBreakdownDto BuildWorkloadBreakdown(IReadOnlyDictionary<string, int> bandCounts)
    {
        var total = bandCounts.Values.Sum();
        var items = new List<KpiBreakdownItemDto>(_workloadBands.Length);

        foreach (var (key, label, status) in _workloadBands)
        {
            var count = bandCounts.GetValueOrDefault(key, 0);
            var percentage = total > 0 ? Math.Round(count * 100m / total, 1) : 0m;
            items.Add(new KpiBreakdownItemDto(
                Key: key,
                Label: label,
                Count: count,
                Percentage: percentage,
                Amount: null,
                FormattedAmount: null,
                Status: status,
                DrillDownRoute: $"/teaching/workload?status={key}"));
        }

        return new KpiBreakdownDto(
            Code: "TCH-03-WORKLOAD-BANDS",
            Title: "Faculty Workload Utilization Distribution",
            Subtitle: "Active teachers grouped across the 5 policy utilization bands",
            ChartType: "horizontal-bar",
            Items: items);
    }

    private static KpiBreakdownDto BuildOpenDemandsBreakdown(
        IReadOnlyList<OpenDemandCapabilityRow> openByCapability,
        int totalOpenDemands)
    {
        var items = openByCapability
            .Select(row =>
            {
                var percentage = totalOpenDemands > 0
                    ? Math.Round(row.DemandCount * 100m / totalOpenDemands, 1)
                    : 0m;
                var status = row.DemandCount >= 3 ? "critical" : "warning";
                return new KpiBreakdownItemDto(
                    Key: row.CapabilityCode,
                    Label: row.CapabilityCode,
                    Count: row.DemandCount,
                    Percentage: percentage,
                    Amount: null,
                    FormattedAmount: $"{row.TotalHours.ToString("0.#", CultureInfo.InvariantCulture)} hrs",
                    Status: status,
                    DrillDownRoute: $"/teaching/assignments?status=unstaffed&capability={Uri.EscapeDataString(row.CapabilityCode)}");
            })
            .ToList();

        return new KpiBreakdownDto(
            Code: "TCH-02-OPEN-DEMANDS",
            Title: "Open Unstaffed Demands by Capability",
            Subtitle: "Unfilled instructional demands grouped by required capability code",
            ChartType: "horizontal-bar",
            Items: items);
    }

    private static KpiBreakdownDto BuildConflictsBreakdown(
        ConflictSummaryRow conflictSummary,
        int pendingQualifications)
    {
        var total = conflictSummary.BlockingConflicts + conflictSummary.AdvisoryConflicts + pendingQualifications;

        decimal Share(int count) => total > 0 ? Math.Round(count * 100m / total, 1) : 0m;

        var items = new List<KpiBreakdownItemDto>
        {
            new(
                Key: "blocking",
                Label: "Blocking Capacity Conflicts",
                Count: conflictSummary.BlockingConflicts,
                Percentage: Share(conflictSummary.BlockingConflicts),
                Amount: null,
                FormattedAmount: null,
                Status: conflictSummary.BlockingConflicts > 0 ? "critical" : "healthy",
                DrillDownRoute: "/teaching/workload?tab=conflicts&severity=blocking"),
            new(
                Key: "advisory",
                Label: "Advisory Capacity Conflicts",
                Count: conflictSummary.AdvisoryConflicts,
                Percentage: Share(conflictSummary.AdvisoryConflicts),
                Amount: null,
                FormattedAmount: null,
                Status: conflictSummary.AdvisoryConflicts > 0 ? "warning" : "healthy",
                DrillDownRoute: "/teaching/workload?tab=conflicts&severity=advisory"),
            new(
                Key: "pending_qualifications",
                Label: "Pending Qualification Verifications",
                Count: pendingQualifications,
                Percentage: Share(pendingQualifications),
                Amount: null,
                FormattedAmount: null,
                Status: pendingQualifications > 0 ? "warning" : "healthy",
                DrillDownRoute: "/teaching/faculty?verificationState=submitted")
        };

        return new KpiBreakdownDto(
            Code: "TCH-04-CONFLICTS-QUALIFICATIONS",
            Title: "Capacity Conflicts & Pending Qualifications",
            Subtitle: "Unresolved conflicts by severity alongside submitted credential verifications",
            ChartType: "horizontal-bar",
            Items: items);
    }

    private static List<AttentionSignalDto> BuildAttentionSignals(
        ResolvedKpiContextDto context,
        int openDemandsCount,
        decimal openDemandsHours,
        int blockingConflictsCount,
        int breachedCount,
        int overloadedCount)
    {
        var signals = new List<AttentionSignalDto>();

        if (blockingConflictsCount >= 1)
        {
            signals.Add(new AttentionSignalDto(
                SignalCode: "TCH_BLOCKING_CONFLICT",
                Domain: "Teaching",
                Severity: "critical",
                Title: "Unresolved Blocking Capacity Conflicts",
                Description: $"{blockingConflictsCount} blocking capacity conflict(s) require immediate schedule or workload resolution.",
                AffectedCount: blockingConflictsCount,
                ImpactLabel: $"{blockingConflictsCount} blocking",
                ActionLabel: "Resolve Conflicts",
                DrillDownRoute: "/teaching/workload?tab=conflicts&severity=blocking"));
        }

        if (breachedCount >= 1 || overloadedCount >= 1)
        {
            var severity = breachedCount >= 1 ? "critical" : "warning";
            var title = breachedCount >= 1
                ? "Faculty Workload Policy Limit Breached"
                : "Faculty Operating Over Standard Workload";
            var description = breachedCount >= 1
                ? $"{breachedCount} teacher(s) exceeded workload policy capacity without an approved exception ({overloadedCount} overloaded with exception)."
                : $"{overloadedCount} teacher(s) are operating above standard workload with an approved exception.";
            var impactLabel = breachedCount >= 1
                ? $"{breachedCount} breached"
                : $"{overloadedCount} w/ exception";
            var route = breachedCount >= 1
                ? "/teaching/workload?status=breached"
                : "/teaching/workload?status=overloaded_with_exception";

            signals.Add(new AttentionSignalDto(
                SignalCode: "TCH_WORKLOAD_BREACH",
                Domain: "Teaching",
                Severity: severity,
                Title: title,
                Description: description,
                AffectedCount: breachedCount + overloadedCount,
                ImpactLabel: impactLabel,
                ActionLabel: "Review Workload",
                DrillDownRoute: route));
        }

        if (openDemandsCount >= 1)
        {
            var severity = openDemandsCount >= 3 ? "critical" : "warning";
            signals.Add(new AttentionSignalDto(
                SignalCode: "TCH_UNSTAFFED_DEMAND",
                Domain: "Teaching",
                Severity: severity,
                Title: "Unstaffed Instructional Demands",
                Description: $"{openDemandsCount} teaching demand(s) totaling {openDemandsHours.ToString("0.#", CultureInfo.InvariantCulture)} hrs remain unstaffed in {context.AcademicPeriodName}.",
                AffectedCount: openDemandsCount,
                ImpactLabel: $"{openDemandsHours.ToString("0.#", CultureInfo.InvariantCulture)} hrs open",
                ActionLabel: "Assign Teachers",
                DrillDownRoute: "/teaching/assignments?status=unstaffed"));
        }

        return signals
            .OrderBy(s => s.Severity == "critical" ? 0 : 1)
            .ToList();
    }

    private sealed record DemandSummaryRow(
        int CurrentTotalDemands,
        int CurrentStaffedDemands,
        int CurrentOpenDemands,
        decimal CurrentOpenHours,
        int PriorTotalDemands,
        int PriorStaffedDemands);

    private sealed record OpenDemandCapabilityRow(
        string CapabilityCode,
        int DemandCount,
        decimal TotalHours);

    private sealed record WorkloadBandRow(
        string BandStatus,
        int TeacherCount);

    private sealed record ConflictSummaryRow(
        int BlockingConflicts,
        int AdvisoryConflicts);
}

internal sealed record TeachingKpiSnapshot(
    IReadOnlyList<KpiMetricCardDto> HeadlineKpis,
    IReadOnlyList<KpiBreakdownDto> Breakdowns,
    IReadOnlyList<AttentionSignalDto> AttentionSignals,
    int OpenDemandsCount,
    int BreachedWorkloadCount);
