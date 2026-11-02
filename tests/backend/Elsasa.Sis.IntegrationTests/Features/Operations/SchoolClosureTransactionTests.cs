namespace Elsasa.Sis.IntegrationTests.Features.Operations;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.IntegrationTests.Fixtures;
using Elsasa.Sis.Operations.Features.CalendarPeriods.CreateAcademicYear;
using Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.CreateSchoolCalendar;
using Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolClosures;
using Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolClosures.DeclareSchoolClosure;
using FluentAssertions;
using Xunit;

/// <summary>
/// End-to-end integration tests for School Closures, multi-table transactions, and amendment auditing (ADR-004, ADR-005, TD-05, UC-OPS-12).
/// Verifies atomic declaration of closures: closure record, calendar entry, version bumping, and amendment records.
/// </summary>
public sealed class SchoolClosureTransactionTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _academicYearsUri = "/api/operations/academic-years";
    private const string _calendarsUri = "/api/operations/calendars";
    private const string _closuresUri = "/api/operations/closures";

    [SkippableFact]
    public async Task DeclareSchoolClosure_Should_AtomicallyCreateClosureRecord_CalendarEntry_VersionIncrement_AndAmendment()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        var ayRequest = new CreateAcademicYearRequest(
            Code: "AY-CLOSURE-2026-2027",
            Name: "AY for School Closure Tests",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 6, 30));

        var ayResponse = await Client.PostAsJsonAsync(_academicYearsUri, ayRequest);
        ayResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var ay = await ayResponse.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();
        ay.Should().NotBeNull();

        // Create and publish school calendar (starts at version 1.0)
        var calRequest = new CreateSchoolCalendarRequest(Name: "Closure Test Calendar 2026/2027");
        var calResponse = await Client.PostAsJsonAsync($"{_academicYearsUri}/{ay!.Id}/calendars", calRequest);
        var cal = await calResponse.Content.ReadFromJsonAsync<CreateSchoolCalendarResponse>();
        cal.Should().NotBeNull();

        var publishResponse = await Client.PostAsync($"{_calendarsUri}/{cal!.Id}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act: Declare emergency school closure
        var closureRequest = new DeclareSchoolClosureRequest(
            AcademicYearId: ay.Id,
            CalendarId: cal.Id,
            ReasonCategory: "weather",
            ReasonDetails: "Severe monsoon flooding in East Campus",
            StartDate: new DateOnly(2026, 11, 10),
            EndDate: new DateOnly(2026, 11, 12),
            AffectedCampus: "Main Campus",
            AffectedPopulation: "All",
            AuthorizedBy: "Board of Directors",
            RecoveryPlanNotes: "Asynchronous remote learning during closure");

        var response = await Client.PostAsJsonAsync(_closuresUri, closureRequest);

        // Assert HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var closure = await response.Content.ReadFromJsonAsync<SchoolClosureResponse>();
        closure.Should().NotBeNull();
        closure!.ClosureNumber.Should().StartWith("CLS-");
        closure.Status.Should().Be("declared");
        closure.ReasonCategory.Should().Be("weather");
        closure.StartDate.Should().Be(new DateOnly(2026, 11, 10));
        closure.EndDate.Should().Be(new DateOnly(2026, 11, 12));

        // Assert 1: Database operations.school_closures row
        const string selectClosureSql = """
            SELECT
                id AS Id,
                closure_number AS ClosureNumber,
                status AS Status,
                reason_category AS ReasonCategory,
                start_date AS StartDate,
                end_date AS EndDate
            FROM operations.school_closures
            WHERE id = @Id;
            """;

        var dbClosure = await QuerySingleOrDefaultAsync<ClosureDbRow>(selectClosureSql, new { Id = closure.Id });
        dbClosure.Should().NotBeNull();
        dbClosure!.Status.Should().Be("declared");
        dbClosure.ClosureNumber.Should().Be(closure.ClosureNumber);

        // Assert 2: Database operations.calendar_entries linked row
        const string selectEntrySql = """
            SELECT
                id AS Id,
                calendar_id AS CalendarId,
                closure_id AS ClosureId,
                designation AS Designation,
                is_instructional AS IsInstructional
            FROM operations.calendar_entries
            WHERE closure_id = @ClosureId;
            """;

        var dbEntry = await QuerySingleOrDefaultAsync<CalendarEntryDbRow>(selectEntrySql, new { ClosureId = closure.Id });
        dbEntry.Should().NotBeNull();
        dbEntry!.CalendarId.Should().Be(cal.Id);
        dbEntry.Designation.Should().Be("school_closure");
        dbEntry.IsInstructional.Should().BeFalse();

        // Assert 3: Database operations.school_calendars version incremented to 1.1
        const string selectVersionSql = "SELECT version FROM operations.school_calendars WHERE id = @Id;";
        var updatedVersion = await ExecuteScalarAsync<string>(selectVersionSql, new { Id = cal.Id });
        updatedVersion.Should().Be("1.1");

        // Assert 4: Database operations.calendar_amendments audit record
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

        var dbAmendment = await QuerySingleOrDefaultAsync<ClosureAmendmentDbRow>(selectAmendmentSql, new { CalendarId = cal.Id });
        dbAmendment.Should().NotBeNull();
        dbAmendment!.AmendmentType.Should().Be("closure_addition");
        dbAmendment.VersionPrior.Should().Be("1.0");
        dbAmendment.VersionResulting.Should().Be("1.1");
        dbAmendment.JustificationReason.Should().Contain("monsoon flooding");
    }

    [SkippableFact]
    public async Task LiftSchoolClosure_Should_TransitionStatusToLifted()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        var ayRequest = new CreateAcademicYearRequest(
            Code: "AY-LIFT-2026-2027",
            Name: "AY for Lift Closure Tests",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 6, 30));

        var ayResponse = await Client.PostAsJsonAsync(_academicYearsUri, ayRequest);
        var ay = await ayResponse.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();

        var calRequest = new CreateSchoolCalendarRequest(Name: "Lift Test Calendar 2026/2027");
        var calResponse = await Client.PostAsJsonAsync($"{_academicYearsUri}/{ay!.Id}/calendars", calRequest);
        var cal = await calResponse.Content.ReadFromJsonAsync<CreateSchoolCalendarResponse>();
        await Client.PostAsync($"{_calendarsUri}/{cal!.Id}/publish", null);

        var closureRequest = new DeclareSchoolClosureRequest(
            AcademicYearId: ay.Id,
            CalendarId: cal.Id,
            ReasonCategory: "facility",
            ReasonDetails: "Water pipe maintenance",
            StartDate: new DateOnly(2026, 12, 1),
            EndDate: new DateOnly(2026, 12, 2),
            AffectedCampus: "Main Campus",
            AffectedPopulation: "All",
            AuthorizedBy: "Facilities Manager",
            RecoveryPlanNotes: "Facility repaired overnight");

        var declareResponse = await Client.PostAsJsonAsync(_closuresUri, closureRequest);
        declareResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var closure = await declareResponse.Content.ReadFromJsonAsync<SchoolClosureResponse>();
        closure.Should().NotBeNull();

        // Act: Lift the school closure
        var liftResponse = await Client.PostAsync($"{_closuresUri}/{closure!.Id}/lift", null);

        // Assert
        liftResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var liftedClosure = await liftResponse.Content.ReadFromJsonAsync<SchoolClosureResponse>();
        liftedClosure.Should().NotBeNull();
        liftedClosure!.Status.Should().Be("lifted");

        // Verify database row
        const string selectStatusSql = "SELECT status FROM operations.school_closures WHERE id = @Id;";
        var statusInDb = await ExecuteScalarAsync<string>(selectStatusSql, new { Id = closure.Id });
        statusInDb.Should().Be("lifted");
    }

    private sealed record ClosureDbRow(
        Guid Id,
        string ClosureNumber,
        string Status,
        string ReasonCategory,
        DateOnly StartDate,
        DateOnly EndDate);

    private sealed record CalendarEntryDbRow(
        Guid Id,
        Guid CalendarId,
        Guid? ClosureId,
        string Designation,
        bool IsInstructional);

    private sealed record ClosureAmendmentDbRow(
        Guid CalendarId,
        string AmendmentType,
        string VersionPrior,
        string VersionResulting,
        string JustificationReason);
}
