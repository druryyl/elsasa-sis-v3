namespace Elsasa.Sis.IntegrationTests.Features.Students.Admission;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.IntegrationTests.Fixtures;
using Elsasa.Sis.Operations.Features.CalendarPeriods.CreateAcademicYear;
using Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.CreateOperationalPeriod;
using Elsasa.Sis.Students.Features.Admission.AssessmentEvidence.AddAssessmentEvidence;
using Elsasa.Sis.Students.Features.Admission.AssessmentEvidence.UpdateAssessmentEvidence;
using Elsasa.Sis.Students.Features.Admission.GetAdmissionApplicationById;
using Elsasa.Sis.Students.Features.Admission.RegisterApplication;
using FluentAssertions;
using Xunit;

/// <summary>
/// Integration tests verifying the full CRUD lifecycle on entrance assessment evidence (P4-S07).
/// </summary>
public sealed class AssessmentEvidenceIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private async Task<Guid> CreateRegisteredApplicationAsync()
    {
        var suffix = $"EVD-{Guid.NewGuid():N}"[..8];
        var ayRequest = new CreateAcademicYearRequest(
            Code: $"AY-{suffix}",
            Name: "Assessment Test AY",
            StartDate: new DateOnly(2026, 8, 1),
            EndDate: new DateOnly(2027, 7, 31));

        var ayResponse = await Client.PostAsJsonAsync("/api/operations/academic-years", ayRequest);
        ayResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var ay = await ayResponse.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();

        await Client.PostAsync($"/api/operations/academic-years/{ay!.Id}/approve", null);
        await Client.PostAsync($"/api/operations/academic-years/{ay.Id}/activate", null);

        var periodRequest = new CreateOperationalPeriodRequest(
            PeriodCode: $"ADM-{suffix}",
            Name: "Assessment Admission Window",
            PeriodType: "admission_window",
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            EndDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(25)));

        var periodResponse = await Client.PostAsJsonAsync($"/api/operations/academic-years/{ay.Id}/periods", periodRequest);
        periodResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var period = await periodResponse.Content.ReadFromJsonAsync<CreateOperationalPeriodResponse>();

        await ExecuteAsync("UPDATE operations.operational_periods SET status = 'active' WHERE id = @Id;", new { Id = period!.Id });

        var regRequest = new RegisterApplicationRequest(
            ApplicantId: null,
            FullName: "Maya Safira",
            DateOfBirth: new DateOnly(2018, 9, 5),
            GovernmentIdType: "NIK",
            GovernmentIdNumber: $"3171{Random.Shared.Next(10000000, 99999999)}0005",
            AdmissionPeriodId: period!.Id,
            RequestedEntryLevel: "Grade 1",
            Contacts:
            [
                new RegisterApplicationContactRequest("Rahmat Safira", "Father", "+6281233334444", IsPrimary: true)
            ]);

        var regResponse = await Client.PostAsJsonAsync("/api/students/admission/applications", regRequest);
        regResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var reg = await regResponse.Content.ReadFromJsonAsync<RegisterApplicationResponse>();
        return reg!.Id;
    }

    [SkippableFact]
    public async Task AssessmentEvidence_Should_SupportCreateUpdateAndDeleteLifecycle()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var applicationId = await CreateRegisteredApplicationAsync();

        // 1. Act: Create (POST /api/students/admission/applications/{id}/assessment-evidence)
        var addRequest = new AddAssessmentEvidenceRequest(
            AssessmentType: "reading_readiness",
            Score: "85.5",
            Source: "Yayasan Psikologi Terpadu",
            AssessmentDate: new DateOnly(2026, 10, 15),
            Notes: "Strong vocabulary, fluent word recognition.");

        var createResponse = await Client.PostAsJsonAsync(
            $"/api/students/admission/applications/{applicationId}/assessment-evidence",
            addRequest);

        // Assert Create
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<AddAssessmentEvidenceResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();

        // Verify in Database
        const string selectSql = """
            SELECT score AS Score, source AS Source, notes AS Notes
            FROM students.entrance_assessment_evidence
            WHERE id = @Id;
            """;

        var dbRow = await QuerySingleOrDefaultAsync<EvidenceDbRow>(selectSql, new { Id = created.Id });
        dbRow.Should().NotBeNull();
        dbRow!.Score.Should().Be("85.5");
        dbRow.Source.Should().Be("Yayasan Psikologi Terpadu");

        // Verify in Dossier Query
        var dossierResponse = await Client.GetAsync($"/api/students/admission/applications/{applicationId}");
        dossierResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var dossier = await dossierResponse.Content.ReadFromJsonAsync<AdmissionApplicationDetailResponse>();
        dossier.Should().NotBeNull();
        dossier!.AssessmentEvidence.Should().HaveCount(1);
        dossier.AssessmentEvidence[0].Id.Should().Be(created.Id);
        dossier.AssessmentEvidence[0].AssessmentType.Should().Be("reading_readiness");
        dossier.AssessmentEvidence[0].Score.Should().Be("85.5");

        // 2. Act: Update (PUT /api/students/admission/applications/{id}/assessment-evidence/{evidenceId})
        var updateRequest = new UpdateAssessmentEvidenceRequest(
            AssessmentType: "reading_readiness",
            Score: "92.0",
            Source: "Yayasan Psikologi Terpadu",
            AssessmentDate: new DateOnly(2026, 10, 15),
            Notes: "Re-evaluated with advanced comprehension passage; score revised.");

        var updateResponse = await Client.PutAsJsonAsync(
            $"/api/students/admission/applications/{applicationId}/assessment-evidence/{created.Id}",
            updateRequest);

        // Assert Update
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<UpdateAssessmentEvidenceResponse>();
        updated.Should().NotBeNull();
        updated!.Score.Should().Be("92.0");

        // Verify in Database
        var updatedDbRow = await QuerySingleOrDefaultAsync<EvidenceDbRow>(selectSql, new { Id = created.Id });
        updatedDbRow.Should().NotBeNull();
        updatedDbRow!.Score.Should().Be("92.0");

        // 3. Act: Delete (DELETE /api/students/admission/applications/{id}/assessment-evidence/{evidenceId})
        var deleteResponse = await Client.DeleteAsync(
            $"/api/students/admission/applications/{applicationId}/assessment-evidence/{created.Id}");

        // Assert Delete
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify Database row removed
        const string countSql = "SELECT COUNT(1) FROM students.entrance_assessment_evidence WHERE id = @Id;";
        var count = await ExecuteScalarAsync<int>(countSql, new { Id = created.Id });
        count.Should().Be(0);

        // Verify Dossier has 0 evidence records
        var dossierAfterDeleteResponse = await Client.GetAsync($"/api/students/admission/applications/{applicationId}");
        var dossierAfterDelete = await dossierAfterDeleteResponse.Content.ReadFromJsonAsync<AdmissionApplicationDetailResponse>();
        dossierAfterDelete!.AssessmentEvidence.Should().BeEmpty();
    }

    private sealed record EvidenceDbRow(string Score, string Source, string Notes);
}
