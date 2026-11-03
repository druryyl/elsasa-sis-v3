namespace Elsasa.Sis.IntegrationTests.Features.Operations;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.IntegrationTests.Fixtures;
using Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.ActivateAcademicYear;
using Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.ApproveAcademicYear;
using Elsasa.Sis.Operations.Features.CalendarPeriods.CreateAcademicYear;
using Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.CreateOperationalPeriod;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

/// <summary>
/// End-to-end integration tests for the Academic Year lifecycle transitions (ADR-004, ADR-005, UC-OPS-01..04).
/// Tests transitions from draft -> approved -> active, and asserts the single active academic year constraint.
/// </summary>
public sealed class AcademicYearLifecycleTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _academicYearsUri = "/api/operations/academic-years";

    [SkippableFact]
    public async Task AcademicYear_Should_TransitionThrough_DraftApprovedActiveLifecycle()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        var createRequest = new CreateAcademicYearRequest(
            Code: "AY-LC-2026-2027",
            Name: "Lifecycle Test AY 2026/2027",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 6, 30),
            Notes: "Testing full lifecycle transitions.");

        // 1. Create -> Draft
        var createResponse = await Client.PostAsJsonAsync(_academicYearsUri, createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();
        created.Should().NotBeNull();
        created!.Status.Should().Be("draft");

        // 2. Approve -> Approved
        var approveResponse = await Client.PostAsync($"{_academicYearsUri}/{created.Id}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var approved = await approveResponse.Content.ReadFromJsonAsync<ApproveAcademicYearResponse>();
        approved.Should().NotBeNull();
        approved!.Id.Should().Be(created.Id);
        approved.Status.Should().Be("approved");
        approved.ApprovedBy.Should().NotBeNullOrWhiteSpace();
        approved.ApprovedDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));

        // 3. Activate -> Active
        var activateResponse = await Client.PostAsync($"{_academicYearsUri}/{created.Id}/activate", null);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var activated = await activateResponse.Content.ReadFromJsonAsync<ActivateAcademicYearResponse>();
        activated.Should().NotBeNull();
        activated!.Id.Should().Be(created.Id);
        activated.Status.Should().Be("active");

        // 4. Verify in SQL Server database via Dapper
        const string selectSql = """
            SELECT
                id AS Id,
                code AS Code,
                status AS Status,
                approved_by AS ApprovedBy,
                approved_date AS ApprovedDate
            FROM operations.academic_years
            WHERE id = @Id;
            """;

        var dbRow = await QuerySingleOrDefaultAsync<LifecycleDbRow>(selectSql, new { Id = created.Id });
        dbRow.Should().NotBeNull();
        dbRow!.Status.Should().Be("active");
        dbRow.ApprovedBy.Should().NotBeNullOrWhiteSpace();
        dbRow.ApprovedDate.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task ActivatingAcademicYear_Should_MarkPriorActiveYearAsCompleted()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        // 1. Create, approve, and activate AY 1
        var ay1Request = new CreateAcademicYearRequest(
            Code: "AY-ACTIVE-1",
            Name: "First Active Year",
            StartDate: new DateOnly(2025, 9, 1),
            EndDate: new DateOnly(2026, 6, 30));

        var createAy1Response = await Client.PostAsJsonAsync(_academicYearsUri, ay1Request);
        createAy1Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var ay1 = await createAy1Response.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();
        ay1.Should().NotBeNull();

        await Client.PostAsync($"{_academicYearsUri}/{ay1!.Id}/approve", null);
        var activateAy1Response = await Client.PostAsync($"{_academicYearsUri}/{ay1.Id}/activate", null);
        activateAy1Response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Create and approve AY 2
        var ay2Request = new CreateAcademicYearRequest(
            Code: "AY-ACTIVE-2",
            Name: "Second Active Year",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 6, 30));

        var createAy2Response = await Client.PostAsJsonAsync(_academicYearsUri, ay2Request);
        createAy2Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var ay2 = await createAy2Response.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();
        ay2.Should().NotBeNull();

        await Client.PostAsync($"{_academicYearsUri}/{ay2!.Id}/approve", null);

        // Act: Activate AY 2
        var activateAy2Response = await Client.PostAsync($"{_academicYearsUri}/{ay2.Id}/activate", null);
        activateAy2Response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert: Database state must reflect single active year (AY 1 is 'completed', AY 2 is 'active')
        const string selectStatusSql = """
            SELECT status
            FROM operations.academic_years
            WHERE id = @Id;
            """;

        var ay1Status = await ExecuteScalarAsync<string>(selectStatusSql, new { Id = ay1.Id });
        var ay2Status = await ExecuteScalarAsync<string>(selectStatusSql, new { Id = ay2.Id });

        ay1Status.Should().Be("completed");
        ay2Status.Should().Be("active");

        // Verify only 1 academic year in the database has status = 'active'
        const string countActiveSql = "SELECT COUNT(1) FROM operations.academic_years WHERE status = 'active';";
        var activeCount = await ExecuteScalarAsync<int>(countActiveSql);
        activeCount.Should().Be(1);
    }

    [SkippableFact]
    public async Task ActivatingAcademicYear_Should_ReturnUnprocessableEntity_When_NotApproved()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        var request = new CreateAcademicYearRequest(
            Code: "AY-DRAFT-DIRECT",
            Name: "Draft Direct Activation Test",
            StartDate: new DateOnly(2028, 9, 1),
            EndDate: new DateOnly(2029, 6, 30));

        var createResponse = await Client.PostAsJsonAsync(_academicYearsUri, request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();
        created.Should().NotBeNull();

        // Act: Attempt to activate directly while status is still 'draft'
        var activateResponse = await Client.PostAsync($"{_academicYearsUri}/{created!.Id}/activate", null);

        // Assert: HTTP 422 with Operations.InvalidStatusTransition
        activateResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var problem = await activateResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Extensions.Should().ContainKey("errorCode");
        problem.Extensions["errorCode"]!.ToString().Should().Be("Operations.InvalidStatusTransition");
    }

    [SkippableFact]
    public async Task ApprovingAcademicYear_Should_ReturnUnprocessableEntity_When_NotDraft()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        var request = new CreateAcademicYearRequest(
            Code: "AY-DOUBLE-APPROVE",
            Name: "Double Approve Test",
            StartDate: new DateOnly(2029, 9, 1),
            EndDate: new DateOnly(2030, 6, 30));

        var createResponse = await Client.PostAsJsonAsync(_academicYearsUri, request);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();
        created.Should().NotBeNull();

        // Approve once -> approved
        var firstApprove = await Client.PostAsync($"{_academicYearsUri}/{created!.Id}/approve", null);
        firstApprove.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act: Approve again -> should fail because already approved
        var secondApprove = await Client.PostAsync($"{_academicYearsUri}/{created.Id}/approve", null);

        // Assert: HTTP 422
        secondApprove.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await secondApprove.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Extensions.Should().ContainKey("errorCode");
        problem.Extensions["errorCode"]!.ToString().Should().Be("Operations.InvalidStatusTransition");
    }

    [SkippableFact]
    public async Task ActivatingAcademicYear_Should_PromoteChildDraftPeriodsToActive_And_DemoteOutgoingYearActivePeriodsToClosed()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        // 1. Create, approve, and activate AY 1
        var ay1Request = new CreateAcademicYearRequest(
            Code: "AY-CASC-1",
            Name: "First Academic Year For Cascading",
            StartDate: new DateOnly(2025, 9, 1),
            EndDate: new DateOnly(2026, 6, 30));

        var createAy1Response = await Client.PostAsJsonAsync(_academicYearsUri, ay1Request);
        createAy1Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var ay1 = await createAy1Response.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();
        ay1.Should().NotBeNull();

        await Client.PostAsync($"{_academicYearsUri}/{ay1!.Id}/approve", null);
        var activateAy1Response = await Client.PostAsync($"{_academicYearsUri}/{ay1.Id}/activate", null);
        activateAy1Response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Create child operational period for AY 1 and activate it
        var ay1PeriodRequest = new CreateOperationalPeriodRequest(
            PeriodCode: "SEM-AY1-ACTIVE",
            Name: "AY 1 Active Semester",
            PeriodType: "semester",
            StartDate: new DateOnly(2025, 9, 1),
            EndDate: new DateOnly(2026, 1, 31));

        var ay1PeriodResponse = await Client.PostAsJsonAsync($"{_academicYearsUri}/{ay1.Id}/periods", ay1PeriodRequest);
        ay1PeriodResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var ay1Period = await ay1PeriodResponse.Content.ReadFromJsonAsync<CreateOperationalPeriodResponse>();
        ay1Period.Should().NotBeNull();

        var activateAy1PeriodResponse = await Client.PostAsync($"/api/operations/periods/{ay1Period!.Id}/activate", null);
        activateAy1PeriodResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify AY 1 period is active in DB before AY 2 activation
        const string selectPeriodStatusSql = "SELECT status FROM operations.operational_periods WHERE id = @Id;";
        var ay1PeriodStatusBefore = await ExecuteScalarAsync<string>(selectPeriodStatusSql, new { Id = ay1Period.Id });
        ay1PeriodStatusBefore.Should().Be("active");

        // 2. Create AY 2 with child periods in draft status
        var ay2Request = new CreateAcademicYearRequest(
            Code: "AY-CASC-2",
            Name: "Second Academic Year For Cascading",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 6, 30));

        var createAy2Response = await Client.PostAsJsonAsync(_academicYearsUri, ay2Request);
        createAy2Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var ay2 = await createAy2Response.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();
        ay2.Should().NotBeNull();

        var ay2Period1Request = new CreateOperationalPeriodRequest(
            PeriodCode: "SEM-AY2-DRAFT1",
            Name: "AY 2 Semester 1 Draft",
            PeriodType: "semester",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 1, 31));

        var ay2Period1Response = await Client.PostAsJsonAsync($"{_academicYearsUri}/{ay2!.Id}/periods", ay2Period1Request);
        ay2Period1Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var ay2Period1 = await ay2Period1Response.Content.ReadFromJsonAsync<CreateOperationalPeriodResponse>();
        ay2Period1.Should().NotBeNull();
        ay2Period1!.Status.Should().Be("draft");

        var ay2Period2Request = new CreateOperationalPeriodRequest(
            PeriodCode: "SEM-AY2-DRAFT2",
            Name: "AY 2 Semester 2 Draft",
            PeriodType: "semester",
            StartDate: new DateOnly(2027, 2, 1),
            EndDate: new DateOnly(2027, 6, 30));

        var ay2Period2Response = await Client.PostAsJsonAsync($"{_academicYearsUri}/{ay2.Id}/periods", ay2Period2Request);
        ay2Period2Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var ay2Period2 = await ay2Period2Response.Content.ReadFromJsonAsync<CreateOperationalPeriodResponse>();
        ay2Period2.Should().NotBeNull();
        ay2Period2!.Status.Should().Be("draft");

        // Approve AY 2
        var approveAy2Response = await Client.PostAsync($"{_academicYearsUri}/{ay2.Id}/approve", null);
        approveAy2Response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act: Activate AY 2 -> cascades demotion to AY 1 active periods and promotion to AY 2 draft periods
        var activateAy2Response = await Client.PostAsync($"{_academicYearsUri}/{ay2.Id}/activate", null);
        activateAy2Response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert:
        // 1) Active period of outgoing AY 1 is demoted to 'closed'
        var ay1PeriodStatusAfter = await ExecuteScalarAsync<string>(selectPeriodStatusSql, new { Id = ay1Period.Id });
        ay1PeriodStatusAfter.Should().Be("closed");

        // 2) Child draft periods of target AY 2 are promoted to 'active'
        var ay2Period1StatusAfter = await ExecuteScalarAsync<string>(selectPeriodStatusSql, new { Id = ay2Period1.Id });
        var ay2Period2StatusAfter = await ExecuteScalarAsync<string>(selectPeriodStatusSql, new { Id = ay2Period2.Id });
        ay2Period1StatusAfter.Should().Be("active");
        ay2Period2StatusAfter.Should().Be("active");

        // 3) AY 1 is completed and AY 2 is active
        const string selectAyStatusSql = "SELECT status FROM operations.academic_years WHERE id = @Id;";
        var ay1Status = await ExecuteScalarAsync<string>(selectAyStatusSql, new { Id = ay1.Id });
        var ay2Status = await ExecuteScalarAsync<string>(selectAyStatusSql, new { Id = ay2.Id });
        ay1Status.Should().Be("completed");
        ay2Status.Should().Be("active");
    }

    private sealed record LifecycleDbRow(
        Guid Id,
        string Code,
        string Status,
        string? ApprovedBy,
        DateOnly? ApprovedDate);
}
