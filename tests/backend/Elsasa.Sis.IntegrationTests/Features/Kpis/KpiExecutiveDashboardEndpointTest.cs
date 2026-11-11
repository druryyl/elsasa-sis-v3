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
/// Integration tests for the Level 1 Executive Overview endpoint (<c>GET /api/dashboard/executive</c>)
/// verifying headline KPIs, domain health summaries (including Stage 3 Development placeholder),
/// prioritized attention queue, campus/school-level filtering, and <c>DEC-006</c> permission decoration (P6-S13).
/// </summary>
public sealed class KpiExecutiveDashboardEndpointTest(
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
    public async Task GetExecutiveDashboard_Should_Return_FiveHeadlineKpis_SixDomainSummaries_And_SortedAttentionQueue()
    {
        SkipIfDatabaseUnavailable();

        var client = Factory.CreateClient();
        await SeedDemoDataAsync(client);

        var response = await client.GetAsync("/api/dashboard/executive");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ExecutiveDashboardResponse>();
        payload.Should().NotBeNull();

        payload!.Context.AcademicYearCode.Should().Be("AY-2026-2027");
        payload.Context.AcademicPeriodName.Should().Contain("Semester 2");
        payload.Context.PriorAcademicPeriodId.Should().NotBeNull("Semester 1 comparison baseline should resolve automatically");
        payload.Context.Campus.Should().BeNull();
        payload.Context.SchoolLevel.Should().BeNull();
        payload.AvailablePeriods.Should().HaveCountGreaterThanOrEqualTo(2);

        payload.HeadlineKpis.Select(k => k.Code).Should().Equal(
            "STD-01",
            "ACA-01",
            "FIN-01",
            "TCH-01",
            "OPS-01");

        payload.HeadlineKpis.Should().AllSatisfy(kpi =>
        {
            kpi.FormattedValue.Should().NotBeNullOrWhiteSpace();
            kpi.DrillDownRoute.Should().StartWith("/");
        });

        payload.DomainSummaries.Should().HaveCount(6);
        payload.DomainSummaries.Select(d => d.DomainKey).Should().Equal(
            "students",
            "academics",
            "finance",
            "teaching",
            "operations",
            "development");

        var developmentCard = payload.DomainSummaries.Single(d => d.DomainKey == "development");
        developmentCard.Status.Should().Be("pending_stage");
        developmentCard.StatusLabel.Should().Be("Stage 3 Pending");
        developmentCard.DashboardRoute.Should().Be("/dashboard/development");
        developmentCard.IsAccessible.Should().BeTrue();

        payload.DomainSummaries.Where(d => d.DomainKey != "development").Should().AllSatisfy(d =>
        {
            d.IsAccessible.Should().BeTrue();
            d.DashboardRoute.Should().StartWith("/dashboard/");
        });

        payload.NeedsAttentionQueue.Should().NotBeEmpty();
        payload.NeedsAttentionQueue.Count.Should().BeLessThanOrEqualTo(12);

        // Verify critical signals appear before warning signals, and descending AffectedCount within severity
        var severityRanks = payload.NeedsAttentionQueue
            .Select(s => string.Equals(s.Severity, "critical", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ToList();
        severityRanks.Should().BeInAscendingOrder("critical signals must precede warning signals");
        severityRanks.Should().Contain(0, "seeded demo data should raise at least one critical signal");
        severityRanks.Should().Contain(1, "seeded demo data should raise at least one warning signal");
    }

    [SkippableFact]
    public async Task GetExecutiveDashboard_Should_Filter_ByCampus_And_SchoolLevel()
    {
        SkipIfDatabaseUnavailable();

        var client = Factory.CreateClient();
        await SeedDemoDataAsync(client);

        var allPayload = await client.GetFromJsonAsync<ExecutiveDashboardResponse>("/api/dashboard/executive");
        var mainPayload = await client.GetFromJsonAsync<ExecutiveDashboardResponse>("/api/dashboard/executive?campus=campus-main");
        var eastPayload = await client.GetFromJsonAsync<ExecutiveDashboardResponse>("/api/dashboard/executive?campus=campus-east");
        var elcPayload = await client.GetFromJsonAsync<ExecutiveDashboardResponse>("/api/dashboard/executive?schoolLevel=ELC");

        allPayload.Should().NotBeNull();
        mainPayload.Should().NotBeNull();
        eastPayload.Should().NotBeNull();
        elcPayload.Should().NotBeNull();

        mainPayload!.Context.Campus.Should().Be("campus-main");
        eastPayload!.Context.Campus.Should().Be("campus-east");
        elcPayload!.Context.SchoolLevel.Should().Be("ELC");

        var allStd01 = allPayload!.HeadlineKpis.Single(k => k.Code == "STD-01").Value;
        var mainStd01 = mainPayload.HeadlineKpis.Single(k => k.Code == "STD-01").Value;
        var eastStd01 = eastPayload.HeadlineKpis.Single(k => k.Code == "STD-01").Value;
        var elcStd01 = elcPayload.HeadlineKpis.Single(k => k.Code == "STD-01").Value;

        allStd01.Should().Be(45m, "48 seeded students minus 2 exited and 1 on leave");
        mainStd01.Should().Be(37m);
        eastStd01.Should().Be(8m);
        (mainStd01 + eastStd01).Should().Be(allStd01);

        elcStd01.Should().BeGreaterThan(0m);
        elcStd01.Should().BeLessThan(allStd01);

        // Operations & Teaching metrics remain populated for East Campus
        eastPayload.HeadlineKpis.Single(k => k.Code == "OPS-01").Value.Should().BeGreaterThan(0m);
        eastPayload.HeadlineKpis.Single(k => k.Code == "TCH-01").Value.Should().BeGreaterThanOrEqualTo(0m);
    }

    [SkippableFact]
    public async Task GetExecutiveDashboard_Should_Decorate_IsAccessible_And_FilterQueue_BasedOnCallerPermissions()
    {
        SkipIfDatabaseUnavailable();

        var client = Factory.CreateClient();
        await SeedDemoDataAsync(client);

        // Configure a Finance-only caller lacking permissions/roles for Students, Academics, Teaching, and Operations (DEC-006)
        Factory.CurrentUser.SetUser(
            id: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            username: "finance-officer",
            roles: ["FinanceOfficer"],
            permissions: ["Permissions.Finance.Obligations.View"]);

        var response = await client.GetAsync("/api/dashboard/executive");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ExecutiveDashboardResponse>();
        payload.Should().NotBeNull();

        var summariesByKey = payload!.DomainSummaries.ToDictionary(d => d.DomainKey, StringComparer.OrdinalIgnoreCase);
        summariesByKey["finance"].IsAccessible.Should().BeTrue();
        summariesByKey["development"].IsAccessible.Should().BeTrue();

        summariesByKey["students"].IsAccessible.Should().BeFalse();
        summariesByKey["academics"].IsAccessible.Should().BeFalse();
        summariesByKey["teaching"].IsAccessible.Should().BeFalse();
        summariesByKey["operations"].IsAccessible.Should().BeFalse();

        payload.NeedsAttentionQueue.Should().NotBeEmpty();
        payload.NeedsAttentionQueue.Should().OnlyContain(
            s => string.Equals(s.Domain, "Finance", StringComparison.OrdinalIgnoreCase),
            "attention queue should exclude signals from domains the caller cannot access");
    }

    [SkippableFact]
    public async Task GetExecutiveDashboard_Should_Return_Unauthorized_When_Unauthenticated()
    {
        SkipIfDatabaseUnavailable();

        var client = Factory.CreateClient();
        Factory.CurrentUser.SetUnauthenticated();

        var response = await client.GetAsync("/api/dashboard/executive");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
