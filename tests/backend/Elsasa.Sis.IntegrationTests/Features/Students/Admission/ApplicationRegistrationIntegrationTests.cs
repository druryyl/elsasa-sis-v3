namespace Elsasa.Sis.IntegrationTests.Features.Students.Admission;

using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Elsasa.Sis.IntegrationTests.Fixtures;
using Elsasa.Sis.Operations.Features.CalendarPeriods.CreateAcademicYear;
using Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.CreateOperationalPeriod;
using Elsasa.Sis.Students.Features.Admission.GetAdmissionApplicationById;
using Elsasa.Sis.Students.Features.Admission.GetApplicationStatusByNumber;
using Elsasa.Sis.Students.Features.Admission.RegisterApplication;
using FluentAssertions;
using Xunit;

/// <summary>
/// Integration tests verifying Application Registration, sequential numbering, multi-table transactions,
/// deduplication, and active registration uniqueness (P3-S04, P3-S05).
/// </summary>
public sealed class ApplicationRegistrationIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _registerStaffUri = "/api/students/admission/applications";
    private const string _registerPortalUri = "/api/students/admission/portal/applications";

    private async Task<(Guid AcademicYearId, Guid AdmissionPeriodId)> SetupAcademicYearAndAdmissionPeriodAsync(string suffix)
    {
        var ayCode = $"AY-{suffix}";
        var ayRequest = new CreateAcademicYearRequest(
            Code: ayCode,
            Name: $"Test AY {ayCode}",
            StartDate: new DateOnly(2026, 8, 1),
            EndDate: new DateOnly(2027, 7, 31));

        var ayResponse = await Client.PostAsJsonAsync("/api/operations/academic-years", ayRequest);
        ayResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var ay = await ayResponse.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();

        await Client.PostAsync($"/api/operations/academic-years/{ay!.Id}/approve", null);
        await Client.PostAsync($"/api/operations/academic-years/{ay.Id}/activate", null);

        var periodRequest = new CreateOperationalPeriodRequest(
            PeriodCode: $"ADM-{suffix}",
            Name: "Test Admission Window",
            PeriodType: "admission_window",
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            EndDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)));

        var periodResponse = await Client.PostAsJsonAsync($"/api/operations/academic-years/{ay.Id}/periods", periodRequest);
        periodResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var period = await periodResponse.Content.ReadFromJsonAsync<CreateOperationalPeriodResponse>();

        await ExecuteAsync("UPDATE operations.operational_periods SET status = 'active' WHERE id = @Id;", new { Id = period!.Id });

        return (ay.Id, period.Id);
    }

    [SkippableFact]
    public async Task RegisterApplication_Should_GenerateApplicationNumber_And_PersistMultiTableEntities()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (_, periodId) = await SetupAcademicYearAndAdmissionPeriodAsync($"REG-{Guid.NewGuid():N}"[..8]);

        var govId = $"3171{Random.Shared.Next(10000000, 99999999)}0001";
        var dob = new DateOnly(2018, 5, 20);

        var request = new RegisterApplicationRequest(
            ApplicantId: null,
            FullName: "Budi Santoso",
            DateOfBirth: dob,
            Gender: "Male",
            GovernmentIdType: "NIK",
            GovernmentIdNumber: govId,
            Email: "budi.santoso@elsasa.sch.id",
            Phone: "+6281234567890",
            Address: "Jl. Sudirman No. 45, Jakarta",
            PreviousSchoolName: "TK Pertiwi 1",
            PreviousSchoolAddress: "Jakarta Pusat",
            AdmissionPeriodId: periodId,
            ApplicationType: "new",
            RequestedEntryLevel: "Grade 1",
            RequestedCampus: "Central Campus",
            RequestedProgram: "Regular",
            Contacts:
            [
                new RegisterApplicationContactRequest(
                    FullName: "Agus Santoso",
                    Relationship: "Father",
                    Phone: "+628111222333",
                    Email: "agus.santoso@elsasa.sch.id",
                    Address: "Jl. Sudirman No. 45, Jakarta",
                    IsPrimary: true)
            ],
            BypassWindowCheck: false,
            Notes: "Priority registration");

        // Act
        var response = await Client.PostAsJsonAsync(_registerStaffUri, request);

        // Assert HTTP response
        var responseContent = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, because: responseContent);
        var result = await response.Content.ReadFromJsonAsync<RegisterApplicationResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.ApplicantId.Should().NotBeEmpty();
        result.Status.Should().Be("registered");
        result.RegistrationDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));

        // Format ADM-{Year}-{Seq:D4}
        Regex.IsMatch(result.ApplicationNumber, @"^ADM-\d{4}-\d{4}$").Should().BeTrue();

        // Verify Database Persistence via Dapper
        const string checkApplicantSql = "SELECT COUNT(1) FROM students.applicants WHERE id = @Id;";
        var applicantExists = await ExecuteScalarAsync<int>(checkApplicantSql, new { Id = result.ApplicantId });
        applicantExists.Should().Be(1);

        const string checkApplicationSql = "SELECT COUNT(1) FROM students.admission_applications WHERE id = @Id AND status = 'registered';";
        var applicationExists = await ExecuteScalarAsync<int>(checkApplicationSql, new { Id = result.Id });
        applicationExists.Should().Be(1);

        const string checkContactsSql = "SELECT COUNT(1) FROM students.admission_application_contacts WHERE application_id = @AppId;";
        var contactCount = await ExecuteScalarAsync<int>(checkContactsSql, new { AppId = result.Id });
        contactCount.Should().Be(1);

        // Verify Read Dossier Endpoint
        var dossierResponse = await Client.GetAsync($"/api/students/admission/applications/{result.Id}");
        dossierResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var dossier = await dossierResponse.Content.ReadFromJsonAsync<AdmissionApplicationDetailResponse>();
        dossier.Should().NotBeNull();
        dossier!.ApplicationNumber.Should().Be(result.ApplicationNumber);
        dossier.Applicant.FullName.Should().Be("Budi Santoso");
        dossier.Contacts.Should().HaveCount(1);
        dossier.Contacts[0].FullName.Should().Be("Agus Santoso");

        // Verify Public Status Lookup Endpoint
        var statusResponse = await Client.GetAsync(
            $"/api/students/admission/applications/status?applicationNumber={result.ApplicationNumber}&dateOfBirth={dob:yyyy-MM-dd}");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await statusResponse.Content.ReadFromJsonAsync<ApplicationStatusResponse>();
        status.Should().NotBeNull();
        status!.ApplicationNumber.Should().Be(result.ApplicationNumber);
        status.ApplicantFullName.Should().Be("Budi Santoso");
        status.Status.Should().Be("registered");
    }

    [SkippableFact]
    public async Task RegisterApplication_Should_ReturnConflict_When_DuplicateActiveRegistrationInSamePeriod()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (_, periodId) = await SetupAcademicYearAndAdmissionPeriodAsync($"DUP-{Guid.NewGuid():N}"[..8]);

        var govId = $"3171{Random.Shared.Next(10000000, 99999999)}0002";
        var dob = new DateOnly(2018, 6, 15);

        var request1 = new RegisterApplicationRequest(
            ApplicantId: null,
            FullName: "Dewi Lestari",
            DateOfBirth: dob,
            Gender: "Female",
            GovernmentIdType: "NIK",
            GovernmentIdNumber: govId,
            Email: "dewi.lestari@elsasa.sch.id",
            Phone: "+6281234567800",
            AdmissionPeriodId: periodId,
            RequestedEntryLevel: "Grade 1",
            Contacts:
            [
                new RegisterApplicationContactRequest("Rina Lestari", "Mother", "+6281234567801", IsPrimary: true)
            ]);

        // First registration succeeds
        var response1 = await Client.PostAsJsonAsync(_registerStaffUri, request1);
        response1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act: Attempt second registration for same period & government ID
        var request2 = request1 with { Email = "dewi.lestari.new@elsasa.sch.id" };
        var response2 = await Client.PostAsJsonAsync(_registerStaffUri, request2);

        // Assert
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [SkippableFact]
    public async Task RegisterApplication_Should_ReuseExistingApplicantRecord_When_MatchingGovernmentIdNumber()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (_, period1Id) = await SetupAcademicYearAndAdmissionPeriodAsync($"RE1-{Guid.NewGuid():N}"[..8]);
        var (_, period2Id) = await SetupAcademicYearAndAdmissionPeriodAsync($"RE2-{Guid.NewGuid():N}"[..8]);

        var govId = $"3171{Random.Shared.Next(10000000, 99999999)}0003";
        var dob = new DateOnly(2018, 7, 10);

        // Register in Period 1
        var request1 = new RegisterApplicationRequest(
            ApplicantId: null,
            FullName: "Citra Kirana",
            DateOfBirth: dob,
            Gender: "Female",
            GovernmentIdType: "NIK",
            GovernmentIdNumber: govId,
            Email: "citra.kirana@elsasa.sch.id",
            AdmissionPeriodId: period1Id,
            RequestedEntryLevel: "Grade 1",
            Contacts:
            [
                new RegisterApplicationContactRequest("Bambang Kirana", "Father", "+6281987654321", IsPrimary: true)
            ]);

        var response1 = await Client.PostAsJsonAsync(_registerStaffUri, request1);
        var content1 = await response1.Content.ReadAsStringAsync();
        response1.StatusCode.Should().Be(HttpStatusCode.Created, because: content1);
        var app1 = await response1.Content.ReadFromJsonAsync<RegisterApplicationResponse>();
        app1.Should().NotBeNull();

        // Register in Period 2 with the same government ID (deduplication matching)
        var request2 = new RegisterApplicationRequest(
            ApplicantId: null,
            FullName: "Citra Kirana Updated",
            DateOfBirth: dob,
            Gender: "Female",
            GovernmentIdType: "NIK",
            GovernmentIdNumber: govId,
            Email: "citra.updated@elsasa.sch.id",
            AdmissionPeriodId: period2Id,
            RequestedEntryLevel: "Grade 2",
            Contacts:
            [
                new RegisterApplicationContactRequest("Bambang Kirana", "Father", "+6281987654321", IsPrimary: true)
            ]);

        var response2 = await Client.PostAsJsonAsync(_registerStaffUri, request2);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);
        var app2 = await response2.Content.ReadFromJsonAsync<RegisterApplicationResponse>();
        app2.Should().NotBeNull();

        // Assert: Both applications reuse the same applicant record
        app2!.ApplicantId.Should().Be(app1!.ApplicantId);

        const string applicantCountSql = "SELECT COUNT(1) FROM students.applicants WHERE government_id_number = @GovId;";
        var totalApplicants = await ExecuteScalarAsync<int>(applicantCountSql, new { GovId = govId });
        totalApplicants.Should().Be(1, "deduplication matching must reuse applicant rather than creating a duplicate");
    }
}
