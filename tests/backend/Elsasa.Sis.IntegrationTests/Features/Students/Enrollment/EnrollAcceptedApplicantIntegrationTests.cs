namespace Elsasa.Sis.IntegrationTests.Features.Students.Enrollment;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.IntegrationTests.Fixtures;
using Elsasa.Sis.Operations.Features.CalendarPeriods.CreateAcademicYear;
using Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.CreateOperationalPeriod;
using Elsasa.Sis.Students.Features.Admission.RecordAdmissionDecision;
using Elsasa.Sis.Students.Features.Admission.RegisterApplication;
using Elsasa.Sis.Students.Features.Enrollment.EnrollAcceptedApplicant;
using FluentAssertions;
using Xunit;

/// <summary>
/// End-to-end integration tests for applicant enrollment (P7-S11, P3-S04, ADR-003, ADR-004, ADR-005, GAP-01).
/// Verifies atomic multi-table persistence, sequential student number generation, document promotion without duplication,
/// guardian linking, payment clearance gate variations, and duplicate rejection.
/// </summary>
public sealed class EnrollAcceptedApplicantIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private async Task<(Guid AcademicYearId, Guid AdmissionPeriodId, Guid ApplicationId, Guid ApplicantId, string GovId)> SetupAcceptedApplicationAsync(string suffix)
    {
        var ayRequest = new CreateAcademicYearRequest(
            Code: $"AY-{suffix}",
            Name: $"Enrollment Academic Year {suffix}",
            StartDate: new DateOnly(2026, 8, 1),
            EndDate: new DateOnly(2027, 7, 31));

        var ayResponse = await Client.PostAsJsonAsync("/api/operations/academic-years", ayRequest);
        ayResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var ay = await ayResponse.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();

        await Client.PostAsync($"/api/operations/academic-years/{ay!.Id}/approve", null);
        await Client.PostAsync($"/api/operations/academic-years/{ay.Id}/activate", null);

        var periodRequest = new CreateOperationalPeriodRequest(
            PeriodCode: $"ADM-{suffix}",
            Name: $"Admission Window {suffix}",
            PeriodType: "admission_window",
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            EndDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)));

        var periodResponse = await Client.PostAsJsonAsync($"/api/operations/academic-years/{ay.Id}/periods", periodRequest);
        periodResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var period = await periodResponse.Content.ReadFromJsonAsync<CreateOperationalPeriodResponse>();

        await ExecuteAsync("UPDATE operations.operational_periods SET status = 'active' WHERE id = @Id;", new { Id = period!.Id });

        var govId = $"3171{Random.Shared.Next(10000000, 99999999)}0001";
        var regRequest = new RegisterApplicationRequest(
            ApplicantId: null,
            FullName: $"Admitted Student {suffix}",
            DateOfBirth: new DateOnly(2018, 5, 15),
            GovernmentIdType: "NIK",
            GovernmentIdNumber: govId,
            AdmissionPeriodId: period!.Id,
            RequestedEntryLevel: "Grade 1",
            RequestedCampus: "Main Campus",
            Contacts:
            [
                new RegisterApplicationContactRequest("Father of " + suffix, "Father", "+6281200001111", IsPrimary: true)
            ]);

        var regResponse = await Client.PostAsJsonAsync("/api/students/admission/applications", regRequest);
        regResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var reg = await regResponse.Content.ReadFromJsonAsync<RegisterApplicationResponse>();

        // Transition through under_review to decided: accepted
        var startReviewResponse = await Client.PostAsync($"/api/students/admission/applications/{reg!.Id}/start-review", null);
        startReviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var decisionRequest = new RecordAdmissionDecisionRequest(
            DecisionType: "accepted",
            DecisionDate: today,
            EffectiveDate: today,
            Reason: "Fully qualified applicant",
            SupportingReference: $"DEC-REF-{suffix}");

        var decisionResponse = await Client.PostAsJsonAsync($"/api/students/admission/applications/{reg.Id}/decision", decisionRequest);
        decisionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        return (ay.Id, period.Id, reg.Id, reg.ApplicantId, govId);
    }

    [SkippableFact]
    public async Task EnrollAcceptedApplicant_Should_CreateAuthoritativeStudentRecord_And_PromoteDocuments_And_PublishEvent()
    {
        SkipIfDatabaseUnavailable();
        var suffix = $"ENR-{Guid.NewGuid():N}"[..8];
        var (academicYearId, _, applicationId, applicantId, govId) = await SetupAcceptedApplicationAsync(suffix);

        // Seed a verified applicant document to verify promotion
        var docId = Guid.NewGuid();
        const string fileRef = "storage/admission/docs/birth_cert.pdf";
        await ExecuteAsync("""
            INSERT INTO students.applicant_documents (
                id, application_id, document_type, submission_status,
                file_reference, file_name, file_size_bytes, content_type,
                created_at, created_by
            ) VALUES (
                @Id, @AppId, 'birth_certificate', 'verified',
                @FileRef, 'birth_certificate.pdf', 1024, 'application/pdf',
                SYSUTCDATETIME(), 'test-admin'
            );
            """, new { Id = docId, AppId = applicationId, FileRef = fileRef });

        // Act: Enroll the accepted applicant
        var guardianGovId = $"3172{Random.Shared.Next(10000000, 99999999)}0002";
        var enrollRequest = new EnrollAcceptedApplicantRequest(
            ApplicantId: applicantId,
            AcademicYearId: academicYearId,
            InitialCampus: "Main Campus",
            InitialLevel: "Grade 1",
            InitialAcademicYear: "2026/2027",
            InitialProgram: "National Curriculum",
            InitialCohort: "2026-A",
            PaymentWaived: false,
            PaymentReference: "BANK-MANDIRI-2026-001",
            Guardians:
            [
                new EnrollGuardianRequest(
                    FullName: "Jane Doe Guardian",
                    RelationshipType: "mother",
                    GovernmentIdType: "NIK",
                    GovernmentIdNumber: guardianGovId,
                    Phone: "+6281299998888",
                    Email: "jane.doe@example.com",
                    IsPrimaryContact: true)
            ]);

        var enrollResponse = await Client.PostAsJsonAsync("/api/students/enrollments", enrollRequest);

        // Assert HTTP 201 Created
        enrollResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var responseBody = await enrollResponse.Content.ReadFromJsonAsync<EnrollAcceptedApplicantResponse>();
        responseBody.Should().NotBeNull();
        responseBody!.StudentId.Should().NotBeEmpty();
        responseBody.EnrollmentId.Should().NotBeEmpty();
        responseBody.StudentNumber.Should().MatchRegex(@"^STD-\d{4}-\d{4}$");

        // Assert students.students record created with authoritative attributes
        const string studentSql = "SELECT * FROM students.students WHERE id = @Id;";
        var student = (await QueryAsync<dynamic>(studentSql, new { Id = responseBody.StudentId })).Single();
        ((string)student.student_number).Should().Be(responseBody.StudentNumber);
        ((Guid)student.applicant_id).Should().Be(applicantId);
        ((string)student.current_status).Should().Be("active");

        // Assert students.enrollments record created
        const string enrollmentSql = "SELECT * FROM students.enrollments WHERE id = @Id;";
        var enrollment = (await QueryAsync<dynamic>(enrollmentSql, new { Id = responseBody.EnrollmentId })).Single();
        ((Guid)enrollment.student_id).Should().Be(responseBody.StudentId);
        ((Guid)enrollment.application_id).Should().Be(applicationId);
        ((string)enrollment.payment_reference).Should().Be("BANK-MANDIRI-2026-001");
        ((bool)enrollment.enrollment_payment_confirmed).Should().BeTrue();

        // Assert document promotion: document promoted with original file reference reused
        const string docSql = "SELECT * FROM students.student_documents WHERE student_id = @StudentId;";
        var studentDocs = (await QueryAsync<dynamic>(docSql, new { StudentId = responseBody.StudentId })).ToList();
        studentDocs.Should().HaveCount(1);
        ((string)studentDocs[0].file_reference).Should().Be(fileRef);
        ((string)studentDocs[0].document_type).Should().Be("birth_certificate");
        ((string)studentDocs[0].submission_status).Should().Be("verified");

        // Assert guardian linked with primary contact authority
        const string guardianSql = """
            SELECT g.full_name, r.relationship_type, r.is_primary_contact, r.is_active
            FROM students.student_guardian_relationships r
            INNER JOIN students.guardians g ON g.id = r.guardian_id
            WHERE r.student_id = @StudentId;
            """;
        var guardians = (await QueryAsync<dynamic>(guardianSql, new { StudentId = responseBody.StudentId })).ToList();
        guardians.Should().HaveCount(1);
        ((string)guardians[0].full_name).Should().Be("Jane Doe Guardian");
        ((bool)guardians[0].is_primary_contact).Should().BeTrue();
        ((bool)guardians[0].is_active).Should().BeTrue();

        // Assert post-commit event published
        Factory.EventCollector.StudentEnrolledEvents.Should().ContainSingle(e =>
            e.StudentId == responseBody.StudentId &&
            e.ApplicantId == applicantId &&
            e.AcademicYearId == academicYearId);
    }

    [SkippableFact]
    public async Task EnrollAcceptedApplicant_Should_GenerateSequentialNonCollidingStudentNumbers()
    {
        SkipIfDatabaseUnavailable();
        var (ayId1, _, _, applicantId1, _) = await SetupAcceptedApplicationAsync($"SEQ1-{Guid.NewGuid():N}"[..6]);
        var (ayId2, _, _, applicantId2, _) = await SetupAcceptedApplicationAsync($"SEQ2-{Guid.NewGuid():N}"[..6]);

        var req1 = new EnrollAcceptedApplicantRequest(
            ApplicantId: applicantId1,
            AcademicYearId: ayId1,
            InitialCampus: "Campus A",
            InitialLevel: "Grade 1",
            InitialAcademicYear: "2026/2027",
            PaymentWaived: true,
            WaiverReference: "SK-WAIVE-01");

        var req2 = new EnrollAcceptedApplicantRequest(
            ApplicantId: applicantId2,
            AcademicYearId: ayId2,
            InitialCampus: "Campus B",
            InitialLevel: "Grade 2",
            InitialAcademicYear: "2026/2027",
            PaymentWaived: true,
            WaiverReference: "SK-WAIVE-02");

        var res1 = await Client.PostAsJsonAsync("/api/students/enrollments", req1);
        var res2 = await Client.PostAsJsonAsync("/api/students/enrollments", req2);

        res1.StatusCode.Should().Be(HttpStatusCode.Created);
        res2.StatusCode.Should().Be(HttpStatusCode.Created);

        var body1 = await res1.Content.ReadFromJsonAsync<EnrollAcceptedApplicantResponse>();
        var body2 = await res2.Content.ReadFromJsonAsync<EnrollAcceptedApplicantResponse>();

        body1!.StudentNumber.Should().NotBe(body2!.StudentNumber);
        body1.StudentNumber.Should().MatchRegex(@"^STD-\d{4}-\d{4}$");
        body2.StudentNumber.Should().MatchRegex(@"^STD-\d{4}-\d{4}$");
    }

    [SkippableFact]
    public async Task EnrollAcceptedApplicant_Should_ReturnConflict_When_ApplicationNotAccepted()
    {
        SkipIfDatabaseUnavailable();
        var suffix = $"UNACC-{Guid.NewGuid():N}"[..6];

        // Register applicant but leave status as 'registered' (not accepted)
        var ayRequest = new CreateAcademicYearRequest(
            Code: $"AY-{suffix}",
            Name: $"AY {suffix}",
            StartDate: new DateOnly(2026, 8, 1),
            EndDate: new DateOnly(2027, 7, 31));
        var ayResponse = await Client.PostAsJsonAsync("/api/operations/academic-years", ayRequest);
        var ay = await ayResponse.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();
        await Client.PostAsync($"/api/operations/academic-years/{ay!.Id}/approve", null);
        await Client.PostAsync($"/api/operations/academic-years/{ay.Id}/activate", null);

        var periodRequest = new CreateOperationalPeriodRequest(
            PeriodCode: $"ADM-{suffix}",
            Name: $"Adm {suffix}",
            PeriodType: "admission_window",
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            EndDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)));
        var periodResponse = await Client.PostAsJsonAsync($"/api/operations/academic-years/{ay.Id}/periods", periodRequest);
        var period = await periodResponse.Content.ReadFromJsonAsync<CreateOperationalPeriodResponse>();
        await ExecuteAsync("UPDATE operations.operational_periods SET status = 'active' WHERE id = @Id;", new { Id = period!.Id });

        var regRequest = new RegisterApplicationRequest(
            ApplicantId: null,
            FullName: $"Unaccepted Applicant {suffix}",
            DateOfBirth: new DateOnly(2018, 1, 1),
            AdmissionPeriodId: period!.Id,
            RequestedEntryLevel: "Grade 1");
        var regResponse = await Client.PostAsJsonAsync("/api/students/admission/applications", regRequest);
        var reg = await regResponse.Content.ReadFromJsonAsync<RegisterApplicationResponse>();

        var enrollRequest = new EnrollAcceptedApplicantRequest(
            ApplicantId: reg!.ApplicantId,
            AcademicYearId: ay.Id,
            InitialCampus: "Campus A",
            InitialLevel: "Grade 1",
            InitialAcademicYear: "2026/2027",
            PaymentWaived: true,
            WaiverReference: "SK-WAIVER-TEST");

        // Act
        var enrollResponse = await Client.PostAsJsonAsync("/api/students/enrollments", enrollRequest);

        // Assert 409 Conflict
        enrollResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [SkippableFact]
    public async Task EnrollAcceptedApplicant_Should_ReturnConflict_When_ApplicantAlreadyEnrolled()
    {
        SkipIfDatabaseUnavailable();
        var (academicYearId, _, _, applicantId, _) = await SetupAcceptedApplicationAsync($"DUP-{Guid.NewGuid():N}"[..6]);

        var enrollRequest = new EnrollAcceptedApplicantRequest(
            ApplicantId: applicantId,
            AcademicYearId: academicYearId,
            InitialCampus: "Main Campus",
            InitialLevel: "Grade 1",
            InitialAcademicYear: "2026/2027",
            PaymentWaived: true,
            WaiverReference: "SK-WAIVER-DUP");

        // First enrollment succeeds
        var firstResponse = await Client.PostAsJsonAsync("/api/students/enrollments", enrollRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Second enrollment attempt with same applicant returns 409 Conflict
        var secondResponse = await Client.PostAsJsonAsync("/api/students/enrollments", enrollRequest);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [SkippableFact]
    public async Task PaymentGate_UnconfirmedPaymentWithoutWaiver_Should_ReturnConflict()
    {
        SkipIfDatabaseUnavailable();
        var (academicYearId, _, _, applicantId, _) = await SetupAcceptedApplicationAsync($"PG-UNCONF-{Guid.NewGuid():N}"[..6]);

        // Unpaid, CheckFinanceModule = true, finance module returns false
        Factory.FinanceModule.Reset();

        var enrollRequest = new EnrollAcceptedApplicantRequest(
            ApplicantId: applicantId,
            AcademicYearId: academicYearId,
            InitialCampus: "Main Campus",
            InitialLevel: "Grade 1",
            InitialAcademicYear: "2026/2027",
            PaymentWaived: false,
            PaymentReference: null,
            CheckFinanceModule: true);

        var enrollResponse = await Client.PostAsJsonAsync("/api/students/enrollments", enrollRequest);
        enrollResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [SkippableFact]
    public async Task PaymentGate_ManualPaymentReference_Should_Succeed()
    {
        SkipIfDatabaseUnavailable();
        var (academicYearId, _, _, applicantId, _) = await SetupAcceptedApplicationAsync($"PG-MAN-{Guid.NewGuid():N}"[..6]);

        var enrollRequest = new EnrollAcceptedApplicantRequest(
            ApplicantId: applicantId,
            AcademicYearId: academicYearId,
            InitialCampus: "Main Campus",
            InitialLevel: "Grade 1",
            InitialAcademicYear: "2026/2027",
            PaymentWaived: false,
            PaymentReference: "BANK-BCA-MANUAL-REF-12345");

        var enrollResponse = await Client.PostAsJsonAsync("/api/students/enrollments", enrollRequest);
        enrollResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await enrollResponse.Content.ReadFromJsonAsync<EnrollAcceptedApplicantResponse>();
        const string checkSql = "SELECT enrollment_payment_confirmed, payment_reference FROM students.enrollments WHERE id = @Id;";
        var row = (await QueryAsync<dynamic>(checkSql, new { Id = body!.EnrollmentId })).Single();
        ((bool)row.enrollment_payment_confirmed).Should().BeTrue();
        ((string)row.payment_reference).Should().Be("BANK-BCA-MANUAL-REF-12345");
    }

    [SkippableFact]
    public async Task PaymentGate_FeeWaiverOverride_WithDecree_Should_Succeed()
    {
        SkipIfDatabaseUnavailable();
        var (academicYearId, _, _, applicantId, _) = await SetupAcceptedApplicationAsync($"PG-WAIVE-{Guid.NewGuid():N}"[..6]);

        var enrollRequest = new EnrollAcceptedApplicantRequest(
            ApplicantId: applicantId,
            AcademicYearId: academicYearId,
            InitialCampus: "Main Campus",
            InitialLevel: "Grade 1",
            InitialAcademicYear: "2026/2027",
            PaymentWaived: true,
            WaiverReference: "SK-YAYS-DECREE-2026-999");

        var enrollResponse = await Client.PostAsJsonAsync("/api/students/enrollments", enrollRequest);
        enrollResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await enrollResponse.Content.ReadFromJsonAsync<EnrollAcceptedApplicantResponse>();
        const string checkSql = "SELECT payment_waived, waiver_reference FROM students.enrollments WHERE id = @Id;";
        var row = (await QueryAsync<dynamic>(checkSql, new { Id = body!.EnrollmentId })).Single();
        ((bool)row.payment_waived).Should().BeTrue();
        ((string)row.waiver_reference).Should().Be("SK-YAYS-DECREE-2026-999");
    }

    [SkippableFact]
    public async Task PaymentGate_SimulatedFinanceModuleSuccess_Should_Succeed()
    {
        SkipIfDatabaseUnavailable();
        var (academicYearId, _, _, applicantId, _) = await SetupAcceptedApplicationAsync($"PG-FIN-{Guid.NewGuid():N}"[..6]);

        // Simulate Finance Module confirming payment
        Factory.FinanceModule.MarkFeePaid(applicantId, academicYearId);

        var enrollRequest = new EnrollAcceptedApplicantRequest(
            ApplicantId: applicantId,
            AcademicYearId: academicYearId,
            InitialCampus: "Main Campus",
            InitialLevel: "Grade 1",
            InitialAcademicYear: "2026/2027",
            PaymentWaived: false,
            PaymentReference: null,
            CheckFinanceModule: true);

        var enrollResponse = await Client.PostAsJsonAsync("/api/students/enrollments", enrollRequest);
        enrollResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await enrollResponse.Content.ReadFromJsonAsync<EnrollAcceptedApplicantResponse>();
        const string checkSql = "SELECT enrollment_payment_confirmed FROM students.enrollments WHERE id = @Id;";
        var row = (await QueryAsync<dynamic>(checkSql, new { Id = body!.EnrollmentId })).Single();
        ((bool)row.enrollment_payment_confirmed).Should().BeTrue();
    }
}
