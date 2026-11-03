namespace Elsasa.Sis.IntegrationTests.Features.Students.Records;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Elsasa.Sis.IntegrationTests.Fixtures;
using Elsasa.Sis.Operations.Features.CalendarPeriods.CreateAcademicYear;
using Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.CreateOperationalPeriod;
using Elsasa.Sis.Students.Features.Admission.RecordAdmissionDecision;
using Elsasa.Sis.Students.Features.Admission.RegisterApplication;
using Elsasa.Sis.Students.Features.Enrollment.EnrollAcceptedApplicant;
using Elsasa.Sis.Students.Features.Records.Documents.UploadStudentDocument;
using Elsasa.Sis.Students.Features.Records.Documents.VerifyStudentDocument;
using Elsasa.Sis.Students.Features.Records.GetStudentById;
using Elsasa.Sis.Students.Features.Records.Guardians.AddGuardianRelationship;
using Elsasa.Sis.Students.Features.Records.SearchStudents;
using Elsasa.Sis.Students.Features.Records.UpdateStudentIdentity;
using FluentAssertions;
using Xunit;

/// <summary>
/// End-to-end integration tests for Student Records desk operations (P7-S11, P4-S05, P4-S06, P5-S07, P5-S08, P6-S09).
/// Verifies paginated student search, 360° dossier projection, certified identity update with independence flag,
/// primary guardian partial unique index invariant enforcement, and student document upload/stream/verify lifecycle.
/// </summary>
public sealed class StudentRecordsIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private async Task<(Guid StudentId, string StudentNumber, Guid AcademicYearId, string Campus, string Level, string FullName)> SetupEnrolledStudentAsync(string suffix)
    {
        var ayRequest = new CreateAcademicYearRequest(
            Code: $"AY-REC-{suffix}",
            Name: $"Records AY {suffix}",
            StartDate: new DateOnly(2026, 8, 1),
            EndDate: new DateOnly(2027, 7, 31));

        var ayResponse = await Client.PostAsJsonAsync("/api/operations/academic-years", ayRequest);
        ayResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var ay = await ayResponse.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();

        await Client.PostAsync($"/api/operations/academic-years/{ay!.Id}/approve", null);
        await Client.PostAsync($"/api/operations/academic-years/{ay.Id}/activate", null);

        var periodRequest = new CreateOperationalPeriodRequest(
            PeriodCode: $"ADM-REC-{suffix}",
            Name: $"Records Window {suffix}",
            PeriodType: "admission_window",
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            EndDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)));

        var periodResponse = await Client.PostAsJsonAsync($"/api/operations/academic-years/{ay.Id}/periods", periodRequest);
        periodResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var period = await periodResponse.Content.ReadFromJsonAsync<CreateOperationalPeriodResponse>();

        await ExecuteAsync("UPDATE operations.operational_periods SET status = 'active' WHERE id = @Id;", new { Id = period!.Id });

        var fullName = $"Student Records Candidate {suffix}";
        var govId = $"3173{Random.Shared.Next(10000000, 99999999)}0003";
        var regRequest = new RegisterApplicationRequest(
            ApplicantId: null,
            FullName: fullName,
            DateOfBirth: new DateOnly(2018, 3, 20),
            GovernmentIdType: "NIK",
            GovernmentIdNumber: govId,
            AdmissionPeriodId: period!.Id,
            RequestedEntryLevel: "Grade 1",
            RequestedCampus: "South Campus",
            Contacts:
            [
                new RegisterApplicationContactRequest("Father " + suffix, "Father", "+6281211112222", IsPrimary: true)
            ]);

        var regResponse = await Client.PostAsJsonAsync("/api/students/admission/applications", regRequest);
        regResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var reg = await regResponse.Content.ReadFromJsonAsync<RegisterApplicationResponse>();

        await Client.PostAsync($"/api/students/admission/applications/{reg!.Id}/start-review", null);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var decisionRequest = new RecordAdmissionDecisionRequest(
            DecisionType: "accepted",
            DecisionDate: today,
            EffectiveDate: today,
            Reason: "Candidate passed assessment",
            SupportingReference: $"DEC-REC-{suffix}");

        await Client.PostAsJsonAsync($"/api/students/admission/applications/{reg.Id}/decision", decisionRequest);

        var enrollRequest = new EnrollAcceptedApplicantRequest(
            ApplicantId: reg.ApplicantId,
            AcademicYearId: ay.Id,
            InitialCampus: "South Campus",
            InitialLevel: "Grade 1",
            InitialAcademicYear: "2026/2027",
            InitialProgram: "Standard Track",
            InitialCohort: "1-A",
            PaymentWaived: true,
            WaiverReference: $"SK-WAIVE-{suffix}",
            Guardians:
            [
                new EnrollGuardianRequest(
                    FullName: $"Initial Primary Guardian {suffix}",
                    RelationshipType: "father",
                    GovernmentIdType: "NIK",
                    GovernmentIdNumber: $"3174{Random.Shared.Next(10000000, 99999999)}0004",
                    Phone: "+6281233334444",
                    IsPrimaryContact: true)
            ]);

        var enrollResponse = await Client.PostAsJsonAsync("/api/students/enrollments", enrollRequest);
        enrollResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var enrolled = await enrollResponse.Content.ReadFromJsonAsync<EnrollAcceptedApplicantResponse>();

        return (enrolled!.StudentId, enrolled.StudentNumber, ay.Id, "South Campus", "Grade 1", fullName);
    }

    [SkippableFact]
    public async Task SearchStudents_Should_ReturnPaginatedList_MatchingFilters()
    {
        SkipIfDatabaseUnavailable();
        var suffix = $"SRCH-{Guid.NewGuid():N}"[..6];
        var (studentId, studentNumber, _, campus, level, fullName) = await SetupEnrolledStudentAsync(suffix);

        // Act: Search with search term matching student number and campus filter
        var url = $"/api/students?searchTerm={studentNumber}&campus={WebUtility.UrlEncode(campus)}&level={WebUtility.UrlEncode(level)}&status=enrolled&pageNumber=1&pageSize=10";
        var response = await Client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var searchResult = await response.Content.ReadFromJsonAsync<SearchStudentsResponse>();

        searchResult.Should().NotBeNull();
        searchResult!.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        searchResult.Items.Should().ContainSingle(s => s.Id == studentId);

        var item = searchResult.Items.Single(s => s.Id == studentId);
        item.StudentNumber.Should().Be(studentNumber);
        item.FullName.Should().Be(fullName);
        item.InitialCampus.Should().Be(campus);
        item.InitialLevel.Should().Be(level);
        item.CurrentStatus.Should().Be("enrolled");
        item.PrimaryGuardianName.Should().Contain("Initial Primary Guardian");
    }

    [SkippableFact]
    public async Task GetStudentById_Should_Return360DegreeDossier_WithAllFourTabs()
    {
        SkipIfDatabaseUnavailable();
        var suffix = $"DOS-{Guid.NewGuid():N}"[..6];
        var (studentId, studentNumber, _, campus, level, fullName) = await SetupEnrolledStudentAsync(suffix);

        // Add a certified student document so the document checklist tab has items
        await ExecuteAsync("""
            INSERT INTO students.student_documents (
                id, student_id, document_type, submission_status,
                file_reference, file_name, file_size_bytes, content_type,
                created_at, created_by
            ) VALUES (
                NEWID(), @StudentId, 'family_card', 'submitted',
                'storage/students/family_card.pdf', 'family_card.pdf', 2048, 'application/pdf',
                SYSUTCDATETIME(), 'test-admin'
            );
            """, new { StudentId = studentId });

        // Act: Fetch 360° student dossier
        var response = await Client.GetAsync($"/api/students/{studentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dossier = await response.Content.ReadFromJsonAsync<StudentDetailResponse>();

        dossier.Should().NotBeNull();

        // 1. Core certified identity
        dossier!.Id.Should().Be(studentId);
        dossier.StudentNumber.Should().Be(studentNumber);
        dossier.FullName.Should().Be(fullName);
        dossier.CurrentStatus.Should().Be("enrolled");
        dossier.IsIndependent.Should().BeFalse();

        // 2. Enrollment placement history
        dossier.EnrollmentHistory.Should().NotBeEmpty();
        dossier.EnrollmentHistory[0].Campus.Should().Be(campus);
        dossier.EnrollmentHistory[0].Level.Should().Be(level);
        dossier.EnrollmentHistory[0].PaymentWaived.Should().BeTrue();

        // 3. Guardian matrix
        dossier.GuardianRelationships.Should().NotBeEmpty();
        dossier.GuardianRelationships.Should().Contain(g => g.IsPrimaryContact && g.IsActive);

        // 4. Certified documents checklist
        dossier.Documents.Should().NotBeEmpty();
        dossier.Documents.Should().Contain(d => d.DocumentType == "family_card" && d.SubmissionStatus == "submitted");
    }

    [SkippableFact]
    public async Task UpdateStudentIdentity_Should_UpdateDatabaseAndAuditColumns_And_ToggleIndependence()
    {
        SkipIfDatabaseUnavailable();
        var suffix = $"UPD-{Guid.NewGuid():N}"[..6];
        var (studentId, _, _, _, _, _) = await SetupEnrolledStudentAsync(suffix);

        var updateRequest = new UpdateStudentIdentityRequest(
            FullName: $"Emancipated Student {suffix}",
            PreferredName: $"Student {suffix}",
            DateOfBirth: new DateOnly(2010, 6, 12),
            PlaceOfBirth: "Surabaya",
            Gender: "Female",
            Nationality: "Indonesian",
            GovernmentIdType: "NIK",
            GovernmentIdNumber: $"3175{Random.Shared.Next(10000000, 99999999)}0005",
            Address: "Jl. Merdeka No. 45, Jakarta",
            Phone: "+6281244445555",
            Email: $"student.{suffix}@example.com",
            IsIndependent: true,
            Notes: "Granted independent learner status by decree");

        // Act
        var response = await Client.PutAsJsonAsync($"/api/students/{studentId}/identity", updateRequest);

        // Assert HTTP 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify database updates
        const string sql = """
            SELECT full_name, preferred_name, place_of_birth, gender,
                   address, phone, email, is_independent, notes,
                   updated_at, updated_by
            FROM students.students
            WHERE id = @Id;
            """;

        var updated = (await QueryAsync<dynamic>(sql, new { Id = studentId })).Single();
        ((string)updated.full_name).Should().Be($"Emancipated Student {suffix}");
        ((string)updated.preferred_name).Should().Be($"Student {suffix}");
        ((string)updated.place_of_birth).Should().Be("Surabaya");
        ((string)updated.gender).Should().Be("Female");
        ((string)updated.address).Should().Be("Jl. Merdeka No. 45, Jakarta");
        ((string)updated.phone).Should().Be("+6281244445555");
        ((string)updated.email).Should().Be($"student.{suffix}@example.com");
        ((bool)updated.is_independent).Should().BeTrue();
        ((string)updated.notes).Should().Be("Granted independent learner status by decree");
        ((DateTimeOffset?)updated.updated_at).Should().NotBeNull();
        ((string)updated.updated_by).Should().NotBeNullOrWhiteSpace();
    }

    [SkippableFact]
    public async Task AddGuardianRelationship_WithPrimaryContact_Should_DemotePriorPrimaryContact_MaintainingPartialUniqueIndex()
    {
        SkipIfDatabaseUnavailable();
        var suffix = $"GREL-{Guid.NewGuid():N}"[..6];
        var (studentId, _, _, _, _, _) = await SetupEnrolledStudentAsync(suffix);

        // Initial primary contact exists from enrollment setup
        const string initialCheckSql = """
            SELECT relationship_type, is_primary_contact, is_active
            FROM students.student_guardian_relationships
            WHERE student_id = @StudentId AND is_primary_contact = 1 AND is_active = 1;
            """;
        var initialPrimary = (await QueryAsync<dynamic>(initialCheckSql, new { StudentId = studentId })).ToList();
        initialPrimary.Should().HaveCount(1);
        ((string)initialPrimary[0].relationship_type).Should().Be("father");

        // Act: Add mother as primary contact
        var newGuardianGovId = $"3176{Random.Shared.Next(10000000, 99999999)}0006";
        var addGuardianRequest = new AddGuardianRelationshipRequest(
            FullName: $"Mother of {suffix}",
            RelationshipType: "mother",
            GovernmentIdType: "NIK",
            GovernmentIdNumber: newGuardianGovId,
            Phone: "+6281266667777",
            Email: $"mother.{suffix}@example.com",
            IsPrimaryContact: true,
            IsEmergencyContact: true);

        var addResponse = await Client.PostAsJsonAsync($"/api/students/{studentId}/guardians", addGuardianRequest);
        addResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Assert database state: Prior primary contact was demoted (is_primary_contact = 0)
        // and new contact is now the sole active primary contact satisfying ux_students_rel_primary_active
        const string verifyAllSql = """
            SELECT r.relationship_type, r.is_primary_contact, r.is_active, g.full_name
            FROM students.student_guardian_relationships r
            INNER JOIN students.guardians g ON g.id = r.guardian_id
            WHERE r.student_id = @StudentId;
            """;
        var allRelationships = (await QueryAsync<dynamic>(verifyAllSql, new { StudentId = studentId })).ToList();
        allRelationships.Should().HaveCount(2);

        var fatherRel = allRelationships.Single(r => (string)r.relationship_type == "father");
        ((bool)fatherRel.is_primary_contact).Should().BeFalse("prior primary contact must be demoted");
        ((bool)fatherRel.is_active).Should().BeTrue();

        var motherRel = allRelationships.Single(r => (string)r.relationship_type == "mother");
        ((bool)motherRel.is_primary_contact).Should().BeTrue("newly added guardian becomes primary");
        ((bool)motherRel.is_active).Should().BeTrue();

        // Enforce SQL Server partial unique index invariant: exactly 1 active primary contact for this student
        const string primaryCountSql = """
            SELECT COUNT(1)
            FROM students.student_guardian_relationships
            WHERE student_id = @StudentId AND is_primary_contact = 1 AND is_active = 1;
            """;
        var primaryCount = await ExecuteScalarAsync<int>(primaryCountSql, new { StudentId = studentId });
        primaryCount.Should().Be(1);
    }

    [SkippableFact]
    public async Task StudentDocumentLifecycle_Should_SupportUpload_DownloadStreaming_And_Verification()
    {
        SkipIfDatabaseUnavailable();
        var suffix = $"DOCL-{Guid.NewGuid():N}"[..6];
        var (studentId, _, _, _, _, _) = await SetupEnrolledStudentAsync(suffix);

        // 1. Upload student document via multipart/form-data
        using var uploadContent = new MultipartFormDataContent();
        var fileBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 }; // "%PDF-1.4"
        var byteContent = new ByteArrayContent(fileBytes);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        uploadContent.Add(byteContent, "file", "academic_transcript.pdf");
        uploadContent.Add(new StringContent("academic_transcript"), "documentType");

        var uploadResponse = await Client.PostAsync($"/api/students/{studentId}/documents", uploadContent);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<UploadStudentDocumentResponse>();
        uploadResult.Should().NotBeNull();
        uploadResult!.Id.Should().NotBeEmpty();
        uploadResult.DocumentType.Should().Be("academic_transcript");
        uploadResult.SubmissionStatus.Should().Be("submitted");

        var docId = uploadResult.Id;

        // 2. Download streaming
        var downloadResponse = await Client.GetAsync($"/api/students/{studentId}/documents/{docId}/file");
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        downloadResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        downloadedBytes.Should().Equal(fileBytes);

        // 3. Verification status transition
        var verifyRequest = new VerifyStudentDocumentRequest(
            SubmissionStatus: "verified",
            Notes: "Officially certified by academic registrar");

        var verifyResponse = await Client.PutAsJsonAsync($"/api/students/{studentId}/documents/{docId}/verify", verifyRequest);
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var verifyResult = await verifyResponse.Content.ReadFromJsonAsync<VerifyStudentDocumentResponse>();
        verifyResult.Should().NotBeNull();
        verifyResult!.SubmissionStatus.Should().Be("verified");

        // Verify in database
        const string checkSql = """
            SELECT submission_status, verified_by, verified_date, notes
            FROM students.student_documents
            WHERE id = @Id;
            """;
        var docRow = (await QueryAsync<dynamic>(checkSql, new { Id = docId })).Single();
        ((string)docRow.submission_status).Should().Be("verified");
        ((string)docRow.verified_by).Should().NotBeNullOrWhiteSpace();
        ((DateTime?)docRow.verified_date).Should().NotBeNull();
        ((string)docRow.notes).Should().Be("Officially certified by academic registrar");
    }
}
