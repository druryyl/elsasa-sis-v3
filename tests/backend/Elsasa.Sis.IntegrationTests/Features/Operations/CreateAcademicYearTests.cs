namespace Elsasa.Sis.IntegrationTests.Features.Operations;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.IntegrationTests.Fixtures;
using Elsasa.Sis.Operations.Features.CalendarPeriods.CreateAcademicYear;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

/// <summary>
/// End-to-end integration tests for the CreateAcademicYear vertical slice (ADR-005, ADR-008).
/// Verifies HTTP endpoint routing, FluentValidation filter execution, Dapper database persistence,
/// and RFC 7807 problem details error mapping against the real SQL Server container.
/// </summary>
public sealed class CreateAcademicYearTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _academicYearsUri = "/api/operations/academic-years";

    [Fact]
    public async Task CreateAcademicYear_Should_ReturnUnprocessableEntity_When_PayloadIsInvalid()
    {
        // Arrange: invalid payload where Code and Name are empty, and EndDate is before StartDate
        var invalidRequest = new CreateAcademicYearRequest(
            Code: string.Empty,
            Name: string.Empty,
            StartDate: new DateOnly(2027, 6, 30),
            EndDate: new DateOnly(2026, 9, 1));

        // Act
        var response = await Client.PostAsJsonAsync(_academicYearsUri, invalidRequest);

        // Assert: HTTP 422 with RFC 7807 validation problem details structure
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.UnprocessableEntity);
        problem.Title.Should().Be("One or more validation errors occurred.");
        problem.Detail.Should().Be("One or more validation errors occurred.");
        problem.Extensions.Should().ContainKey("errorCode");
        problem.Extensions["errorCode"]!.ToString().Should().Be(Elsasa.Sis.Shared.Endpoints.ValidationFilter<CreateAcademicYearRequest>.DefaultValidationErrorCode);

        problem.Errors.Should().ContainKey("Code");
        problem.Errors.Should().ContainKey("Name");
        problem.Errors.Should().ContainKey("EndDate");
    }

    [SkippableFact]
    public async Task CreateAcademicYear_Should_ReturnCreated_When_PayloadIsValid()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        var request = new CreateAcademicYearRequest(
            Code: "AY-2026-2027",
            Name: "Academic Year 2026/2027",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 6, 30),
            Notes: "Initial draft notes.");

        // Act
        var response = await Client.PostAsJsonAsync(_academicYearsUri, request);

        // Assert: HTTP 201 Created with valid payload and Location header
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.Code.Should().Be(request.Code);
        result.Name.Should().Be(request.Name);
        result.StartDate.Should().Be(request.StartDate);
        result.EndDate.Should().Be(request.EndDate);
        result.Status.Should().Be("draft");
        result.Notes.Should().Be(request.Notes);

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Be($"/api/operations/academic-years/{result.Id}");
    }

    [SkippableFact]
    public async Task CreateAcademicYear_Should_PersistRecordInDatabase_When_SuccessfullyCreated()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        var request = new CreateAcademicYearRequest(
            Code: "AY-2027-2028",
            Name: "Academic Year 2027/2028",
            StartDate: new DateOnly(2027, 9, 1),
            EndDate: new DateOnly(2028, 6, 30),
            Notes: "Created for database persistence verification.");

        // Act
        var response = await Client.PostAsJsonAsync(_academicYearsUri, request);

        // Assert HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();
        created.Should().NotBeNull();

        // Inspect database directly via Dapper
        const string selectSql = """
            SELECT 
                id AS Id,
                code AS Code,
                name AS Name,
                start_date AS StartDate,
                end_date AS EndDate,
                status AS Status,
                notes AS Notes,
                created_at AS CreatedAt,
                created_by AS CreatedBy
            FROM operations.academic_years
            WHERE id = @Id;
            """;

        var persistedRecord = await QuerySingleOrDefaultAsync<AcademicYearDbRow>(
            selectSql,
            new { Id = created!.Id });

        persistedRecord.Should().NotBeNull();
        persistedRecord!.Id.Should().Be(created.Id);
        persistedRecord.Code.Should().Be(request.Code);
        persistedRecord.Name.Should().Be(request.Name);
        persistedRecord.StartDate.Should().Be(request.StartDate);
        persistedRecord.EndDate.Should().Be(request.EndDate);
        persistedRecord.Status.Should().Be("draft");
        persistedRecord.Notes.Should().Be(request.Notes);
        persistedRecord.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        persistedRecord.CreatedBy.Should().Be(Factory.CurrentUser.Username);
    }

    [SkippableFact]
    public async Task CreateAcademicYear_Should_ReturnConflict_When_CodeAlreadyExists()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        var request = new CreateAcademicYearRequest(
            Code: "AY-DUP-TEST",
            Name: "Original Academic Year",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 6, 30));

        var firstResponse = await Client.PostAsJsonAsync(_academicYearsUri, request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicateRequest = new CreateAcademicYearRequest(
            Code: "AY-DUP-TEST",
            Name: "Duplicate Academic Year",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 6, 30));

        // Act
        var duplicateResponse = await Client.PostAsJsonAsync(_academicYearsUri, duplicateRequest);

        // Assert
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problem = await duplicateResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.Conflict);
        problem.Extensions.Should().ContainKey("errorCode");
        problem.Extensions["errorCode"]!.ToString().Should().Be("Operations.DuplicateAcademicYearCode");
    }

    private sealed record AcademicYearDbRow(
        Guid Id,
        string Code,
        string Name,
        DateOnly StartDate,
        DateOnly EndDate,
        string Status,
        string? Notes,
        DateTimeOffset CreatedAt,
        string? CreatedBy);
}
