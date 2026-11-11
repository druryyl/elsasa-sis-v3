namespace Elsasa.Sis.Students.Features.Kpis.GetStudentsKpiDashboard;

using System.Globalization;
using Dapper;
using Elsasa.Sis.Contracts.Kpis;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Computes Release 1 Students domain KPI metrics (<c>STD-01</c>, <c>STD-02</c>, <c>STD-03</c>, <c>STD-06</c>, <c>STD-07</c>),
/// diagnostic breakdowns, and attention signals via a single parameterized Dapper <c>QueryMultipleAsync</c> batch.
/// </summary>
public sealed class GetStudentsKpiDashboardHandler(
    ISqlConnectionFactory connectionFactory,
    IKpiPeriodResolver periodResolver)
{
    private const int _standardSectionCapacity = 20;

    private const string _studentsKpiBatchSql = """
        -- 1. Student status & attrition counts (STD-01, STD-06)
        SELECT
            COUNT(DISTINCT CASE WHEN ss.status = N'active' THEN s.id END) AS ActiveCount,
            COUNT(DISTINCT CASE WHEN ss.status IN (N'withdrawn', N'transferred_out') THEN s.id END) AS AttritionCount,
            COUNT(DISTINCT CASE WHEN ss.status = N'on_leave' THEN s.id END) AS OnLeaveCount,
            COUNT(DISTINCT s.id) AS TotalEnrolledCount,
            COALESCE(
                CAST(COUNT(DISTINCT CASE WHEN ss.status IN (N'withdrawn', N'transferred_out') THEN s.id END) AS DECIMAL(18, 4)) * 100.0
                / NULLIF(CAST(COUNT(DISTINCT s.id) AS DECIMAL(18, 4)), 0),
                0.0
            ) AS AttritionRatePercent
        FROM students.students s
        INNER JOIN students.student_statuses ss
            ON ss.student_id = s.id
           AND ss.is_current = 1
        INNER JOIN students.administrative_placements ap
            ON ap.student_id = s.id
           AND ap.is_current = 1
        WHERE (@Campus IS NULL OR ap.campus = @Campus)
          AND (@SchoolLevel IS NULL OR ap.school_level = @SchoolLevel);

        -- 2. Admission pipeline counts & conversion rate (STD-02, STD_ENROLLMENT_BLOCKED)
        SELECT
            COUNT(CASE WHEN aa.status = N'submitted' THEN 1 END) AS SubmittedCount,
            COUNT(CASE WHEN aa.status = N'under_review' THEN 1 END) AS UnderReviewCount,
            COUNT(CASE WHEN aa.status = N'entrance_assessed' THEN 1 END) AS EntranceAssessedCount,
            COUNT(CASE WHEN aa.status = N'waitlisted' THEN 1 END) AS WaitlistedCount,
            COUNT(CASE WHEN aa.status = N'accepted' AND e.id IS NULL THEN 1 END) AS AcceptedUnenrolledCount,
            COUNT(CASE WHEN e.id IS NOT NULL OR aa.status = N'enrolled' THEN 1 END) AS EnrolledCount,
            COUNT(CASE WHEN aa.status IN (N'withdrawn', N'rejected') THEN 1 END) AS WithdrawnRejectedCount,
            COUNT(CASE WHEN aa.status IN (
                N'submitted', N'under_review', N'entrance_assessed',
                N'waitlisted', N'accepted', N'rejected', N'withdrawn', N'enrolled'
            ) THEN 1 END) AS TotalSubmittedOrBeyondCount,
            COALESCE(
                CAST(COUNT(CASE WHEN e.id IS NOT NULL OR aa.status = N'enrolled' THEN 1 END) AS DECIMAL(18, 4)) * 100.0
                / NULLIF(CAST(COUNT(CASE WHEN aa.status IN (
                    N'submitted', N'under_review', N'entrance_assessed',
                    N'waitlisted', N'accepted', N'rejected', N'withdrawn', N'enrolled'
                ) THEN 1 END) AS DECIMAL(18, 4)), 0),
                0.0
            ) AS ConversionRatePercent
        FROM students.admission_applications aa
        LEFT JOIN students.enrollments e
            ON e.application_id = aa.id
        WHERE aa.academic_year_id = @AcademicYearId
          AND (@Campus IS NULL OR aa.requested_campus = @Campus)
          AND (
              @SchoolLevel IS NULL
              OR aa.requested_entry_level = @SchoolLevel
              OR (@SchoolLevel = N'ELC' AND aa.requested_entry_level IN (N'Grade 1', N'Grade 2', N'Grade 3', N'Grade 4', N'Grade 5', N'Grade 6'))
              OR (@SchoolLevel = N'JHLC' AND aa.requested_entry_level IN (N'Grade 7', N'Grade 8', N'Grade 9'))
              OR (@SchoolLevel = N'HSLC' AND aa.requested_entry_level IN (N'Grade 10', N'Grade 11', N'Grade 12'))
          );

        -- 3. Applicant document verification counts (STD-03)
        SELECT
            COUNT(CASE WHEN doc.submission_status = N'verified' THEN 1 END) AS VerifiedCount,
            COUNT(CASE WHEN doc.submission_status IN (N'not_submitted', N'submitted', N'rejected') THEN 1 END) AS PendingCount,
            COUNT(doc.id) AS TotalCount,
            COALESCE(
                CAST(COUNT(CASE WHEN doc.submission_status = N'verified' THEN 1 END) AS DECIMAL(18, 4)) * 100.0
                / NULLIF(CAST(COUNT(doc.id) AS DECIMAL(18, 4)), 0),
                0.0
            ) AS VerificationRatePercent
        FROM students.applicant_documents doc
        INNER JOIN students.admission_applications aa
            ON aa.id = doc.application_id
        WHERE aa.academic_year_id = @AcademicYearId
          AND aa.status IN (N'submitted', N'under_review', N'accepted')
          AND (@Campus IS NULL OR aa.requested_campus = @Campus)
          AND (
              @SchoolLevel IS NULL
              OR aa.requested_entry_level = @SchoolLevel
              OR (@SchoolLevel = N'ELC' AND aa.requested_entry_level IN (N'Grade 1', N'Grade 2', N'Grade 3', N'Grade 4', N'Grade 5', N'Grade 6'))
              OR (@SchoolLevel = N'JHLC' AND aa.requested_entry_level IN (N'Grade 7', N'Grade 8', N'Grade 9'))
              OR (@SchoolLevel = N'HSLC' AND aa.requested_entry_level IN (N'Grade 10', N'Grade 11', N'Grade 12'))
          );

        -- 4. Cohort & section fill utilization (STD-07)
        SELECT
            ap.campus AS Campus,
            ap.school_level AS SchoolLevel,
            COALESCE(NULLIF(LTRIM(RTRIM(ap.grade)), N''), N'Unassigned') AS Grade,
            COALESCE(NULLIF(LTRIM(RTRIM(ap.cohort)), N''), ap.school_level) AS Cohort,
            COUNT(DISTINCT s.id) AS StudentCount
        FROM students.students s
        INNER JOIN students.student_statuses ss
            ON ss.student_id = s.id
           AND ss.is_current = 1
           AND ss.status = N'active'
        INNER JOIN students.administrative_placements ap
            ON ap.student_id = s.id
           AND ap.is_current = 1
        WHERE (@Campus IS NULL OR ap.campus = @Campus)
          AND (@SchoolLevel IS NULL OR ap.school_level = @SchoolLevel)
        GROUP BY
            ap.campus,
            ap.school_level,
            COALESCE(NULLIF(LTRIM(RTRIM(ap.grade)), N''), N'Unassigned'),
            COALESCE(NULLIF(LTRIM(RTRIM(ap.cohort)), N''), ap.school_level)
        ORDER BY
            ap.campus ASC,
            ap.school_level ASC,
            Grade ASC,
            Cohort ASC;

        -- 5. Expired leave signal count (STD_LEAVE_EXPIRED)
        SELECT
            COUNT(DISTINCT s.id) AS ExpiredLeaveCount
        FROM students.students s
        INNER JOIN students.student_statuses ss
            ON ss.student_id = s.id
           AND ss.is_current = 1
        INNER JOIN students.administrative_placements ap
            ON ap.student_id = s.id
           AND ap.is_current = 1
        OUTER APPLY (
            SELECT TOP (1) ahe.event_date
            FROM students.administrative_history_entries ahe
            WHERE ahe.student_id = s.id
              AND ahe.event_type = N'leave_of_absence'
              AND ahe.is_correction = 0
            ORDER BY ahe.event_date DESC, ahe.created_at DESC
        ) latest_leave
        WHERE (
            ss.status = N'leave_expired'
            OR (
                ss.status = N'on_leave'
                AND COALESCE(latest_leave.event_date, ss.effective_date) <= DATEADD(day, -14, CAST(SYSUTCDATETIME() AS DATE))
            )
        )
          AND (@Campus IS NULL OR ap.campus = @Campus)
          AND (@SchoolLevel IS NULL OR ap.school_level = @SchoolLevel);
        """;

    /// <summary>
    /// Executes the Level 2 Students KPI dashboard query for the supplied global filter.
    /// </summary>
    public async Task<Result<DomainKpiDashboardResponse>> HandleAsync(
        GlobalKpiFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var context = await periodResolver.ResolveContextAsync(filter, cancellationToken);
        var availablePeriods = await periodResolver.GetAvailablePeriodsAsync(context.AcademicYearId, cancellationToken);
        var snapshot = await ComputeSnapshotAsync(context, cancellationToken);

        return Result.Success(new DomainKpiDashboardResponse(
            DomainKey: "students",
            DomainName: "Students",
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Context: context,
            AvailablePeriods: availablePeriods,
            HeadlineKpis: snapshot.HeadlineKpis,
            Breakdowns: snapshot.Breakdowns,
            AttentionSignals: snapshot.AttentionSignals));
    }

    internal async Task<StudentsKpiSnapshot> ComputeSnapshotAsync(
        ResolvedKpiContextDto context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        await using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            _studentsKpiBatchSql,
            new
            {
                context.AcademicYearId,
                context.Campus,
                context.SchoolLevel
            },
            cancellationToken: cancellationToken));

        var population = await multi.ReadSingleAsync<StudentPopulationRow>();
        var admission = await multi.ReadSingleAsync<AdmissionPipelineRow>();
        var documents = await multi.ReadSingleAsync<DocumentVerificationRow>();
        var cohortSections = (await multi.ReadAsync<CohortSectionRow>()).AsList();
        var expiredLeaveCount = await multi.ReadSingleAsync<int>();

        var std01 = BuildStd01Card(population);
        var std02 = BuildStd02Card(admission);
        var std03 = BuildStd03Card(documents);
        var std06 = BuildStd06Card(population);
        var std07 = BuildStd07Card(cohortSections);

        var headlineKpis = new KpiMetricCardDto[]
        {
            std01,
            std02,
            std03,
            std06,
            std07
        };

        var breakdowns = new KpiBreakdownDto[]
        {
            BuildAdmissionFunnelBreakdown(admission),
            BuildCohortFillBreakdown(cohortSections)
        };

        var signals = BuildAttentionSignals(admission.AcceptedUnenrolledCount, expiredLeaveCount);

        return new StudentsKpiSnapshot(
            Std01: std01,
            Std06: std06,
            HeadlineKpis: headlineKpis,
            Breakdowns: breakdowns,
            AttentionSignals: signals);
    }

    private static KpiMetricCardDto BuildStd01Card(StudentPopulationRow population)
    {
        var status = population.TotalEnrolledCount == 0
            ? "neutral"
            : (population.ActiveCount > 0 ? "healthy" : "warning");

        return new KpiMetricCardDto(
            Code: "STD-01",
            Label: "Active Enrolled Students",
            Domain: "students",
            Value: population.ActiveCount,
            FormattedValue: population.ActiveCount.ToString("N0", CultureInfo.InvariantCulture),
            Unit: "count",
            PriorValue: null,
            DeltaPercent: null,
            DeltaLabel: null,
            TrendDirection: "flat",
            Status: status,
            TargetLabel: "Active Enrollment",
            Subtitle: $"{population.ActiveCount} Active / {population.AttritionCount} Exits / {population.OnLeaveCount} On Leave",
            DrillDownRoute: "/students/records?status=active");
    }

    private static KpiMetricCardDto BuildStd02Card(AdmissionPipelineRow admission)
    {
        var rate = Math.Round(admission.ConversionRatePercent, 1, MidpointRounding.AwayFromZero);
        var status = admission.TotalSubmittedOrBeyondCount == 0
            ? "neutral"
            : admission.AcceptedUnenrolledCount >= 4
                ? "critical"
                : admission.AcceptedUnenrolledCount >= 1
                    ? "warning"
                    : "healthy";

        return new KpiMetricCardDto(
            Code: "STD-02",
            Label: "Admission Pipeline Conversion",
            Domain: "students",
            Value: rate,
            FormattedValue: $"{rate.ToString("0.0", CultureInfo.InvariantCulture)}%",
            Unit: "percentage",
            PriorValue: null,
            DeltaPercent: null,
            DeltaLabel: null,
            TrendDirection: "flat",
            Status: status,
            TargetLabel: "Gate Backlog: 0",
            Subtitle: $"{admission.EnrolledCount} of {admission.TotalSubmittedOrBeyondCount} enrolled · {admission.AcceptedUnenrolledCount} awaiting enrollment",
            DrillDownRoute: "/students/admission?status=accepted");
    }

    private static KpiMetricCardDto BuildStd03Card(DocumentVerificationRow documents)
    {
        var rate = Math.Round(documents.VerificationRatePercent, 1, MidpointRounding.AwayFromZero);
        var status = documents.TotalCount == 0
            ? "neutral"
            : rate >= 90.0m
                ? "healthy"
                : rate >= 75.0m
                    ? "warning"
                    : "critical";

        return new KpiMetricCardDto(
            Code: "STD-03",
            Label: "Document Verification Rate",
            Domain: "students",
            Value: rate,
            FormattedValue: $"{rate.ToString("0.0", CultureInfo.InvariantCulture)}%",
            Unit: "percentage",
            PriorValue: null,
            DeltaPercent: null,
            DeltaLabel: null,
            TrendDirection: "flat",
            Status: status,
            TargetLabel: "Target: >= 90%",
            Subtitle: $"{documents.VerifiedCount} of {documents.TotalCount} verified ({documents.PendingCount} pending)",
            DrillDownRoute: "/students/admission?docStatus=pending");
    }

    private static KpiMetricCardDto BuildStd06Card(StudentPopulationRow population)
    {
        var rate = Math.Round(population.AttritionRatePercent, 2, MidpointRounding.AwayFromZero);
        var status = population.TotalEnrolledCount == 0
            ? "neutral"
            : rate <= 3.0m
                ? "healthy"
                : rate <= 6.0m
                    ? "warning"
                    : "critical";

        return new KpiMetricCardDto(
            Code: "STD-06",
            Label: "Attrition Rate",
            Domain: "students",
            Value: rate,
            FormattedValue: $"{rate.ToString("0.00", CultureInfo.InvariantCulture)}%",
            Unit: "percentage",
            PriorValue: null,
            DeltaPercent: null,
            DeltaLabel: null,
            TrendDirection: "flat",
            Status: status,
            TargetLabel: "Target: <= 3.0%",
            Subtitle: $"{population.AttritionCount} exits of {population.TotalEnrolledCount} enrolled",
            DrillDownRoute: "/students/lifecycle?status=withdrawn");
    }

    private static KpiMetricCardDto BuildStd07Card(IReadOnlyList<CohortSectionRow> sections)
    {
        var totalSections = sections.Count;
        var totalActive = sections.Sum(s => s.StudentCount);
        var totalCapacity = totalSections * _standardSectionCapacity;
        var utilizationRate = totalCapacity == 0
            ? 0.0m
            : Math.Round((decimal)totalActive * 100.0m / totalCapacity, 1, MidpointRounding.AwayFromZero);

        var status = totalSections == 0
            ? "neutral"
            : (utilizationRate >= 75.0m && utilizationRate <= 95.0m ? "healthy" : "warning");

        return new KpiMetricCardDto(
            Code: "STD-07",
            Label: "Cohort Fill Utilization",
            Domain: "students",
            Value: utilizationRate,
            FormattedValue: $"{utilizationRate.ToString("0.0", CultureInfo.InvariantCulture)}%",
            Unit: "percentage",
            PriorValue: null,
            DeltaPercent: null,
            DeltaLabel: null,
            TrendDirection: "flat",
            Status: status,
            TargetLabel: "Target: 75%–95%",
            Subtitle: $"{totalActive} active across {totalSections} sections (cap. {_standardSectionCapacity}/section)",
            DrillDownRoute: "/students/records?status=active");
    }

    private static KpiBreakdownDto BuildAdmissionFunnelBreakdown(AdmissionPipelineRow admission)
    {
        var total = admission.TotalSubmittedOrBeyondCount;

        static decimal Share(int count, int denominator) =>
            denominator == 0 ? 0.0m : Math.Round((decimal)count * 100.0m / denominator, 1, MidpointRounding.AwayFromZero);

        var items = new List<KpiBreakdownItemDto>
        {
            new(
                Key: "submitted",
                Label: "Submitted",
                Count: admission.SubmittedCount,
                Percentage: Share(admission.SubmittedCount, total),
                Amount: null,
                FormattedAmount: null,
                Status: "neutral",
                DrillDownRoute: "/students/admission?status=submitted"),
            new(
                Key: "under_review",
                Label: "Under Review",
                Count: admission.UnderReviewCount,
                Percentage: Share(admission.UnderReviewCount, total),
                Amount: null,
                FormattedAmount: null,
                Status: "neutral",
                DrillDownRoute: "/students/admission?status=under_review"),
            new(
                Key: "entrance_assessed",
                Label: "Entrance Assessed",
                Count: admission.EntranceAssessedCount,
                Percentage: Share(admission.EntranceAssessedCount, total),
                Amount: null,
                FormattedAmount: null,
                Status: "neutral",
                DrillDownRoute: "/students/admission?status=entrance_assessed"),
        };

        if (admission.WaitlistedCount > 0)
        {
            items.Add(new KpiBreakdownItemDto(
                Key: "waitlisted",
                Label: "Waitlisted",
                Count: admission.WaitlistedCount,
                Percentage: Share(admission.WaitlistedCount, total),
                Amount: null,
                FormattedAmount: null,
                Status: "warning",
                DrillDownRoute: "/students/admission?status=waitlisted"));
        }

        var acceptedStatus = admission.AcceptedUnenrolledCount >= 4
            ? "critical"
            : admission.AcceptedUnenrolledCount >= 1
                ? "warning"
                : "healthy";

        items.Add(new KpiBreakdownItemDto(
            Key: "accepted",
            Label: "Accepted (Awaiting Enrollment)",
            Count: admission.AcceptedUnenrolledCount,
            Percentage: Share(admission.AcceptedUnenrolledCount, total),
            Amount: null,
            FormattedAmount: null,
            Status: acceptedStatus,
            DrillDownRoute: "/students/admission?status=accepted"));

        items.Add(new KpiBreakdownItemDto(
            Key: "enrolled",
            Label: "Enrolled",
            Count: admission.EnrolledCount,
            Percentage: Share(admission.EnrolledCount, total),
            Amount: null,
            FormattedAmount: null,
            Status: "healthy",
            DrillDownRoute: "/students/records?status=active"));

        items.Add(new KpiBreakdownItemDto(
            Key: "withdrawn_rejected",
            Label: "Withdrawn / Rejected",
            Count: admission.WithdrawnRejectedCount,
            Percentage: Share(admission.WithdrawnRejectedCount, total),
            Amount: null,
            FormattedAmount: null,
            Status: "neutral",
            DrillDownRoute: "/students/admission?status=withdrawn"));

        return new KpiBreakdownDto(
            Code: "STD-FUNNEL",
            Title: "Admission Pipeline Funnel",
            Subtitle: "Applications by intake stage from submission to enrollment",
            ChartType: "funnel",
            Items: items);
    }

    private static KpiBreakdownDto BuildCohortFillBreakdown(IReadOnlyList<CohortSectionRow> sections)
    {
        var items = new List<KpiBreakdownItemDto>(sections.Count);

        foreach (var section in sections)
        {
            var pct = Math.Round((decimal)section.StudentCount * 100.0m / _standardSectionCapacity, 1, MidpointRounding.AwayFromZero);
            var status = pct >= 75.0m && pct <= 95.0m ? "healthy" : "warning";
            var campusLabel = section.Campus switch
            {
                "campus-main" => "Main",
                "campus-east" => "East",
                _ => section.Campus
            };

            items.Add(new KpiBreakdownItemDto(
                Key: $"{section.Campus}:{section.SchoolLevel}:{section.Grade}:{section.Cohort}".ToLowerInvariant(),
                Label: $"{campusLabel} · {section.Grade} ({section.Cohort})",
                Count: section.StudentCount,
                Percentage: pct,
                Amount: null,
                FormattedAmount: null,
                Status: status,
                DrillDownRoute: $"/students/records?status=active&campus={Uri.EscapeDataString(section.Campus)}&schoolLevel={Uri.EscapeDataString(section.SchoolLevel)}&cohort={Uri.EscapeDataString(section.Cohort)}"));
        }

        return new KpiBreakdownDto(
            Code: "STD-COHORT-FILL",
            Title: "Cohort & Section Fill Utilization",
            Subtitle: $"Active learners per section against {_standardSectionCapacity}-student capacity standard",
            ChartType: "horizontal-bar",
            Items: items);
    }

    private static List<AttentionSignalDto> BuildAttentionSignals(
        int acceptedUnenrolledCount,
        int expiredLeaveCount)
    {
        var signals = new List<AttentionSignalDto>();

        if (acceptedUnenrolledCount >= 1)
        {
            var severity = acceptedUnenrolledCount >= 4 ? "critical" : "warning";
            var noun = acceptedUnenrolledCount == 1 ? "applicant" : "applicants";
            var verb = acceptedUnenrolledCount == 1 ? "has" : "have";

            signals.Add(new AttentionSignalDto(
                SignalCode: "STD_ENROLLMENT_BLOCKED",
                Domain: "students",
                Severity: severity,
                Title: "Accepted Applicants Awaiting Enrollment",
                Description: $"{acceptedUnenrolledCount} accepted {noun} {verb} not completed enrollment activation.",
                AffectedCount: acceptedUnenrolledCount,
                ImpactLabel: $"{acceptedUnenrolledCount} {noun} blocked",
                ActionLabel: "Review Admissions",
                DrillDownRoute: "/students/admission?status=accepted"));
        }

        if (expiredLeaveCount >= 1)
        {
            var severity = expiredLeaveCount >= 3 ? "critical" : "warning";
            var studentNoun = expiredLeaveCount == 1 ? "student" : "students";
            var leaveNoun = expiredLeaveCount == 1 ? "leave" : "leaves";
            var verb = expiredLeaveCount == 1 ? "has" : "have";

            signals.Add(new AttentionSignalDto(
                SignalCode: "STD_LEAVE_EXPIRED",
                Domain: "students",
                Severity: severity,
                Title: "Students on Leave Past Return Window",
                Description: $"{expiredLeaveCount} {studentNoun} {verb} been on leave for more than 14 days without return or exit resolution.",
                AffectedCount: expiredLeaveCount,
                ImpactLabel: $"{expiredLeaveCount} expired {leaveNoun}",
                ActionLabel: "Open Lifecycle",
                DrillDownRoute: "/students/lifecycle?status=on_leave"));
        }

        return signals;
    }

    internal sealed record StudentsKpiSnapshot(
        KpiMetricCardDto Std01,
        KpiMetricCardDto Std06,
        IReadOnlyList<KpiMetricCardDto> HeadlineKpis,
        IReadOnlyList<KpiBreakdownDto> Breakdowns,
        IReadOnlyList<AttentionSignalDto> AttentionSignals);

    private sealed record StudentPopulationRow(
        int ActiveCount,
        int AttritionCount,
        int OnLeaveCount,
        int TotalEnrolledCount,
        decimal AttritionRatePercent);

    private sealed record AdmissionPipelineRow(
        int SubmittedCount,
        int UnderReviewCount,
        int EntranceAssessedCount,
        int WaitlistedCount,
        int AcceptedUnenrolledCount,
        int EnrolledCount,
        int WithdrawnRejectedCount,
        int TotalSubmittedOrBeyondCount,
        decimal ConversionRatePercent);

    private sealed record DocumentVerificationRow(
        int VerifiedCount,
        int PendingCount,
        int TotalCount,
        decimal VerificationRatePercent);

    private sealed record CohortSectionRow(
        string Campus,
        string SchoolLevel,
        string Grade,
        string Cohort,
        int StudentCount);
}
