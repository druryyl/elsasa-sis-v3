namespace Elsasa.Sis.IntegrationTests.Features.Kpis;

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Contracts.Kpis;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

/// <summary>
/// Integration tests for all five Level 2 Domain KPI Dashboard endpoints
/// (<c>GET /api/{domain}/kpis/dashboard</c>) verifying headline KPIs, prior-period comparison deltas,
/// non-zero diagnostic breakdowns (all 5 aging brackets in <c>FIN-04</c>, all 5 workload bands in <c>TCH-03</c>),
/// at-risk learners (<c>ACA-02</c>), unreviewed calendar amendments (<c>OPS-01</c>), campus filtering,
/// and domain authorization enforcement (P6-S13).
/// </summary>
public sealed class DomainKpiDashboardEndpointsTest(
    IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private async Task SeedDemoDataAsync(HttpClient client)
    {
        Factory.CurrentUser.Roles = ["SuperAdmin"];
        Factory.CurrentUser.Permissions = ["*"];

        var resetResponse = await client.PostAsJsonAsync(
            "/api/operations/demo-data/reset",
            new { cleanOnly = false });
        resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await resetResponse.Content.ReadFromJsonAsync<DemoResetSummary>();
        summary.Should().NotBeNull();
        summary!.TotalStudentsSeeded.Should().Be(48);

        Factory.CurrentUser.Reset();
    }

    [SkippableFact]
    public async Task GetOperationsKpiDashboard_Should_Return_Readiness_And_UnreviewedAmendments()
    {
        SkipIfDatabaseUnavailable();

        var client = Factory.CreateClient();
        await SeedDemoDataAsync(client);

        var response = await client.GetAsync("/api/operations/kpis/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<DomainKpiDashboardResponse>();
        payload.Should().NotBeNull();
        payload!.DomainKey.Should().Be("operations");
        payload.HeadlineKpis.Select(k => k.Code).Should().Equal("OPS-01", "OPS-02");

        var ops01 = payload.HeadlineKpis.Single(k => k.Code == "OPS-01");
        ops01.Value.Should().BeGreaterThan(0m).And.BeLessThan(100m, "unreviewed amendment lowers OPS-01 below 100%");

        var amendmentBreakdown = payload.Breakdowns.Single(b => b.Code == "OPS-AMENDMENT-REVIEW");
        var unreviewedItem = amendmentBreakdown.Items.Single(i => i.Key == "unreviewed");
        unreviewedItem.Count.Should().BeGreaterThan(0m, "seeded demo data includes an unreviewed calendar amendment");

        payload.AttentionSignals.Should().Contain(s => s.SignalCode == "OPS_UNREVIEWED_AMENDMENT" && s.AffectedCount >= 1);
    }

    [SkippableFact]
    public async Task GetStudentsKpiDashboard_Should_Return_AllMetrics_Breakdowns_And_Signals()
    {
        SkipIfDatabaseUnavailable();

        var client = Factory.CreateClient();
        await SeedDemoDataAsync(client);

        var response = await client.GetAsync("/api/students/kpis/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<DomainKpiDashboardResponse>();
        payload.Should().NotBeNull();
        payload!.DomainKey.Should().Be("students");
        payload.HeadlineKpis.Select(k => k.Code).Should().Equal("STD-01", "STD-02", "STD-03", "STD-06", "STD-07");

        payload.HeadlineKpis.Single(k => k.Code == "STD-01").Value.Should().Be(45m);
        payload.HeadlineKpis.Single(k => k.Code == "STD-03").Value.Should().BeGreaterThan(0m);
        payload.HeadlineKpis.Single(k => k.Code == "STD-06").Value.Should().BeGreaterThan(0m);
        payload.HeadlineKpis.Single(k => k.Code == "STD-07").Value.Should().BeGreaterThan(0m);

        payload.Breakdowns.Select(b => b.Code).Should().Equal("STD-FUNNEL", "STD-COHORT-FILL");
        payload.AttentionSignals.Should().Contain(s => s.SignalCode == "STD_ENROLLMENT_BLOCKED" && s.AffectedCount >= 1);
        payload.AttentionSignals.Should().Contain(s => s.SignalCode == "STD_LEAVE_EXPIRED" && s.AffectedCount >= 1);

        // Verify East Campus scoping
        var eastPayload = await client.GetFromJsonAsync<DomainKpiDashboardResponse>("/api/students/kpis/dashboard?campus=campus-east");
        eastPayload.Should().NotBeNull();
        eastPayload!.HeadlineKpis.Single(k => k.Code == "STD-01").Value.Should().Be(8m);
    }

    [SkippableFact]
    public async Task GetFinanceKpiDashboard_Should_Return_AllFiveAgingBrackets_WithNonZeroCounts_And_Signals()
    {
        SkipIfDatabaseUnavailable();

        var client = Factory.CreateClient();
        await SeedDemoDataAsync(client);

        var response = await client.GetAsync("/api/finance/kpis/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<DomainKpiDashboardResponse>();
        payload.Should().NotBeNull();
        payload!.DomainKey.Should().Be("finance");
        payload.HeadlineKpis.Select(k => k.Code).Should().Equal("FIN-01", "FIN-02", "FIN-03", "FIN-04", "FIN-05");

        var fin01 = payload.HeadlineKpis.Single(k => k.Code == "FIN-01");
        fin01.Value.Should().BeGreaterThan(0m).And.BeLessThanOrEqualTo(100m, "RISK-003 excludes negative scholarship credit rows");
        fin01.PriorValue.Should().NotBeNull();
        fin01.DeltaPercent.Should().NotBeNull();

        var agingBreakdown = payload.Breakdowns.Single(b => b.Code == "FIN-04");
        agingBreakdown.Items.Select(i => i.Key).Should().Equal(
            "current",
            "1-30-days",
            "31-60-days",
            "61-90-days",
            "90-plus-days");

        agingBreakdown.Items.Should().AllSatisfy(item =>
        {
            item.Count.Should().BeGreaterThan(0m, $"aging bracket '{item.Key}' should have non-zero accounts");
            item.Amount.Should().BeGreaterThan(0m, $"aging bracket '{item.Key}' should have non-zero outstanding amount");
        });

        payload.AttentionSignals.Should().Contain(s => s.SignalCode == "FIN_OVERDUE_90_PLUS" && s.AffectedCount >= 1);
        payload.AttentionSignals.Should().Contain(s => s.SignalCode == "FIN_UNVERIFIED_PAYMENTS" && s.AffectedCount >= 1);
        payload.AttentionSignals.Should().Contain(s => s.SignalCode == "FIN_UNALLOCATED_CASH" && s.AffectedCount >= 1);
    }

    [SkippableFact]
    public async Task GetAcademicsKpiDashboard_Should_Return_PriorComparison_AtRiskLearners_And_Signals()
    {
        SkipIfDatabaseUnavailable();

        var client = Factory.CreateClient();
        await SeedDemoDataAsync(client);

        var response = await client.GetAsync("/api/academics/kpis/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<DomainKpiDashboardResponse>();
        payload.Should().NotBeNull();
        payload!.DomainKey.Should().Be("academics");
        payload.HeadlineKpis.Select(k => k.Code).Should().Equal("ACA-01", "ACA-02", "ACA-03", "ACA-04", "ACA-05");

        var aca01 = payload.HeadlineKpis.Single(k => k.Code == "ACA-01");
        aca01.PriorValue.Should().NotBeNull();
        aca01.DeltaPercent.Should().BeGreaterThan(0m, "Semester 2 good standing improves over Semester 1");

        var aca02 = payload.HeadlineKpis.Single(k => k.Code == "ACA-02");
        aca02.Value.Should().BeGreaterThan(0m, "seeded demo data includes unaddressed at-risk learners");

        payload.Breakdowns.Select(b => b.Code).Should().Equal("ACA-STANDING-DISTRIBUTION", "ACA-SUBMISSION-STATUS");
        payload.AttentionSignals.Should().Contain(s => s.SignalCode == "ACA_AT_RISK_UNADDRESSED" && s.AffectedCount >= 1);
        payload.AttentionSignals.Should().Contain(s => s.SignalCode == "ACA_CHRONIC_ABSENTEEISM" && s.AffectedCount >= 1);
        payload.AttentionSignals.Should().Contain(s => s.SignalCode == "ACA_MISSING_SUBMISSIONS" && s.AffectedCount >= 1);
        payload.AttentionSignals.Should().Contain(s => s.SignalCode == "ACA_UNPUBLISHED_RESULTS" && s.AffectedCount >= 1);
    }

    [SkippableFact]
    public async Task GetTeachingKpiDashboard_Should_Return_AllFiveWorkloadBands_WithNonZeroCounts_And_Conflicts()
    {
        SkipIfDatabaseUnavailable();

        var client = Factory.CreateClient();
        await SeedDemoDataAsync(client);

        var response = await client.GetAsync("/api/teaching/kpis/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<DomainKpiDashboardResponse>();
        payload.Should().NotBeNull();
        payload!.DomainKey.Should().Be("teaching");
        payload.HeadlineKpis.Select(k => k.Code).Should().Equal("TCH-01", "TCH-02", "TCH-03", "TCH-04");

        var workloadBreakdown = payload.Breakdowns.Single(b => b.Code == "TCH-03-WORKLOAD-BANDS");
        workloadBreakdown.Items.Select(i => i.Key).Should().Equal(
            "under_allocated",
            "normal",
            "approaching_limit",
            "overloaded_with_exception",
            "breached");

        workloadBreakdown.Items.Should().AllSatisfy(band =>
            band.Count.Should().BeGreaterThan(0m, $"workload band '{band.Key}' should be populated in demo data"));

        var conflictsBreakdown = payload.Breakdowns.Single(b => b.Code == "TCH-04-CONFLICTS-QUALIFICATIONS");
        conflictsBreakdown.Items.Single(i => i.Key == "blocking").Count.Should().BeGreaterThan(0m);
        conflictsBreakdown.Items.Single(i => i.Key == "advisory").Count.Should().BeGreaterThan(0m);

        payload.AttentionSignals.Should().Contain(s => s.SignalCode == "TCH_BLOCKING_CONFLICT" && s.AffectedCount >= 1);
        payload.AttentionSignals.Should().Contain(s => s.SignalCode == "TCH_WORKLOAD_BREACH" && s.AffectedCount >= 1);
        payload.AttentionSignals.Should().Contain(s => s.SignalCode == "TCH_UNSTAFFED_DEMAND" && s.AffectedCount >= 1);
    }

    [SkippableFact]
    public async Task GetDomainKpiDashboards_Should_Return_Forbidden_When_CallerLacksDomainPermission()
    {
        SkipIfDatabaseUnavailable();

        var client = Factory.CreateClient();

        Factory.CurrentUser.SetUser(
            id: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            username: "finance-only-user",
            roles: ["FinanceOfficer"],
            permissions: ["Permissions.Finance.Obligations.View"]);

        var studentsResponse = await client.GetAsync("/api/students/kpis/dashboard");
        studentsResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var teachingResponse = await client.GetAsync("/api/teaching/kpis/dashboard");
        teachingResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var operationsResponse = await client.GetAsync("/api/operations/kpis/dashboard");
        operationsResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var academicsResponse = await client.GetAsync("/api/academics/kpis/dashboard");
        academicsResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
