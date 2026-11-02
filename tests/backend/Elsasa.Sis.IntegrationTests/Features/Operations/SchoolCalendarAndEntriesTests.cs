namespace Elsasa.Sis.IntegrationTests.Features.Operations;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.IntegrationTests.Fixtures;
using Elsasa.Sis.Operations.Features.CalendarPeriods.CalendarEntries.CreateCalendarEntry;
using Elsasa.Sis.Operations.Features.CalendarPeriods.CalendarEntries.GetCalendarMatrixProjection;
using Elsasa.Sis.Operations.Features.CalendarPeriods.CreateAcademicYear;
using Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.CreateSchoolCalendar;
using Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.PublishSchoolCalendar;
using FluentAssertions;
using Xunit;

/// <summary>
/// End-to-end integration tests for School Calendars, entries, publication lifecycle, and matrix projection (ADR-004, ADR-005, UC-OPS-09..11).
/// </summary>
public sealed class SchoolCalendarAndEntriesTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _academicYearsUri = "/api/operations/academic-years";
    private const string _calendarsUri = "/api/operations/calendars";

    [SkippableFact]
    public async Task SchoolCalendar_Should_Create_AddHolidayEntry_AndPublish()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        var ayRequest = new CreateAcademicYearRequest(
            Code: "AY-CAL-2026-2027",
            Name: "Academic Year for Calendar Tests",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 6, 30));

        var ayResponse = await Client.PostAsJsonAsync(_academicYearsUri, ayRequest);
        ayResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var ay = await ayResponse.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();
        ay.Should().NotBeNull();

        // 1. Create School Calendar in draft
        var calRequest = new CreateSchoolCalendarRequest(
            Name: "Master School Calendar 2026/2027",
            ScopeType: "institution",
            ScopeReference: "All",
            Notes: "Initial draft calendar");

        var calResponse = await Client.PostAsJsonAsync($"{_academicYearsUri}/{ay!.Id}/calendars", calRequest);
        calResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var calendar = await calResponse.Content.ReadFromJsonAsync<CreateSchoolCalendarResponse>();
        calendar.Should().NotBeNull();
        calendar!.Name.Should().Be("Master School Calendar 2026/2027");
        calendar.PublicationStatus.Should().Be("draft");
        calendar.Version.Should().Be("1.0");

        // 2. Add Holiday Calendar Entry (Christmas 2026)
        var holidayRequest = new CreateCalendarEntryRequest(
            EntryDate: new DateOnly(2026, 12, 25),
            EndDate: null,
            Designation: "holiday",
            Name: "Christmas Day",
            Description: "National public holiday",
            IsInstructional: false);

        var entryResponse = await Client.PostAsJsonAsync($"{_calendarsUri}/{calendar.Id}/entries", holidayRequest);
        entryResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var entry = await entryResponse.Content.ReadFromJsonAsync<CreateCalendarEntryResponse>();
        entry.Should().NotBeNull();
        entry!.Designation.Should().Be("holiday");
        entry.IsInstructional.Should().BeFalse();

        // 3. Publish School Calendar
        var publishResponse = await Client.PostAsync($"{_calendarsUri}/{calendar.Id}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var published = await publishResponse.Content.ReadFromJsonAsync<PublishSchoolCalendarResponse>();
        published.Should().NotBeNull();
        published!.PublicationStatus.Should().Be("published");
        published.PublishedBy.Should().NotBeNullOrWhiteSpace();

        // 4. Query Matrix Projection across Christmas week
        var startDate = new DateOnly(2026, 12, 24);
        var endDate = new DateOnly(2026, 12, 26);
        var matrixResponse = await Client.GetAsync(
            $"{_calendarsUri}/{calendar.Id}/matrix?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        matrixResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var matrix = await matrixResponse.Content.ReadFromJsonAsync<CalendarMatrixProjectionResponse>();
        matrix.Should().NotBeNull();
        matrix!.CalendarId.Should().Be(calendar.Id);
        matrix.PublicationStatus.Should().Be("published");
        matrix.Days.Should().HaveCount(3);

        var christmasDay = matrix.Days.Single(d => d.Date == new DateOnly(2026, 12, 25));
        christmasDay.Designation.Should().Be("holiday");
        christmasDay.IsInstructional.Should().BeFalse();
        christmasDay.EntryName.Should().Be("Christmas Day");
    }

    [SkippableFact]
    public async Task CreateCalendarEntry_OnPublishedCalendar_Should_IncrementMinorVersionAndRecordAmendment()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        var ayRequest = new CreateAcademicYearRequest(
            Code: "AY-AMEND-2026-2027",
            Name: "AY for Calendar Amendments",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 6, 30));

        var ayResponse = await Client.PostAsJsonAsync(_academicYearsUri, ayRequest);
        var ay = await ayResponse.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();

        // Create and publish calendar (starts at version 1.0)
        var calRequest = new CreateSchoolCalendarRequest(Name: "Amendable Calendar 2026/2027");
        var calResponse = await Client.PostAsJsonAsync($"{_academicYearsUri}/{ay!.Id}/calendars", calRequest);
        var calendar = await calResponse.Content.ReadFromJsonAsync<CreateSchoolCalendarResponse>();

        await Client.PostAsync($"{_calendarsUri}/{calendar!.Id}/publish", null);

        // Act: Add entry to already published calendar
        var newHoliday = new CreateCalendarEntryRequest(
            EntryDate: new DateOnly(2027, 1, 1),
            EndDate: null,
            Designation: "holiday",
            Name: "New Year's Day",
            IsInstructional: false,
            AmendmentReason: "Scheduled public holiday addition");

        var addResponse = await Client.PostAsJsonAsync($"{_calendarsUri}/{calendar.Id}/entries", newHoliday);
        addResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Assert 1: Calendar version incremented to 1.1 in database
        const string selectCalSql = "SELECT version FROM operations.school_calendars WHERE id = @Id;";
        var updatedVersion = await ExecuteScalarAsync<string>(selectCalSql, new { Id = calendar.Id });
        updatedVersion.Should().Be("1.1");

        // Assert 2: Amendment audit log created in operations.calendar_amendments
        const string selectAmendmentSql = """
            SELECT
                calendar_id AS CalendarId,
                amendment_type AS AmendmentType,
                version_prior AS VersionPrior,
                version_resulting AS VersionResulting,
                justification_reason AS JustificationReason
            FROM operations.calendar_amendments
            WHERE calendar_id = @CalendarId;
            """;

        var amendment = await QuerySingleOrDefaultAsync<AmendmentDbRow>(selectAmendmentSql, new { CalendarId = calendar.Id });
        amendment.Should().NotBeNull();
        amendment!.AmendmentType.Should().Be("entry_addition");
        amendment.VersionPrior.Should().Be("1.0");
        amendment.VersionResulting.Should().Be("1.1");
        amendment.JustificationReason.Should().Be("Scheduled public holiday addition");
    }

    private sealed record AmendmentDbRow(
        Guid CalendarId,
        string AmendmentType,
        string VersionPrior,
        string VersionResulting,
        string JustificationReason);
}
