namespace Elsasa.Sis.IntegrationTests.Features.Students.Admission;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Elsasa.Sis.IntegrationTests.Fixtures;
using Elsasa.Sis.Operations.Features.CalendarPeriods.CreateAcademicYear;
using Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.CreateOperationalPeriod;
using Elsasa.Sis.Students.Features.Admission.Documents.UploadApplicantDocument;
using Elsasa.Sis.Students.Features.Admission.Documents.VerifyApplicantDocument;
using Elsasa.Sis.Students.Features.Admission.RegisterApplication;
using FluentAssertions;
using Xunit;

/// <summary>
/// Integration tests verifying applicant document upload via multipart form, physical file streaming,
/// and document verification lifecycle (P4-S06).
/// </summary>
public sealed class DocumentManagementIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private async Task<Guid> CreateRegisteredApplicationAsync()
    {
        var aySuffix = $"DOC-{Guid.NewGuid():N}"[..8];
        var ayRequest = new CreateAcademicYearRequest(
            Code: $"AY-{aySuffix}",
            Name: "Document Test AY",
            StartDate: new DateOnly(2026, 8, 1),
            EndDate: new DateOnly(2027, 7, 31));

        var ayResponse = await Client.PostAsJsonAsync("/api/operations/academic-years", ayRequest);
        ayResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var ay = await ayResponse.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();

        await Client.PostAsync($"/api/operations/academic-years/{ay!.Id}/approve", null);
        await Client.PostAsync($"/api/operations/academic-years/{ay.Id}/activate", null);

        var periodRequest = new CreateOperationalPeriodRequest(
            PeriodCode: $"ADM-{aySuffix}",
            Name: "Document Admission Window",
            PeriodType: "admission_window",
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            EndDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(25)));

        var periodResponse = await Client.PostAsJsonAsync($"/api/operations/academic-years/{ay.Id}/periods", periodRequest);
        periodResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var period = await periodResponse.Content.ReadFromJsonAsync<CreateOperationalPeriodResponse>();

        await ExecuteAsync("UPDATE operations.operational_periods SET status = 'active' WHERE id = @Id;", new { Id = period!.Id });

        var regRequest = new RegisterApplicationRequest(
            ApplicantId: null,
            FullName: "Farhan Hakim",
            DateOfBirth: new DateOnly(2018, 8, 12),
            GovernmentIdType: "NIK",
            GovernmentIdNumber: $"3171{Random.Shared.Next(10000000, 99999999)}0004",
            Email: "farhan.hakim@elsasa.sch.id",
            AdmissionPeriodId: period!.Id,
            RequestedEntryLevel: "Grade 1",
            Contacts:
            [
                new RegisterApplicationContactRequest("Hakim Senior", "Father", "+6281211112222", IsPrimary: true)
            ]);

        var regResponse = await Client.PostAsJsonAsync("/api/students/admission/applications", regRequest);
        regResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var reg = await regResponse.Content.ReadFromJsonAsync<RegisterApplicationResponse>();
        return reg!.Id;
    }

    [SkippableFact]
    public async Task DocumentManagement_Should_SupportUploadDownloadAndVerificationLifecycle()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var applicationId = await CreateRegisteredApplicationAsync();

        var documentType = "birth_certificate";
        var fileBytes = Encoding.UTF8.GetBytes("%PDF-1.4 Mock Birth Certificate content for integration testing %EOF");
        var fileName = "akta_kelahiran_farhan.pdf";
        var contentType = "application/pdf";

        // 1. Act: Multipart Upload (POST /api/students/admission/applications/{id}/documents)
        using var formData = new MultipartFormDataContent();
        var byteContent = new ByteArrayContent(fileBytes);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        formData.Add(byteContent, "file", fileName);
        formData.Add(new StringContent(documentType), "documentType");

        var uploadResponse = await Client.PostAsync($"/api/students/admission/applications/{applicationId}/documents", formData);

        // Assert Upload
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<UploadApplicantDocumentResponse>();
        uploadResult.Should().NotBeNull();
        uploadResult!.ApplicationId.Should().Be(applicationId);
        uploadResult.DocumentType.Should().Be(documentType);
        uploadResult.SubmissionStatus.Should().Be("submitted");
        uploadResult.FileReference.Should().NotBeNullOrWhiteSpace();

        // Verify Database state via Dapper
        const string selectDocSql = """
            SELECT
                id AS Id,
                submission_status AS SubmissionStatus,
                file_reference AS FileReference,
                file_name AS FileName,
                file_size_bytes AS FileSizeBytes,
                content_type AS ContentType
            FROM students.applicant_documents
            WHERE id = @Id;
            """;

        var dbDoc = await QuerySingleOrDefaultAsync<ApplicantDocumentDbRow>(selectDocSql, new { Id = uploadResult.Id });
        dbDoc.Should().NotBeNull();
        dbDoc!.SubmissionStatus.Should().Be("submitted");
        dbDoc.FileName.Should().Be(fileName);
        dbDoc.FileSizeBytes.Should().Be(fileBytes.Length);
        dbDoc.ContentType.Should().Be(contentType);

        // 2. Act: File Streaming Download (GET /api/students/admission/applications/{id}/documents/{docId}/file)
        var downloadResponse = await Client.GetAsync($"/api/students/admission/applications/{applicationId}/documents/{uploadResult.Id}/file");

        // Assert Download
        var downloadError = await downloadResponse.Content.ReadAsStringAsync();
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: downloadError);
        downloadResponse.Content.Headers.ContentType?.MediaType.Should().Be(contentType);

        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        downloadedBytes.Should().Equal(fileBytes, "downloaded file bytes must match exact uploaded bytes");

        // 3. Act: Document Verification (PUT /api/students/admission/applications/{id}/documents/{docId}/verify)
        var verifyRequest = new VerifyApplicantDocumentRequest(
            SubmissionStatus: "verified",
            Notes: "Verified against original civil registry certificate.");

        var verifyResponse = await Client.PutAsJsonAsync(
            $"/api/students/admission/applications/{applicationId}/documents/{uploadResult.Id}/verify",
            verifyRequest);

        // Assert Verification
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var verifyResult = await verifyResponse.Content.ReadFromJsonAsync<VerifyApplicantDocumentResponse>();
        verifyResult.Should().NotBeNull();
        verifyResult!.SubmissionStatus.Should().Be("verified");
        verifyResult.VerifiedBy.Should().NotBeNullOrWhiteSpace();
        verifyResult.VerifiedDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));

        // Verify DB Verification update
        const string checkVerificationSql = """
            SELECT submission_status AS SubmissionStatus, verified_by AS VerifiedBy, verified_date AS VerifiedDate
            FROM students.applicant_documents
            WHERE id = @Id;
            """;

        var verifiedDoc = await QuerySingleOrDefaultAsync<VerifiedDocDbRow>(checkVerificationSql, new { Id = uploadResult.Id });
        verifiedDoc.Should().NotBeNull();
        verifiedDoc!.SubmissionStatus.Should().Be("verified");
        verifiedDoc.VerifiedBy.Should().NotBeNullOrWhiteSpace();
        verifiedDoc.VerifiedDate.Should().NotBeNull();
    }

    private sealed record ApplicantDocumentDbRow(Guid Id, string SubmissionStatus, string FileReference, string FileName, long FileSizeBytes, string ContentType);
    private sealed record VerifiedDocDbRow(string SubmissionStatus, string VerifiedBy, DateOnly? VerifiedDate);
}
