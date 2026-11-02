namespace Elsasa.Sis.IntegrationTests.Features.Operations;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.Contracts.Operations;
using Elsasa.Sis.IntegrationTests.Fixtures;
using Elsasa.Sis.Operations.Features.CalendarPeriods.CalendarEntries.CreateCalendarEntry;
using Elsasa.Sis.Operations.Features.CalendarPeriods.CreateAcademicYear;
using Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.CreateOperationalPeriod;
using Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.CreateSchoolCalendar;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// End-to-end integration tests for <see cref="IOperationsCalendarQueryService"/> (ADR-001, ADR-003, TD-03).
/// Verifies cross-domain queries executed by Students and Finance bounded contexts against real SQL Server data.
/// </summary>
public sealed class OperationsCalendarQueryServiceTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _academicYearsUri = "/api/operations/academic-years";
    private const string _calendarsUri = "/api/operations/calendars";

    private IOperationsCalendarQueryService CreateQueryService()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IOperationsCalendarQueryService>();
    }

    [SkippableFact]
    public async Task GetActiveAcademicYearAsync_Should_ReturnActiveYear_When_Exists()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        var queryService = CreateQueryService();

        // When no year is active initially
        var initialResult = await queryService.GetActiveAcademicYearAsync();
        initialResult.IsSuccess.Should().BeTrue();
        initialResult.Value.Should().BeNull();

        // Create, approve, and activate an academic year
        var ayRequest = new CreateAcademicYearRequest(
            Code: "AY-QUERY-ACTIVE",
            Name: "Queryable Active Academic Year",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 6, 30));

        var ayResponse = await Client.PostAsJsonAsync(_academicYearsUri, ayRequest);
        var ay = await ayResponse.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();

        await Client.PostAsync($"{_academicYearsUri}/{ay!.Id}/approve", null);
        await Client.PostAsync($"{_academicYearsUri}/{ay.Id}/activate", null);

        // Act
        var result = await queryService.GetActiveAcademicYearAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(ay.Id);
        result.Value.Code.Should().Be(ayRequest.Code);
        result.Value.Name.Should().Be(ayRequest.Name);
        result.Value.Status.Should().Be("active");
        result.Value.StartDate.Should().Be(ayRequest.StartDate);
        result.Value.EndDate.Should().Be(ayRequest.EndDate);
    }

    [SkippableFact]
    public async Task GetOperationalPeriodByIdAsync_Should_ReturnPeriod_When_Exists()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        var queryService = CreateQueryService();

        var ayRequest = new CreateAcademicYearRequest(
            Code: "AY-QUERY-PERIODS",
            Name: "AY for Period Query",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 6, 30));

        var ayResponse = await Client.PostAsJsonAsync(_academicYearsUri, ayRequest);
        var ay = await ayResponse.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();

        var semRequest = new CreateOperationalPeriodRequest(
            PeriodCode: "SEM-QUERY-1",
            Name: "Query Semester 1",
            PeriodType: "semester",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 1, 31));

        var semResponse = await Client.PostAsJsonAsync($"{_academicYearsUri}/{ay!.Id}/periods", semRequest);
        var sem = await semResponse.Content.ReadFromJsonAsync<CreateOperationalPeriodResponse>();

        // Act 1: Existing period ID
        var result = await queryService.GetOperationalPeriodByIdAsync(sem!.Id);

        // Assert 1
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(sem.Id);
        result.Value.AcademicYearId.Should().Be(ay.Id);
        result.Value.PeriodCode.Should().Be("SEM-QUERY-1");
        result.Value.PeriodType.Should().Be("semester");
        result.Value.Status.Should().Be("active");

        // Act 2: Non-existent period ID
        var nonExistentResult = await queryService.GetOperationalPeriodByIdAsync(Guid.NewGuid());

        // Assert 2
        nonExistentResult.IsSuccess.Should().BeTrue();
        nonExistentResult.Value.Should().BeNull();
    }

    [SkippableFact]
    public async Task GetActiveAdmissionWindowsAsync_Should_ReturnWindows_When_DateWithinRange()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        var queryService = CreateQueryService();

        var ayRequest = new CreateAcademicYearRequest(
            Code: "AY-QUERY-ADMISSION",
            Name: "AY for Admission Window Query",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 6, 30));

        var ayResponse = await Client.PostAsJsonAsync(_academicYearsUri, ayRequest);
        var ay = await ayResponse.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();

        // Create admission window period (2026-10-01 to 2026-11-30)
        var winRequest = new CreateOperationalPeriodRequest(
            PeriodCode: "ADM-WINDOW-OPEN",
            Name: "Open Admission Window",
            PeriodType: "admission_window",
            StartDate: new DateOnly(2026, 10, 1),
            EndDate: new DateOnly(2026, 11, 30));

        var winResponse = await Client.PostAsJsonAsync($"{_academicYearsUri}/{ay!.Id}/periods", winRequest);
        winResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var win = await winResponse.Content.ReadFromJsonAsync<CreateOperationalPeriodResponse>();

        // Act 1: Date inside admission window range (2026-10-15)
        var activeResult = await queryService.GetActiveAdmissionWindowsAsync(new DateOnly(2026, 10, 15));

        // Assert 1
        activeResult.IsSuccess.Should().BeTrue();
        activeResult.Value.Should().NotBeNull();
        activeResult.Value.Should().Contain(w => w.Id == win!.Id && w.PeriodCode == "ADM-WINDOW-OPEN");

        // Act 2: Date outside admission window range (2026-12-05)
        var outsideResult = await queryService.GetActiveAdmissionWindowsAsync(new DateOnly(2026, 12, 5));

        // Assert 2
        outsideResult.IsSuccess.Should().BeTrue();
        outsideResult.Value.Should().NotBeNull();
        outsideResult.Value.Should().NotContain(w => w.Id == win!.Id);
    }

    [SkippableFact]
    public async Task IsInstructionalDayAsync_Should_ReturnExpectedStatus_BasedOnCalendarAndDayOfWeek()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        var queryService = CreateQueryService();

        var ayRequest = new CreateAcademicYearRequest(
            Code: "AY-QUERY-INSTRUCTIONAL",
            Name: "AY for Instructional Day Query",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 6, 30));

        var ayResponse = await Client.PostAsJsonAsync(_academicYearsUri, ayRequest);
        var ay = await ayResponse.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();

        // Create and publish calendar
        var calRequest = new CreateSchoolCalendarRequest(Name: "Instructional Calendar 2026/2027");
        var calResponse = await Client.PostAsJsonAsync($"{_academicYearsUri}/{ay!.Id}/calendars", calRequest);
        var cal = await calResponse.Content.ReadFromJsonAsync<CreateSchoolCalendarResponse>();

        // Add Christmas holiday entry on Friday 2026-12-25
        var holidayRequest = new CreateCalendarEntryRequest(
            EntryDate: new DateOnly(2026, 12, 25),
            EndDate: null,
            Designation: "holiday",
            Name: "Christmas Day",
            IsInstructional: false);

        await Client.PostAsJsonAsync($"{_calendarsUri}/{cal!.Id}/entries", holidayRequest);
        await Client.PostAsync($"{_calendarsUri}/{cal.Id}/publish", null);

        // Act 1: Friday 2026-12-25 has explicit holiday -> false
        var holidayResult = await queryService.IsInstructionalDayAsync(new DateOnly(2026, 12, 25));

        // Act 2: Regular Monday 2026-10-05 with no exceptions -> true
        var regularMondayResult = await queryService.IsInstructionalDayAsync(new DateOnly(2026, 10, 5));

        // Assert
        holidayResult.IsSuccess.Should().BeTrue();
        holidayResult.Value.Should().BeFalse();

        regularMondayResult.IsSuccess.Should().BeTrue();
        regularMondayResult.Value.Should().BeTrue();
    }
}
