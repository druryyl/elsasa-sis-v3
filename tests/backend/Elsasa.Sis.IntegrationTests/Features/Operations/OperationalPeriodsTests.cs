namespace Elsasa.Sis.IntegrationTests.Features.Operations;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.IntegrationTests.Fixtures;
using Elsasa.Sis.Operations.Features.CalendarPeriods.CreateAcademicYear;
using Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.CreateOperationalPeriod;
using Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.GetOperationalPeriodsByAcademicYear;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

/// <summary>
/// End-to-end integration tests for Operational Periods and hierarchy management (ADR-004, ADR-005, UC-OPS-05..08).
/// Verifies creation of primary semesters, nested admission windows, tree projection, and date containment rules.
/// </summary>
public sealed class OperationalPeriodsTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _academicYearsUri = "/api/operations/academic-years";

    [SkippableFact]
    public async Task CreateOperationalPeriod_Should_ReturnCreated_When_ParentSemesterAndNestedAdmissionWindowAreValid()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        var ayRequest = new CreateAcademicYearRequest(
            Code: "AY-PERIODS-VALID",
            Name: "Academic Year for Operational Periods",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 6, 30));

        var ayResponse = await Client.PostAsJsonAsync(_academicYearsUri, ayRequest);
        ayResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var ay = await ayResponse.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();
        ay.Should().NotBeNull();

        // 1. Create primary semester
        var semRequest = new CreateOperationalPeriodRequest(
            PeriodCode: "SEM-1",
            Name: "Semester 1",
            PeriodType: "semester",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 1, 31),
            CampusContext: "All",
            Notes: "Primary first semester");

        var semResponse = await Client.PostAsJsonAsync($"{_academicYearsUri}/{ay!.Id}/periods", semRequest);
        semResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var sem = await semResponse.Content.ReadFromJsonAsync<CreateOperationalPeriodResponse>();
        sem.Should().NotBeNull();
        sem!.PeriodCode.Should().Be("SEM-1");
        sem.PeriodType.Should().Be("semester");
        sem.ParentPeriodId.Should().BeNull();
        sem.Status.Should().Be("active");

        // 2. Create nested admission window within semester 1 boundaries
        var admissionRequest = new CreateOperationalPeriodRequest(
            PeriodCode: "ADM-WIN-1",
            Name: "Early Admission Window",
            PeriodType: "admission_window",
            StartDate: new DateOnly(2026, 10, 1),
            EndDate: new DateOnly(2026, 11, 30),
            ParentPeriodId: sem.Id,
            CampusContext: "All",
            Notes: "Early enrollment period");

        var admissionResponse = await Client.PostAsJsonAsync($"{_academicYearsUri}/{ay.Id}/periods", admissionRequest);
        admissionResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var admission = await admissionResponse.Content.ReadFromJsonAsync<CreateOperationalPeriodResponse>();
        admission.Should().NotBeNull();
        admission!.PeriodCode.Should().Be("ADM-WIN-1");
        admission.ParentPeriodId.Should().Be(sem.Id);

        // 3. Query operational periods tree for the academic year
        var treeResponse = await Client.GetAsync($"{_academicYearsUri}/{ay.Id}/periods");
        treeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await treeResponse.Content.ReadFromJsonAsync<GetOperationalPeriodsByAcademicYearResponse>();
        result.Should().NotBeNull();
        result!.Periods.Should().HaveCount(2);
        result.Tree.Should().HaveCount(1);

        var rootNode = result.Tree[0];
        rootNode.Id.Should().Be(sem.Id);
        rootNode.PeriodCode.Should().Be("SEM-1");
        rootNode.Children.Should().HaveCount(1);
        rootNode.Children[0].Id.Should().Be(admission.Id);
        rootNode.Children[0].PeriodCode.Should().Be("ADM-WIN-1");
    }

    [SkippableFact]
    public async Task CreateOperationalPeriod_Should_ReturnValidation_When_DatesOutOfBoundsOfAcademicYear()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        var ayRequest = new CreateAcademicYearRequest(
            Code: "AY-PERIODS-OOB",
            Name: "AY Boundary Rejection Test",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 6, 30));

        var ayResponse = await Client.PostAsJsonAsync(_academicYearsUri, ayRequest);
        var ay = await ayResponse.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();
        ay.Should().NotBeNull();

        // Act: StartDate is prior to AY start date (2026-08-15 < 2026-09-01)
        var invalidRequest = new CreateOperationalPeriodRequest(
            PeriodCode: "SEM-OOB",
            Name: "Out of Bounds Semester",
            PeriodType: "semester",
            StartDate: new DateOnly(2026, 8, 15),
            EndDate: new DateOnly(2027, 1, 31));

        var response = await Client.PostAsJsonAsync($"{_academicYearsUri}/{ay!.Id}/periods", invalidRequest);

        // Assert: HTTP 422 with Operations.PeriodDateRangeOutOfBounds
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Extensions.Should().ContainKey("errorCode");
        problem.Extensions["errorCode"]!.ToString().Should().Be("Operations.PeriodDateRangeOutOfBounds");
    }

    [SkippableFact]
    public async Task CreateOperationalPeriod_Should_ReturnValidation_When_ChildDatesOutOfBoundsOfParentPeriod()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        var ayRequest = new CreateAcademicYearRequest(
            Code: "AY-CHILD-OOB",
            Name: "AY Child Boundary Test",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 6, 30));

        var ayResponse = await Client.PostAsJsonAsync(_academicYearsUri, ayRequest);
        var ay = await ayResponse.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();
        ay.Should().NotBeNull();

        // Create parent semester (ends 2027-01-31)
        var semRequest = new CreateOperationalPeriodRequest(
            PeriodCode: "SEM-PARENT",
            Name: "Parent Semester",
            PeriodType: "semester",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 1, 31));

        var semResponse = await Client.PostAsJsonAsync($"{_academicYearsUri}/{ay!.Id}/periods", semRequest);
        var sem = await semResponse.Content.ReadFromJsonAsync<CreateOperationalPeriodResponse>();
        sem.Should().NotBeNull();

        // Act: Child admission window ends 2027-02-15 which exceeds parent semester end 2027-01-31
        var invalidChildRequest = new CreateOperationalPeriodRequest(
            PeriodCode: "ADM-EXCEED",
            Name: "Exceeding Admission Window",
            PeriodType: "admission_window",
            StartDate: new DateOnly(2027, 1, 1),
            EndDate: new DateOnly(2027, 2, 15),
            ParentPeriodId: sem!.Id);

        var response = await Client.PostAsJsonAsync($"{_academicYearsUri}/{ay.Id}/periods", invalidChildRequest);

        // Assert: HTTP 422 with Operations.ChildPeriodDateRangeOutOfBounds
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Extensions.Should().ContainKey("errorCode");
        problem.Extensions["errorCode"]!.ToString().Should().Be("Operations.ChildPeriodDateRangeOutOfBounds");
    }
}
