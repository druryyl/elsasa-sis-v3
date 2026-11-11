namespace Elsasa.Sis.Academics.Features.Kpis.GetAcademicsKpiDashboard;

using System.Globalization;
using Dapper;
using Elsasa.Sis.Contracts.Kpis;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Data.SqlClient;

/// <summary>
/// Query handler computing Level 2 Academics KPI dashboard metrics, breakdowns,
/// and operational attention signals via parameterized Dapper <c>QueryMultipleAsync</c> (P3-S06).
/// </summary>
public sealed class GetAcademicsKpiDashboardHandler(
    ISqlConnectionFactory connectionFactory,
    IKpiPeriodResolver periodResolver)
{
    private const string _dashboardMetricsSql = """
        SELECT
            ISNULL(SUM(CASE WHEN ast.academic_period_id = @AcademicPeriodId THEN 1 ELSE 0 END), 0) AS TotalEvaluated,
            ISNULL(SUM(CASE WHEN ast.academic_period_id = @AcademicPeriodId AND ast.standing = 'good_standing' THEN 1 ELSE 0 END), 0) AS GoodStandingCount,
            ISNULL(SUM(CASE WHEN ast.academic_period_id = @AcademicPeriodId AND ast.standing = 'conditional_standing' THEN 1 ELSE 0 END), 0) AS ConditionalStandingCount,
            ISNULL(SUM(CASE WHEN ast.academic_period_id = @AcademicPeriodId AND ast.standing = 'not_in_good_standing' THEN 1 ELSE 0 END), 0) AS NotInGoodStandingCount,
            ISNULL(SUM(CASE WHEN @PriorAcademicPeriodId IS NOT NULL AND ast.academic_period_id = @PriorAcademicPeriodId THEN 1 ELSE 0 END), 0) AS PriorTotalEvaluated,
            ISNULL(SUM(CASE WHEN @PriorAcademicPeriodId IS NOT NULL AND ast.academic_period_id = @PriorAcademicPeriodId AND ast.standing = 'good_standing' THEN 1 ELSE 0 END), 0) AS PriorGoodStandingCount
        FROM academics.academic_standings ast
        INNER JOIN students.administrative_placements ap
            ON ap.student_id = ast.student_id
           AND ap.is_current = 1
        WHERE ast.academic_period_id IN (@AcademicPeriodId, COALESCE(@PriorAcademicPeriodId, @AcademicPeriodId))
          AND (@Campus IS NULL OR ap.campus = @Campus)
          AND (@SchoolLevel IS NULL OR ap.school_level = @SchoolLevel);

        ;WITH PeriodStudentAttendance AS (
            SELECT
                ar.student_id AS StudentId,
                COUNT(1) AS TotalRecords,
                SUM(CASE WHEN ar.status IN ('present', 'late', 'excused_absence') THEN 1 ELSE 0 END) AS AttendedRecords
            FROM academics.attendance_records ar
            INNER JOIN academics.learning_activities la
                ON la.id = ar.learning_activity_id
            INNER JOIN operations.operational_periods op
                ON op.id = @AcademicPeriodId
            INNER JOIN students.administrative_placements ap
                ON ap.student_id = ar.student_id
               AND ap.is_current = 1
            WHERE la.activity_date BETWEEN op.start_date AND op.end_date
              AND la.status <> 'cancelled'
              AND ar.status IN ('present', 'late', 'excused_absence', 'absent', 'partial')
              AND (@Campus IS NULL OR ap.campus = @Campus)
              AND (@SchoolLevel IS NULL OR ap.school_level = @SchoolLevel)
            GROUP BY ar.student_id
        ),
        ParticipatingStudents AS (
            SELECT DISTINCT
                p.student_id AS StudentId
            FROM academics.academic_participations p
            INNER JOIN students.administrative_placements ap
                ON ap.student_id = p.student_id
               AND ap.is_current = 1
            WHERE p.academic_period_id = @AcademicPeriodId
              AND (@Campus IS NULL OR ap.campus = @Campus)
              AND (@SchoolLevel IS NULL OR ap.school_level = @SchoolLevel)
        ),
        StudentRiskFlags AS (
            SELECT
                ps.StudentId,
                CASE
                    WHEN EXISTS (
                        SELECT 1
                        FROM academics.academic_standings ast
                        WHERE ast.student_id = ps.StudentId
                          AND ast.academic_period_id = @AcademicPeriodId
                          AND ast.standing IN ('conditional_standing', 'not_in_good_standing')
                    )
                    OR EXISTS (
                        SELECT 1
                        FROM academics.learning_progress lp
                        WHERE lp.student_id = ps.StudentId
                          AND lp.academic_period_id = @AcademicPeriodId
                          AND lp.status = 'needs_remediation'
                    )
                    OR EXISTS (
                        SELECT 1
                        FROM PeriodStudentAttendance psa
                        WHERE psa.StudentId = ps.StudentId
                          AND psa.TotalRecords > 0
                          AND (CAST(psa.AttendedRecords AS DECIMAL(10, 4)) * 100.0 / NULLIF(psa.TotalRecords, 0)) < 80.0
                    )
                    THEN 1
                    ELSE 0
                END AS IsAtRisk,
                CASE
                    WHEN EXISTS (
                        SELECT 1
                        FROM academics.academic_interventions ai
                        WHERE ai.student_id = ps.StudentId
                          AND ai.academic_period_id = @AcademicPeriodId
                          AND ai.status IN ('planned', 'in_progress')
                    )
                    THEN 1
                    ELSE 0
                END AS IsCovered
            FROM ParticipatingStudents ps
        )
        SELECT
            COUNT(1) AS TotalParticipatingStudents,
            ISNULL(SUM(IsAtRisk), 0) AS AtRiskCount,
            ISNULL(SUM(CASE WHEN IsAtRisk = 1 AND IsCovered = 1 THEN 1 ELSE 0 END), 0) AS CoveredCount,
            ISNULL(SUM(CASE WHEN IsAtRisk = 1 AND IsCovered = 0 THEN 1 ELSE 0 END), 0) AS UnaddressedCount
        FROM StudentRiskFlags;

        ;WITH PeriodStudentAttendance AS (
            SELECT
                op.id AS PeriodId,
                ar.student_id AS StudentId,
                COUNT(1) AS TotalRecords,
                SUM(CASE WHEN ar.status IN ('present', 'late', 'excused_absence') THEN 1 ELSE 0 END) AS AttendedRecords
            FROM academics.attendance_records ar
            INNER JOIN academics.learning_activities la
                ON la.id = ar.learning_activity_id
            INNER JOIN operations.operational_periods op
                ON op.id IN (@AcademicPeriodId, COALESCE(@PriorAcademicPeriodId, @AcademicPeriodId))
            INNER JOIN students.administrative_placements ap
                ON ap.student_id = ar.student_id
               AND ap.is_current = 1
            WHERE la.activity_date BETWEEN op.start_date AND op.end_date
              AND la.status <> 'cancelled'
              AND ar.status IN ('present', 'late', 'excused_absence', 'absent', 'partial')
              AND (@Campus IS NULL OR ap.campus = @Campus)
              AND (@SchoolLevel IS NULL OR ap.school_level = @SchoolLevel)
            GROUP BY op.id, ar.student_id
        )
        SELECT
            ISNULL(SUM(CASE WHEN PeriodId = @AcademicPeriodId THEN TotalRecords ELSE 0 END), 0) AS TotalAttendanceRecords,
            ISNULL(SUM(CASE WHEN PeriodId = @AcademicPeriodId THEN AttendedRecords ELSE 0 END), 0) AS AttendedRecords,
            ISNULL(SUM(CASE
                WHEN PeriodId = @AcademicPeriodId
                 AND TotalRecords > 0
                 AND (CAST(AttendedRecords AS DECIMAL(10, 4)) * 100.0 / NULLIF(TotalRecords, 0)) < 85.0
                THEN 1 ELSE 0
            END), 0) AS ChronicAbsenteeismCount,
            ISNULL(SUM(CASE WHEN @PriorAcademicPeriodId IS NOT NULL AND PeriodId = @PriorAcademicPeriodId THEN TotalRecords ELSE 0 END), 0) AS PriorTotalAttendanceRecords,
            ISNULL(SUM(CASE WHEN @PriorAcademicPeriodId IS NOT NULL AND PeriodId = @PriorAcademicPeriodId THEN AttendedRecords ELSE 0 END), 0) AS PriorAttendedRecords
        FROM PeriodStudentAttendance;

        SELECT
            COUNT(1) AS TotalSubmissions,
            ISNULL(SUM(CASE WHEN sub.status IN ('submitted', 'excused', 'exempted') THEN 1 ELSE 0 END), 0) AS CompletedCount,
            ISNULL(SUM(CASE WHEN sub.status = 'late' THEN 1 ELSE 0 END), 0) AS LateCount,
            ISNULL(SUM(CASE WHEN sub.status = 'missing' THEN 1 ELSE 0 END), 0) AS MissingCount
        FROM academics.submissions sub
        INNER JOIN academics.assignments a
            ON a.id = sub.assignment_id
        INNER JOIN academics.learning_activities la
            ON la.id = a.learning_activity_id
        INNER JOIN operations.operational_periods op
            ON op.id = @AcademicPeriodId
        INNER JOIN students.administrative_placements ap
            ON ap.student_id = sub.student_id
           AND ap.is_current = 1
        WHERE a.status IN ('released', 'closed')
          AND la.activity_date BETWEEN op.start_date AND op.end_date
          AND (@Campus IS NULL OR ap.campus = @Campus)
          AND (@SchoolLevel IS NULL OR ap.school_level = @SchoolLevel);

        SELECT
            ISNULL(SUM(CASE WHEN op.id = @AcademicPeriodId THEN 1 ELSE 0 END), 0) AS TotalFinalResults,
            ISNULL(SUM(CASE WHEN op.id = @AcademicPeriodId AND res.result_status = 'published' THEN 1 ELSE 0 END), 0) AS PublishedCount,
            ISNULL(SUM(CASE WHEN op.id = @AcademicPeriodId AND res.result_status = 'approved' THEN 1 ELSE 0 END), 0) AS ApprovedCount,
            ISNULL(SUM(CASE WHEN op.id = @AcademicPeriodId AND res.result_status = 'provisional' THEN 1 ELSE 0 END), 0) AS ProvisionalCount,
            ISNULL(SUM(CASE WHEN @PriorAcademicPeriodId IS NOT NULL AND op.id = @PriorAcademicPeriodId THEN 1 ELSE 0 END), 0) AS PriorTotalFinalResults,
            ISNULL(SUM(CASE WHEN @PriorAcademicPeriodId IS NOT NULL AND op.id = @PriorAcademicPeriodId AND res.result_status = 'published' THEN 1 ELSE 0 END), 0) AS PriorPublishedCount
        FROM academics.academic_results res
        INNER JOIN academics.assessments asm
            ON asm.id = res.assessment_id
        INNER JOIN operations.operational_periods op
            ON op.id IN (@AcademicPeriodId, COALESCE(@PriorAcademicPeriodId, @AcademicPeriodId))
        INNER JOIN students.administrative_placements ap
            ON ap.student_id = res.student_id
           AND ap.is_current = 1
        WHERE res.is_final = 1
          AND res.student_id IS NOT NULL
          AND COALESCE(asm.assessment_date, res.evaluated_date) BETWEEN op.start_date AND op.end_date
          AND (@Campus IS NULL OR ap.campus = @Campus)
          AND (@SchoolLevel IS NULL OR ap.school_level = @SchoolLevel);
        """;

    /// <summary>
    /// Resolves period context and computes the full Level 2 Academics KPI dashboard response.
    /// </summary>
    /// <param name="filter">Incoming global KPI filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Academics domain KPI dashboard payload.</returns>
    public async Task<Result<DomainKpiDashboardResponse>> HandleAsync(
        GlobalKpiFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var context = await periodResolver.ResolveContextAsync(filter, cancellationToken);
        var availablePeriods = await periodResolver.GetAvailablePeriodsAsync(context.AcademicYearId, cancellationToken);
        var computed = await ComputeMetricsAsync(context, cancellationToken);

        return Result.Success(new DomainKpiDashboardResponse(
            DomainKey: "academics",
            DomainName: "Academics",
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Context: context,
            AvailablePeriods: availablePeriods,
            HeadlineKpis: computed.HeadlineKpis,
            Breakdowns: computed.Breakdowns,
            AttentionSignals: computed.AttentionSignals));
    }

    internal async Task<AcademicsComputedMetrics> ComputeMetricsAsync(
        ResolvedKpiContextDto context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await using var connection = (SqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);

        var parameters = new
        {
            context.AcademicPeriodId,
            context.PriorAcademicPeriodId,
            context.Campus,
            context.SchoolLevel
        };

        await using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            _dashboardMetricsSql,
            parameters,
            cancellationToken: cancellationToken));

        var standingRow = await multi.ReadSingleAsync<StandingSummaryRow>();
        var atRiskRow = await multi.ReadSingleAsync<AtRiskSummaryRow>();
        var attendanceRow = await multi.ReadSingleAsync<AttendanceSummaryRow>();
        var submissionRow = await multi.ReadSingleAsync<SubmissionSummaryRow>();
        var resultsRow = await multi.ReadSingleAsync<AssessmentResultsSummaryRow>();

        var priorShortName = FormatPriorPeriodShortName(context.PriorAcademicPeriodName);

        // ACA-01: Good Academic Standing Rate
        var aca01Value = ComputePercentage(standingRow.GoodStandingCount, standingRow.TotalEvaluated);
        decimal? aca01Prior = standingRow.PriorTotalEvaluated > 0
            ? ComputePercentage(standingRow.PriorGoodStandingCount, standingRow.PriorTotalEvaluated)
            : null;
        var aca01Delta = ComputeDelta(aca01Value, aca01Prior);
        var aca01Status = standingRow.TotalEvaluated == 0
            ? "neutral"
            : aca01Value >= 85.0m
                ? "healthy"
                : aca01Value >= 75.0m
                    ? "warning"
                    : "critical";

        var aca01Card = new KpiMetricCardDto(
            Code: "ACA-01",
            Label: "Good Academic Standing Rate",
            Domain: "academics",
            Value: aca01Value,
            FormattedValue: FormatPercent(aca01Value),
            Unit: "percentage",
            PriorValue: aca01Prior,
            DeltaPercent: aca01Delta,
            DeltaLabel: FormatDeltaLabel(aca01Delta, priorShortName),
            TrendDirection: DetermineTrendDirection(aca01Delta),
            Status: aca01Status,
            TargetLabel: "Target: >= 85%",
            Subtitle: $"{standingRow.GoodStandingCount} Good / {standingRow.ConditionalStandingCount} Conditional / {standingRow.NotInGoodStandingCount} Not in Good",
            DrillDownRoute: "/academics/cohort-review?standing=conditional_standing");

        // ACA-02: At-Risk Learners & Remediation Coverage
        var aca02Value = atRiskRow.AtRiskCount > 0
            ? ComputePercentage(atRiskRow.CoveredCount, atRiskRow.AtRiskCount)
            : atRiskRow.TotalParticipatingStudents > 0
                ? 100.0m
                : 0.0m;
        var aca02Status = atRiskRow.TotalParticipatingStudents == 0
            ? "neutral"
            : atRiskRow.UnaddressedCount == 0
                ? "healthy"
                : atRiskRow.UnaddressedCount <= 2
                    ? "warning"
                    : "critical";
        var aca02Subtitle = atRiskRow.AtRiskCount > 0
            ? $"{atRiskRow.AtRiskCount} At-Risk / {atRiskRow.CoveredCount} Covered / {atRiskRow.UnaddressedCount} Unaddressed"
            : "0 At-Risk Learners Detected";

        var aca02Card = new KpiMetricCardDto(
            Code: "ACA-02",
            Label: "At-Risk Remediation Coverage",
            Domain: "academics",
            Value: aca02Value,
            FormattedValue: FormatPercent(aca02Value),
            Unit: "percentage",
            PriorValue: null,
            DeltaPercent: null,
            DeltaLabel: null,
            TrendDirection: "flat",
            Status: aca02Status,
            TargetLabel: "Target: 0 Unaddressed",
            Subtitle: aca02Subtitle,
            DrillDownRoute: "/academics/interventions?filter=unaddressed");

        // ACA-03: Period Attendance Rate & Chronic Absenteeism
        var aca03Value = ComputePercentage(attendanceRow.AttendedRecords, attendanceRow.TotalAttendanceRecords);
        decimal? aca03Prior = attendanceRow.PriorTotalAttendanceRecords > 0
            ? ComputePercentage(attendanceRow.PriorAttendedRecords, attendanceRow.PriorTotalAttendanceRecords)
            : null;
        var aca03Delta = ComputeDelta(aca03Value, aca03Prior);
        var aca03Status = attendanceRow.TotalAttendanceRecords == 0
            ? "neutral"
            : aca03Value >= 92.0m
                ? "healthy"
                : aca03Value >= 85.0m
                    ? "warning"
                    : "critical";

        var aca03Card = new KpiMetricCardDto(
            Code: "ACA-03",
            Label: "Period Attendance Rate",
            Domain: "academics",
            Value: aca03Value,
            FormattedValue: FormatPercent(aca03Value),
            Unit: "percentage",
            PriorValue: aca03Prior,
            DeltaPercent: aca03Delta,
            DeltaLabel: FormatDeltaLabel(aca03Delta, priorShortName),
            TrendDirection: DetermineTrendDirection(aca03Delta),
            Status: aca03Status,
            TargetLabel: "Target: >= 92%",
            Subtitle: $"{attendanceRow.ChronicAbsenteeismCount} Chronic (<85%) / {attendanceRow.TotalAttendanceRecords} Records",
            DrillDownRoute: "/academics/sessions?filter=low-attendance");

        // ACA-04: Coursework Submission Completion Rate
        var aca04Value = ComputePercentage(submissionRow.CompletedCount, submissionRow.TotalSubmissions);
        var aca04Status = submissionRow.TotalSubmissions == 0
            ? "neutral"
            : aca04Value >= 90.0m
                ? "healthy"
                : aca04Value >= 80.0m
                    ? "warning"
                    : "critical";

        var aca04Card = new KpiMetricCardDto(
            Code: "ACA-04",
            Label: "Coursework Submission Rate",
            Domain: "academics",
            Value: aca04Value,
            FormattedValue: FormatPercent(aca04Value),
            Unit: "percentage",
            PriorValue: null,
            DeltaPercent: null,
            DeltaLabel: null,
            TrendDirection: "flat",
            Status: aca04Status,
            TargetLabel: "Target: >= 90%",
            Subtitle: $"{submissionRow.CompletedCount} Completed / {submissionRow.LateCount} Late / {submissionRow.MissingCount} Missing",
            DrillDownRoute: "/academics/coursework?status=missing");

        // ACA-05: Assessment Result Publication Readiness
        var aca05Value = ComputePercentage(resultsRow.PublishedCount, resultsRow.TotalFinalResults);
        decimal? aca05Prior = resultsRow.PriorTotalFinalResults > 0
            ? ComputePercentage(resultsRow.PriorPublishedCount, resultsRow.PriorTotalFinalResults)
            : null;
        var aca05Delta = ComputeDelta(aca05Value, aca05Prior);
        var aca05Status = resultsRow.TotalFinalResults == 0
            ? "neutral"
            : aca05Value >= 90.0m
                ? "healthy"
                : aca05Value >= 75.0m
                    ? "warning"
                    : "critical";

        var aca05Card = new KpiMetricCardDto(
            Code: "ACA-05",
            Label: "Assessment Publication Readiness",
            Domain: "academics",
            Value: aca05Value,
            FormattedValue: FormatPercent(aca05Value),
            Unit: "percentage",
            PriorValue: aca05Prior,
            DeltaPercent: aca05Delta,
            DeltaLabel: FormatDeltaLabel(aca05Delta, priorShortName),
            TrendDirection: DetermineTrendDirection(aca05Delta),
            Status: aca05Status,
            TargetLabel: "Target: >= 90%",
            Subtitle: $"{resultsRow.PublishedCount} Published / {resultsRow.ApprovedCount} Approved / {resultsRow.ProvisionalCount} Provisional",
            DrillDownRoute: "/academics/assessment?status=provisional");

        // Diagnostic Breakdowns
        var standingBreakdown = new KpiBreakdownDto(
            Code: "ACA-STANDING-DISTRIBUTION",
            Title: "Academic Standing Distribution",
            Subtitle: "Evaluated students by formal standing tier",
            ChartType: "stacked-bar",
            Items:
            [
                new KpiBreakdownItemDto(
                    Key: "good_standing",
                    Label: "Good Standing",
                    Count: standingRow.GoodStandingCount,
                    Percentage: ComputePercentage(standingRow.GoodStandingCount, standingRow.TotalEvaluated),
                    Amount: null,
                    FormattedAmount: null,
                    Status: "healthy",
                    DrillDownRoute: "/academics/cohort-review?standing=good_standing"),
                new KpiBreakdownItemDto(
                    Key: "conditional_standing",
                    Label: "Conditional Standing",
                    Count: standingRow.ConditionalStandingCount,
                    Percentage: ComputePercentage(standingRow.ConditionalStandingCount, standingRow.TotalEvaluated),
                    Amount: null,
                    FormattedAmount: null,
                    Status: "warning",
                    DrillDownRoute: "/academics/cohort-review?standing=conditional_standing"),
                new KpiBreakdownItemDto(
                    Key: "not_in_good_standing",
                    Label: "Not in Good Standing",
                    Count: standingRow.NotInGoodStandingCount,
                    Percentage: ComputePercentage(standingRow.NotInGoodStandingCount, standingRow.TotalEvaluated),
                    Amount: null,
                    FormattedAmount: null,
                    Status: "critical",
                    DrillDownRoute: "/academics/cohort-review?standing=not_in_good_standing")
            ]);

        var submissionBreakdown = new KpiBreakdownDto(
            Code: "ACA-SUBMISSION-STATUS",
            Title: "Coursework Submission Breakdown",
            Subtitle: "Submission completion across released assignments",
            ChartType: "horizontal-bar",
            Items:
            [
                new KpiBreakdownItemDto(
                    Key: "submitted",
                    Label: "On-Time / Completed",
                    Count: submissionRow.CompletedCount,
                    Percentage: ComputePercentage(submissionRow.CompletedCount, submissionRow.TotalSubmissions),
                    Amount: null,
                    FormattedAmount: null,
                    Status: "healthy",
                    DrillDownRoute: "/academics/coursework?status=submitted"),
                new KpiBreakdownItemDto(
                    Key: "late",
                    Label: "Late Submissions",
                    Count: submissionRow.LateCount,
                    Percentage: ComputePercentage(submissionRow.LateCount, submissionRow.TotalSubmissions),
                    Amount: null,
                    FormattedAmount: null,
                    Status: "warning",
                    DrillDownRoute: "/academics/coursework?status=late"),
                new KpiBreakdownItemDto(
                    Key: "missing",
                    Label: "Missing Submissions",
                    Count: submissionRow.MissingCount,
                    Percentage: ComputePercentage(submissionRow.MissingCount, submissionRow.TotalSubmissions),
                    Amount: null,
                    FormattedAmount: null,
                    Status: "critical",
                    DrillDownRoute: "/academics/coursework?status=missing")
            ]);

        // Operational Attention Signals
        var signals = new List<AttentionSignalDto>();

        if (atRiskRow.UnaddressedCount >= 1)
        {
            signals.Add(new AttentionSignalDto(
                SignalCode: "ACA_AT_RISK_UNADDRESSED",
                Domain: "academics",
                Severity: atRiskRow.UnaddressedCount >= 2 ? "critical" : "warning",
                Title: "At-Risk Learners Without Intervention",
                Description: $"{atRiskRow.UnaddressedCount} at-risk learner(s) have no planned or in-progress academic intervention.",
                AffectedCount: atRiskRow.UnaddressedCount,
                ImpactLabel: $"{atRiskRow.UnaddressedCount} unaddressed learner(s)",
                ActionLabel: "Initiate Interventions",
                DrillDownRoute: "/academics/interventions?filter=unaddressed"));
        }

        if (attendanceRow.ChronicAbsenteeismCount >= 1)
        {
            signals.Add(new AttentionSignalDto(
                SignalCode: "ACA_CHRONIC_ABSENTEEISM",
                Domain: "academics",
                Severity: attendanceRow.ChronicAbsenteeismCount >= 4 ? "critical" : "warning",
                Title: "Learners Below Attendance Threshold",
                Description: $"{attendanceRow.ChronicAbsenteeismCount} learner(s) recorded period attendance below 85%.",
                AffectedCount: attendanceRow.ChronicAbsenteeismCount,
                ImpactLabel: $"{attendanceRow.ChronicAbsenteeismCount} learner(s) < 85%",
                ActionLabel: "Review Attendance",
                DrillDownRoute: "/academics/sessions?filter=low-attendance"));
        }

        if (submissionRow.MissingCount >= 1)
        {
            signals.Add(new AttentionSignalDto(
                SignalCode: "ACA_MISSING_SUBMISSIONS",
                Domain: "academics",
                Severity: submissionRow.MissingCount >= 5 ? "critical" : "warning",
                Title: "Missing Coursework Submissions",
                Description: $"{submissionRow.MissingCount} submission(s) remain missing on active released assignments.",
                AffectedCount: submissionRow.MissingCount,
                ImpactLabel: $"{submissionRow.MissingCount} missing item(s)",
                ActionLabel: "Open Coursework",
                DrillDownRoute: "/academics/coursework?status=missing"));
        }

        if (resultsRow.ProvisionalCount >= 1)
        {
            signals.Add(new AttentionSignalDto(
                SignalCode: "ACA_UNPUBLISHED_RESULTS",
                Domain: "academics",
                Severity: resultsRow.ProvisionalCount >= 5 ? "critical" : "warning",
                Title: "Provisional Assessment Results Awaiting Action",
                Description: $"{resultsRow.ProvisionalCount} final assessment result(s) remain in provisional status.",
                AffectedCount: resultsRow.ProvisionalCount,
                ImpactLabel: $"{resultsRow.ProvisionalCount} provisional result(s)",
                ActionLabel: "Review Assessments",
                DrillDownRoute: "/academics/assessment?status=provisional"));
        }

        var orderedSignals = signals
            .OrderBy(s => string.Equals(s.Severity, "critical", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ToList();

        return new AcademicsComputedMetrics(
            HeadlineKpis: [aca01Card, aca02Card, aca03Card, aca04Card, aca05Card],
            Breakdowns: [standingBreakdown, submissionBreakdown],
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

    private static decimal? ComputeDelta(decimal currentValue, decimal? priorValue)
    {
        if (!priorValue.HasValue)
        {
            return null;
        }

        return Math.Round(currentValue - priorValue.Value, 1, MidpointRounding.AwayFromZero);
    }

    private static string DetermineTrendDirection(decimal? deltaPercent)
    {
        if (!deltaPercent.HasValue || deltaPercent.Value == 0.0m)
        {
            return "flat";
        }

        return deltaPercent.Value > 0.0m ? "up" : "down";
    }

    private static string FormatPercent(decimal value)
        => string.Create(CultureInfo.InvariantCulture, $"{value:0.0}%");

    private static string? FormatDeltaLabel(decimal? deltaPercent, string? priorPeriodShortName)
    {
        if (!deltaPercent.HasValue || string.IsNullOrWhiteSpace(priorPeriodShortName))
        {
            return null;
        }

        var sign = deltaPercent.Value > 0.0m ? "+" : string.Empty;
        return string.Create(CultureInfo.InvariantCulture, $"{sign}{deltaPercent.Value:0.0}% vs {priorPeriodShortName}");
    }

    private static string? FormatPriorPeriodShortName(string? priorPeriodName)
    {
        if (string.IsNullOrWhiteSpace(priorPeriodName))
        {
            return null;
        }

        var trimmed = priorPeriodName.Trim();
        if (trimmed.StartsWith("Semester 1", StringComparison.OrdinalIgnoreCase))
        {
            return "Sem 1";
        }

        if (trimmed.StartsWith("Semester 2", StringComparison.OrdinalIgnoreCase))
        {
            return "Sem 2";
        }

        return trimmed;
    }

    internal sealed record AcademicsComputedMetrics(
        IReadOnlyList<KpiMetricCardDto> HeadlineKpis,
        IReadOnlyList<KpiBreakdownDto> Breakdowns,
        IReadOnlyList<AttentionSignalDto> AttentionSignals);

    private sealed record StandingSummaryRow(
        int TotalEvaluated,
        int GoodStandingCount,
        int ConditionalStandingCount,
        int NotInGoodStandingCount,
        int PriorTotalEvaluated,
        int PriorGoodStandingCount);

    private sealed record AtRiskSummaryRow(
        int TotalParticipatingStudents,
        int AtRiskCount,
        int CoveredCount,
        int UnaddressedCount);

    private sealed record AttendanceSummaryRow(
        int TotalAttendanceRecords,
        int AttendedRecords,
        int ChronicAbsenteeismCount,
        int PriorTotalAttendanceRecords,
        int PriorAttendedRecords);

    private sealed record SubmissionSummaryRow(
        int TotalSubmissions,
        int CompletedCount,
        int LateCount,
        int MissingCount);

    private sealed record AssessmentResultsSummaryRow(
        int TotalFinalResults,
        int PublishedCount,
        int ApprovedCount,
        int ProvisionalCount,
        int PriorTotalFinalResults,
        int PriorPublishedCount);
}
