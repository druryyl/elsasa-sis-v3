namespace Elsasa.Sis.Finance.Features.Kpis.GetFinanceKpiDashboard;

using System.Globalization;
using Dapper;
using Elsasa.Sis.Contracts.Kpis;
using Elsasa.Sis.Shared.Database;
using Microsoft.Data.SqlClient;

/// <summary>
/// Executes the Finance domain KPI batch query against <c>finance.v_student_receivables</c>,
/// <c>finance.payments</c>, and <c>finance.payment_allocations</c> and computes headline KPIs,
/// diagnostic breakdowns, and attention signals.
/// </summary>
internal static class FinanceKpiMetricsCalculator
{
    private const string _batchSql = """
        -- 1. Current period receivables summary (FIN-01, FIN-02, FIN-03, FIN_OVERDUE_90_PLUS)
        SELECT
            ISNULL(SUM(CASE WHEN r.net_obligation > 0 THEN CAST(r.total_paid AS BIGINT) ELSE 0 END), 0) AS CollectedAmount,
            ISNULL(SUM(CASE WHEN r.net_obligation > 0 THEN CAST(r.total_paid + r.outstanding_amount AS BIGINT) ELSE 0 END), 0) AS BilledBaseAmount,
            ISNULL(SUM(CASE WHEN r.outstanding_amount > 0 THEN CAST(r.outstanding_amount AS BIGINT) ELSE 0 END), 0) AS TotalOutstandingAmount,
            COUNT(DISTINCT CASE WHEN r.outstanding_amount > 0 THEN COALESCE(r.student_id, r.applicant_id) END) AS OpenAccountCount,
            ISNULL(SUM(CASE WHEN r.is_overdue = 1 THEN CAST(r.outstanding_amount AS BIGINT) ELSE 0 END), 0) AS OverdueAmount,
            COUNT(DISTINCT CASE WHEN r.is_overdue = 1 THEN COALESCE(r.student_id, r.applicant_id) END) AS DelinquentAccountCount,
            ISNULL(SUM(CASE WHEN r.aging_bracket = '90+ Days' AND r.outstanding_amount > 0 THEN CAST(r.outstanding_amount AS BIGINT) ELSE 0 END), 0) AS Overdue90PlusAmount,
            COUNT(DISTINCT CASE WHEN r.aging_bracket = '90+ Days' AND r.outstanding_amount > 0 THEN COALESCE(r.student_id, r.applicant_id) END) AS Overdue90PlusAccountCount
        FROM finance.v_student_receivables r
        LEFT JOIN students.administrative_placements ap
            ON ap.student_id = r.student_id
            AND ap.is_current = 1
        WHERE r.academic_period_id = @AcademicPeriodId
          AND (@Campus IS NULL OR ap.campus = @Campus)
          AND (@SchoolLevel IS NULL OR ap.school_level = @SchoolLevel);

        -- 2. Prior period receivables summary for trend deltas (FIN-01, FIN-02, FIN-03)
        SELECT
            ISNULL(SUM(CASE WHEN r.net_obligation > 0 THEN CAST(r.total_paid AS BIGINT) ELSE 0 END), 0) AS CollectedAmount,
            ISNULL(SUM(CASE WHEN r.net_obligation > 0 THEN CAST(r.total_paid + r.outstanding_amount AS BIGINT) ELSE 0 END), 0) AS BilledBaseAmount,
            ISNULL(SUM(CASE WHEN r.outstanding_amount > 0 THEN CAST(r.outstanding_amount AS BIGINT) ELSE 0 END), 0) AS TotalOutstandingAmount,
            ISNULL(SUM(CASE WHEN r.is_overdue = 1 THEN CAST(r.outstanding_amount AS BIGINT) ELSE 0 END), 0) AS OverdueAmount
        FROM finance.v_student_receivables r
        LEFT JOIN students.administrative_placements ap
            ON ap.student_id = r.student_id
            AND ap.is_current = 1
        WHERE @PriorAcademicPeriodId IS NOT NULL
          AND r.academic_period_id = @PriorAcademicPeriodId
          AND (@Campus IS NULL OR ap.campus = @Campus)
          AND (@SchoolLevel IS NULL OR ap.school_level = @SchoolLevel);

        -- 3. Aging distribution across receivables brackets (FIN-04)
        SELECT
            r.aging_bracket AS AgingBracket,
            COUNT(DISTINCT COALESCE(r.student_id, r.applicant_id)) AS AccountCount,
            ISNULL(SUM(CAST(r.outstanding_amount AS BIGINT)), 0) AS OutstandingAmount
        FROM finance.v_student_receivables r
        LEFT JOIN students.administrative_placements ap
            ON ap.student_id = r.student_id
            AND ap.is_current = 1
        WHERE r.academic_period_id = @AcademicPeriodId
          AND r.outstanding_amount > 0
          AND (@Campus IS NULL OR ap.campus = @Campus)
          AND (@SchoolLevel IS NULL OR ap.school_level = @SchoolLevel)
        GROUP BY r.aging_bracket;

        -- 4. Receivables breakdown by charge type
        SELECT
            r.charge_type AS ChargeType,
            COUNT(DISTINCT COALESCE(r.student_id, r.applicant_id)) AS AccountCount,
            ISNULL(SUM(CAST(r.outstanding_amount AS BIGINT)), 0) AS OutstandingAmount
        FROM finance.v_student_receivables r
        LEFT JOIN students.administrative_placements ap
            ON ap.student_id = r.student_id
            AND ap.is_current = 1
        WHERE r.academic_period_id = @AcademicPeriodId
          AND r.net_obligation > 0
          AND (@Campus IS NULL OR ap.campus = @Campus)
          AND (@SchoolLevel IS NULL OR ap.school_level = @SchoolLevel)
        GROUP BY r.charge_type
        ORDER BY OutstandingAmount DESC, r.charge_type ASC;

        -- 5. Unverified payments and unallocated confirmed cash (FIN-05, FIN_UNVERIFIED_PAYMENTS, FIN_UNALLOCATED_CASH)
        SELECT
            COUNT(CASE WHEN p.confirmation_status = 'submitted' THEN 1 END) AS UnverifiedCount,
            ISNULL(SUM(CASE WHEN p.confirmation_status = 'submitted' THEN CAST(p.payment_amount AS BIGINT) ELSE 0 END), 0) AS UnverifiedAmount,
            COUNT(CASE WHEN p.confirmation_status = 'confirmed' AND (p.payment_amount - ISNULL(alloc.total_allocated, 0)) > 0 THEN 1 END) AS UnallocatedCount,
            ISNULL(SUM(CASE WHEN p.confirmation_status = 'confirmed' AND (p.payment_amount - ISNULL(alloc.total_allocated, 0)) > 0 THEN CAST(p.payment_amount - ISNULL(alloc.total_allocated, 0) AS BIGINT) ELSE 0 END), 0) AS UnallocatedAmount,
            COUNT(CASE WHEN p.confirmation_status = 'confirmed' AND (p.payment_amount - ISNULL(alloc.total_allocated, 0)) <= 0 THEN 1 END) AS FullyAllocatedCount,
            ISNULL(SUM(CASE WHEN p.confirmation_status = 'confirmed' AND (p.payment_amount - ISNULL(alloc.total_allocated, 0)) <= 0 THEN CAST(p.payment_amount AS BIGINT) ELSE 0 END), 0) AS FullyAllocatedAmount
        FROM finance.payments p
        LEFT JOIN students.administrative_placements ap
            ON ap.student_id = p.student_id
            AND ap.is_current = 1
        LEFT JOIN (
            SELECT
                pa.payment_id,
                SUM(pa.allocated_amount) AS total_allocated
            FROM finance.payment_allocations pa
            WHERE pa.status = 'active'
            GROUP BY pa.payment_id
        ) alloc ON alloc.payment_id = p.id
        WHERE p.confirmation_status IN ('submitted', 'confirmed')
          AND (@Campus IS NULL OR ap.campus = @Campus)
          AND (@SchoolLevel IS NULL OR ap.school_level = @SchoolLevel);
        """;

    private static readonly AgingBracketSpec[] _agingBrackets =
    [
        new("Current", "current", "Current", "healthy", "/finance/obligations?aging=Current"),
        new("1-30 Days", "1-30-days", "1-30 Days", "warning", "/finance/obligations?aging=1-30%20Days"),
        new("31-60 Days", "31-60-days", "31-60 Days", "warning", "/finance/obligations?aging=31-60%20Days"),
        new("61-90 Days", "61-90-days", "61-90 Days", "critical", "/finance/obligations?aging=61-90%20Days"),
        new("90+ Days", "90-plus-days", "90+ Days", "critical", "/finance/obligations?aging=90%2B%20Days")
    ];

    internal static async Task<FinanceKpiSnapshot> ComputeAsync(
        ISqlConnectionFactory connectionFactory,
        ResolvedKpiContextDto context,
        CancellationToken cancellationToken)
    {
        await using var connection = (SqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);

        var parameters = new
        {
            context.AcademicPeriodId,
            context.PriorAcademicPeriodId,
            context.Campus,
            context.SchoolLevel
        };

        using var grid = await connection.QueryMultipleAsync(new CommandDefinition(
            _batchSql,
            parameters,
            cancellationToken: cancellationToken));

        var current = await grid.ReadSingleAsync<CurrentSummaryRow>();
        var prior = await grid.ReadSingleAsync<PriorSummaryRow>();
        var agingRows = (await grid.ReadAsync<AgingRow>()).AsList();
        var chargeTypeRows = (await grid.ReadAsync<ChargeTypeRow>()).AsList();
        var paymentQueue = await grid.ReadSingleAsync<PaymentQueueRow>();

        return BuildSnapshot(context, current, prior, agingRows, chargeTypeRows, paymentQueue);
    }

    private static FinanceKpiSnapshot BuildSnapshot(
        ResolvedKpiContextDto context,
        CurrentSummaryRow current,
        PriorSummaryRow prior,
        IReadOnlyList<AgingRow> agingRows,
        IReadOnlyList<ChargeTypeRow> chargeTypeRows,
        PaymentQueueRow paymentQueue)
    {
        var priorLabel = FormatPriorPeriodShortName(context.PriorAcademicPeriodName);
        var hasPriorData = context.PriorAcademicPeriodId.HasValue && prior.BilledBaseAmount > 0;

        // FIN-01: Net Tuition Collection Rate (with net_obligation > 0 safeguard)
        var hasCurrentBase = current.BilledBaseAmount > 0;
        var collectionRate = hasCurrentBase
            ? Math.Round(current.CollectedAmount * 100m / current.BilledBaseAmount, 1, MidpointRounding.AwayFromZero)
            : 0m;

        decimal? priorCollectionRate = hasPriorData
            ? Math.Round(prior.CollectedAmount * 100m / prior.BilledBaseAmount, 1, MidpointRounding.AwayFromZero)
            : null;

        var (fin01Delta, fin01DeltaLabel, fin01Trend) = ComputePercentagePointDelta(
            hasCurrentBase,
            collectionRate,
            priorCollectionRate,
            priorLabel);

        var fin01Status = !hasCurrentBase
            ? "neutral"
            : collectionRate >= 85m ? "healthy"
            : collectionRate >= 70m ? "warning"
            : "critical";

        var fin01 = new KpiMetricCardDto(
            Code: "FIN-01",
            Label: "Net Tuition Collection Rate",
            Domain: "Finance",
            Value: collectionRate,
            FormattedValue: $"{collectionRate.ToString("0.0", CultureInfo.InvariantCulture)}%",
            Unit: "percentage",
            PriorValue: priorCollectionRate,
            DeltaPercent: fin01Delta,
            DeltaLabel: fin01DeltaLabel,
            TrendDirection: fin01Trend,
            Status: fin01Status,
            TargetLabel: "Target: >= 85%",
            Subtitle: $"{FormatIdr(current.CollectedAmount)} of {FormatIdr(current.BilledBaseAmount)} billed",
            DrillDownRoute: "/finance/obligations?status=paid");

        // FIN-02: Total Outstanding Receivables
        long? priorOutstandingAmount = hasPriorData ? prior.TotalOutstandingAmount : null;
        var (fin02Delta, fin02DeltaLabel, fin02Trend) = ComputeAmountDelta(
            hasCurrentBase || current.TotalOutstandingAmount > 0,
            current.TotalOutstandingAmount,
            priorOutstandingAmount,
            priorLabel);

        var fin02Status = !hasCurrentBase && current.TotalOutstandingAmount == 0
            ? "neutral"
            : current.TotalOutstandingAmount == 0
                ? "healthy"
                : ComputeOverdueShareStatus(current.OverdueAmount, current.TotalOutstandingAmount);

        var fin02 = new KpiMetricCardDto(
            Code: "FIN-02",
            Label: "Total Outstanding Receivables",
            Domain: "Finance",
            Value: current.TotalOutstandingAmount,
            FormattedValue: FormatIdr(current.TotalOutstandingAmount),
            Unit: "currency",
            PriorValue: priorOutstandingAmount,
            DeltaPercent: fin02Delta,
            DeltaLabel: fin02DeltaLabel,
            TrendDirection: fin02Trend,
            Status: fin02Status,
            TargetLabel: "Overdue share < 15%",
            Subtitle: $"{current.OpenAccountCount} open {(current.OpenAccountCount == 1 ? "account" : "accounts")}",
            DrillDownRoute: "/finance/obligations?status=unpaid");

        // FIN-03: Overdue Receivable Rate & Delinquent Accounts
        var overdueRate = hasCurrentBase
            ? Math.Round(current.OverdueAmount * 100m / current.BilledBaseAmount, 1, MidpointRounding.AwayFromZero)
            : 0m;

        decimal? priorOverdueRate = hasPriorData
            ? Math.Round(prior.OverdueAmount * 100m / prior.BilledBaseAmount, 1, MidpointRounding.AwayFromZero)
            : null;

        var (fin03Delta, fin03DeltaLabel, fin03Trend) = ComputePercentagePointDelta(
            hasCurrentBase,
            overdueRate,
            priorOverdueRate,
            priorLabel);

        var fin03Status = !hasCurrentBase
            ? "neutral"
            : overdueRate <= 5.0m ? "healthy"
            : overdueRate <= 15.0m ? "warning"
            : "critical";

        var fin03 = new KpiMetricCardDto(
            Code: "FIN-03",
            Label: "Overdue Receivable Rate",
            Domain: "Finance",
            Value: overdueRate,
            FormattedValue: $"{overdueRate.ToString("0.0", CultureInfo.InvariantCulture)}%",
            Unit: "percentage",
            PriorValue: priorOverdueRate,
            DeltaPercent: fin03Delta,
            DeltaLabel: fin03DeltaLabel,
            TrendDirection: fin03Trend,
            Status: fin03Status,
            TargetLabel: "Target: <= 5.0%",
            Subtitle: $"{FormatIdr(current.OverdueAmount)} across {current.DelinquentAccountCount} {(current.DelinquentAccountCount == 1 ? "account" : "accounts")}",
            DrillDownRoute: "/finance/obligations?status=overdue");

        // FIN-04: Chronic Overdue Exposure (90+ Days)
        var fin04Status = !hasCurrentBase && current.TotalOutstandingAmount == 0
            ? "neutral"
            : current.Overdue90PlusAccountCount >= 1 ? "critical" : "healthy";

        var fin04 = new KpiMetricCardDto(
            Code: "FIN-04",
            Label: "90+ Days Overdue Exposure",
            Domain: "Finance",
            Value: current.Overdue90PlusAmount,
            FormattedValue: FormatIdr(current.Overdue90PlusAmount),
            Unit: "currency",
            PriorValue: null,
            DeltaPercent: null,
            DeltaLabel: null,
            TrendDirection: "flat",
            Status: fin04Status,
            TargetLabel: "Target: Rp 0 (90+ Days)",
            Subtitle: $"{current.Overdue90PlusAccountCount} delinquent {(current.Overdue90PlusAccountCount == 1 ? "account" : "accounts")}",
            DrillDownRoute: "/finance/obligations?aging=90%2B%20Days");

        // FIN-05: Unverified Payments & Unallocated Cash
        var fin05Status = paymentQueue.UnverifiedCount >= 4
            ? "critical"
            : (paymentQueue.UnverifiedCount >= 1 || paymentQueue.UnallocatedCount >= 1)
                ? "warning"
                : "healthy";

        var fin05 = new KpiMetricCardDto(
            Code: "FIN-05",
            Label: "Unverified Payments & Unallocated Cash",
            Domain: "Finance",
            Value: paymentQueue.UnverifiedCount,
            FormattedValue: $"{paymentQueue.UnverifiedCount} pending",
            Unit: "count",
            PriorValue: null,
            DeltaPercent: null,
            DeltaLabel: null,
            TrendDirection: "flat",
            Status: fin05Status,
            TargetLabel: "Target: 0 unverified",
            Subtitle: $"{FormatIdr(paymentQueue.UnverifiedAmount)} unverified • {paymentQueue.UnallocatedCount} unallocated ({FormatIdr(paymentQueue.UnallocatedAmount)})",
            DrillDownRoute: "/finance/payments?status=submitted");

        // Breakdowns
        var agingByBracket = agingRows.ToDictionary(r => r.AgingBracket, StringComparer.OrdinalIgnoreCase);
        var agingItems = _agingBrackets
            .Select(spec =>
            {
                agingByBracket.TryGetValue(spec.SqlBracket, out var row);
                var count = row?.AccountCount ?? 0;
                var amount = row?.OutstandingAmount ?? 0L;
                var pct = current.TotalOutstandingAmount > 0
                    ? Math.Round(amount * 100m / current.TotalOutstandingAmount, 1, MidpointRounding.AwayFromZero)
                    : 0m;

                return new KpiBreakdownItemDto(
                    Key: spec.Key,
                    Label: spec.Label,
                    Count: count,
                    Percentage: pct,
                    Amount: amount,
                    FormattedAmount: FormatIdr(amount),
                    Status: spec.Status,
                    DrillDownRoute: spec.DrillDownRoute);
            })
            .ToList();

        var agingBreakdown = new KpiBreakdownDto(
            Code: "FIN-04",
            Title: "Receivables Aging Distribution",
            Subtitle: "Open receivables grouped by days past due date",
            ChartType: "horizontal-bar",
            Items: agingItems);

        var chargeTypeItems = chargeTypeRows
            .Select(row =>
            {
                var pct = current.TotalOutstandingAmount > 0
                    ? Math.Round(row.OutstandingAmount * 100m / current.TotalOutstandingAmount, 1, MidpointRounding.AwayFromZero)
                    : 0m;

                return new KpiBreakdownItemDto(
                    Key: row.ChargeType,
                    Label: FormatChargeTypeLabel(row.ChargeType),
                    Count: row.AccountCount,
                    Percentage: pct,
                    Amount: row.OutstandingAmount,
                    FormattedAmount: FormatIdr(row.OutstandingAmount),
                    Status: row.OutstandingAmount > 0 ? "warning" : "healthy",
                    DrillDownRoute: $"/finance/obligations?chargeType={Uri.EscapeDataString(row.ChargeType)}");
            })
            .ToList();

        var chargeTypeBreakdown = new KpiBreakdownDto(
            Code: "FIN-CHARGE-MIX",
            Title: "Outstanding Receivables by Charge Type",
            Subtitle: "Distribution of unpaid balances across fee categories",
            ChartType: "horizontal-bar",
            Items: chargeTypeItems);

        var totalCashPipelineAmount = paymentQueue.FullyAllocatedAmount + paymentQueue.UnallocatedAmount + paymentQueue.UnverifiedAmount;
        var paymentPipelineBreakdown = new KpiBreakdownDto(
            Code: "FIN-05",
            Title: "Payment Verification & Allocation Pipeline",
            Subtitle: "Confirmed allocated receipts vs unallocated cash and unverified submissions",
            ChartType: "stacked-bar",
            Items:
            [
                new KpiBreakdownItemDto(
                    Key: "allocated",
                    Label: "Confirmed & Allocated",
                    Count: paymentQueue.FullyAllocatedCount,
                    Percentage: totalCashPipelineAmount > 0
                        ? Math.Round(paymentQueue.FullyAllocatedAmount * 100m / totalCashPipelineAmount, 1, MidpointRounding.AwayFromZero)
                        : 0m,
                    Amount: paymentQueue.FullyAllocatedAmount,
                    FormattedAmount: FormatIdr(paymentQueue.FullyAllocatedAmount),
                    Status: "healthy",
                    DrillDownRoute: "/finance/payments?status=confirmed"),
                new KpiBreakdownItemDto(
                    Key: "unallocated",
                    Label: "Unallocated Confirmed Cash",
                    Count: paymentQueue.UnallocatedCount,
                    Percentage: totalCashPipelineAmount > 0
                        ? Math.Round(paymentQueue.UnallocatedAmount * 100m / totalCashPipelineAmount, 1, MidpointRounding.AwayFromZero)
                        : 0m,
                    Amount: paymentQueue.UnallocatedAmount,
                    FormattedAmount: FormatIdr(paymentQueue.UnallocatedAmount),
                    Status: paymentQueue.UnallocatedCount > 0 ? "warning" : "healthy",
                    DrillDownRoute: "/finance/accounts?filter=unallocated"),
                new KpiBreakdownItemDto(
                    Key: "submitted",
                    Label: "Submitted (Unverified)",
                    Count: paymentQueue.UnverifiedCount,
                    Percentage: totalCashPipelineAmount > 0
                        ? Math.Round(paymentQueue.UnverifiedAmount * 100m / totalCashPipelineAmount, 1, MidpointRounding.AwayFromZero)
                        : 0m,
                    Amount: paymentQueue.UnverifiedAmount,
                    FormattedAmount: FormatIdr(paymentQueue.UnverifiedAmount),
                    Status: paymentQueue.UnverifiedCount >= 4 ? "critical" : paymentQueue.UnverifiedCount > 0 ? "warning" : "healthy",
                    DrillDownRoute: "/finance/payments?status=submitted")
            ]);

        // Attention Signals
        var signals = new List<AttentionSignalDto>();

        if (current.Overdue90PlusAccountCount >= 1)
        {
            signals.Add(new AttentionSignalDto(
                SignalCode: "FIN_OVERDUE_90_PLUS",
                Domain: "Finance",
                Severity: "critical",
                Title: "Severe Delinquency (90+ Days)",
                Description: $"{current.Overdue90PlusAccountCount} student {(current.Overdue90PlusAccountCount == 1 ? "account has" : "accounts have")} receivables overdue beyond 90 days totaling {FormatIdr(current.Overdue90PlusAmount)}.",
                AffectedCount: current.Overdue90PlusAccountCount,
                ImpactLabel: $"{FormatIdr(current.Overdue90PlusAmount)} overdue",
                ActionLabel: "Investigate",
                DrillDownRoute: "/finance/obligations?aging=90%2B%20Days"));
        }

        if (paymentQueue.UnverifiedCount >= 1)
        {
            signals.Add(new AttentionSignalDto(
                SignalCode: "FIN_UNVERIFIED_PAYMENTS",
                Domain: "Finance",
                Severity: paymentQueue.UnverifiedCount >= 4 ? "critical" : "warning",
                Title: "Submitted Payments Awaiting Confirmation",
                Description: $"{paymentQueue.UnverifiedCount} submitted {(paymentQueue.UnverifiedCount == 1 ? "payment totaling" : "payments totaling")} {FormatIdr(paymentQueue.UnverifiedAmount)} await cashier verification.",
                AffectedCount: paymentQueue.UnverifiedCount,
                ImpactLabel: $"{FormatIdr(paymentQueue.UnverifiedAmount)} pending",
                ActionLabel: "Verify Payments",
                DrillDownRoute: "/finance/payments?status=submitted"));
        }

        if (paymentQueue.UnallocatedCount >= 1)
        {
            signals.Add(new AttentionSignalDto(
                SignalCode: "FIN_UNALLOCATED_CASH",
                Domain: "Finance",
                Severity: "warning",
                Title: "Confirmed Payments With Unallocated Balance",
                Description: $"{paymentQueue.UnallocatedCount} confirmed {(paymentQueue.UnallocatedCount == 1 ? "payment holds" : "payments hold")} {FormatIdr(paymentQueue.UnallocatedAmount)} in unallocated cash awaiting settlement.",
                AffectedCount: paymentQueue.UnallocatedCount,
                ImpactLabel: $"{FormatIdr(paymentQueue.UnallocatedAmount)} unallocated",
                ActionLabel: "Allocate Cash",
                DrillDownRoute: "/finance/accounts?filter=unallocated"));
        }

        var orderedSignals = signals
            .OrderBy(s => s.Severity == "critical" ? 0 : 1)
            .ThenByDescending(s => s.AffectedCount)
            .ToList();

        return new FinanceKpiSnapshot(
            HeadlineKpis: [fin01, fin02, fin03, fin04, fin05],
            Breakdowns: [agingBreakdown, chargeTypeBreakdown, paymentPipelineBreakdown],
            AttentionSignals: orderedSignals);
    }

    private static string ComputeOverdueShareStatus(long overdueAmount, long totalOutstanding)
    {
        if (totalOutstanding <= 0)
        {
            return "healthy";
        }

        var overdueShare = overdueAmount * 100m / totalOutstanding;
        return overdueShare < 15m ? "healthy"
            : overdueShare <= 30m ? "warning"
            : "critical";
    }

    private static (decimal? DeltaPercent, string? DeltaLabel, string TrendDirection) ComputePercentagePointDelta(
        bool hasCurrentData,
        decimal currentValue,
        decimal? priorValue,
        string priorLabel)
    {
        if (!hasCurrentData || !priorValue.HasValue)
        {
            return (null, null, "flat");
        }

        var delta = Math.Round(currentValue - priorValue.Value, 1, MidpointRounding.AwayFromZero);
        var sign = delta > 0 ? "+" : string.Empty;
        var trend = delta > 0 ? "up" : delta < 0 ? "down" : "flat";
        var label = $"{sign}{delta.ToString("0.0", CultureInfo.InvariantCulture)}% vs {priorLabel}";
        return (delta, label, trend);
    }

    private static (decimal? DeltaPercent, string? DeltaLabel, string TrendDirection) ComputeAmountDelta(
        bool hasCurrentData,
        long currentAmount,
        long? priorAmount,
        string priorLabel)
    {
        if (!hasCurrentData || !priorAmount.HasValue)
        {
            return (null, null, "flat");
        }

        if (priorAmount.Value == 0)
        {
            if (currentAmount == 0)
            {
                return (0m, $"0.0% vs {priorLabel}", "flat");
            }

            return (null, $"vs Rp 0 in {priorLabel}", "up");
        }

        var pctDelta = Math.Round((currentAmount - priorAmount.Value) * 100m / priorAmount.Value, 1, MidpointRounding.AwayFromZero);
        var sign = pctDelta > 0 ? "+" : string.Empty;
        var trend = pctDelta > 0 ? "up" : pctDelta < 0 ? "down" : "flat";
        var label = $"{sign}{pctDelta.ToString("0.0", CultureInfo.InvariantCulture)}% vs {priorLabel}";
        return (pctDelta, label, trend);
    }

    private static string FormatPriorPeriodShortName(string? priorPeriodName)
    {
        if (string.IsNullOrWhiteSpace(priorPeriodName))
        {
            return "prior";
        }

        if (priorPeriodName.Contains("Semester 1", StringComparison.OrdinalIgnoreCase))
        {
            return "Sem 1";
        }

        if (priorPeriodName.Contains("Semester 2", StringComparison.OrdinalIgnoreCase))
        {
            return "Sem 2";
        }

        return priorPeriodName.Trim();
    }

    private static string FormatChargeTypeLabel(string chargeType) => chargeType.ToLowerInvariant() switch
    {
        "tuition" => "Tuition (SPP)",
        "enrollment_fee" => "Facility & Enrollment Fee",
        "admission_fee" => "Admission Fee",
        "activity_fee" => "PACE & Activity Fee",
        _ => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(chargeType.Replace('_', ' '))
    };

    private static string FormatIdr(long amount)
    {
        var abs = Math.Abs(amount);
        var prefix = amount < 0 ? "-Rp " : "Rp ";

        if (abs >= 1_000_000_000L)
        {
            return $"{prefix}{(abs / 1_000_000_000m).ToString("0.00", CultureInfo.InvariantCulture)}B";
        }

        if (abs >= 1_000_000L)
        {
            return $"{prefix}{(abs / 1_000_000m).ToString("0.0", CultureInfo.InvariantCulture)}M";
        }

        if (abs >= 1_000L)
        {
            return $"{prefix}{(abs / 1_000m).ToString("0.0", CultureInfo.InvariantCulture)}K";
        }

        return $"{prefix}{abs.ToString("N0", CultureInfo.InvariantCulture)}";
    }

    internal sealed record FinanceKpiSnapshot(
        IReadOnlyList<KpiMetricCardDto> HeadlineKpis,
        IReadOnlyList<KpiBreakdownDto> Breakdowns,
        IReadOnlyList<AttentionSignalDto> AttentionSignals);

    private sealed record AgingBracketSpec(
        string SqlBracket,
        string Key,
        string Label,
        string Status,
        string DrillDownRoute);

    private sealed record CurrentSummaryRow(
        long CollectedAmount,
        long BilledBaseAmount,
        long TotalOutstandingAmount,
        int OpenAccountCount,
        long OverdueAmount,
        int DelinquentAccountCount,
        long Overdue90PlusAmount,
        int Overdue90PlusAccountCount);

    private sealed record PriorSummaryRow(
        long CollectedAmount,
        long BilledBaseAmount,
        long TotalOutstandingAmount,
        long OverdueAmount);

    private sealed record AgingRow(
        string AgingBracket,
        int AccountCount,
        long OutstandingAmount);

    private sealed record ChargeTypeRow(
        string ChargeType,
        int AccountCount,
        long OutstandingAmount);

    private sealed record PaymentQueueRow(
        int UnverifiedCount,
        long UnverifiedAmount,
        int UnallocatedCount,
        long UnallocatedAmount,
        int FullyAllocatedCount,
        long FullyAllocatedAmount);
}
