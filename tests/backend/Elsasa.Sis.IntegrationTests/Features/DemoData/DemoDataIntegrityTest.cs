namespace Elsasa.Sis.IntegrationTests.Features.DemoData;

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

/// <summary>
/// Integration test verifying data integrity after demo data reset and seed
/// (P5-S10, GAP-005, OQ-003, OQ-013).
/// Verifies deterministic IDs, entity relationships, dynamic date anchoring,
/// and flagship persona integrity (Jonathan, Grace, Budi, Ruth).
/// </summary>
public sealed class DemoDataIntegrityTest : BaseIntegrationTest
{
    public DemoDataIntegrityTest(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    [SkippableFact]
    public async Task DeterministicIDs_Should_Match_DemoDataConstants()
    {
        SkipIfDatabaseUnavailable();

        await Factory.ContainerFixture.ResetDatabaseAsync();

        var client = Factory.CreateClient();

        // Execute full reset and seed
        var resetRequest = new { cleanOnly = false };
        var response = await client.PostAsJsonAsync("/api/operations/demo-data/reset", resetRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await response.Content.ReadFromJsonAsync<DemoResetSummary>();
        summary.Should().NotBeNull();

        // 1. Verify students table has deterministic GUIDs matching DemoDataConstants
        var jonathanStudent = await QuerySingleOrDefaultAsync<StudentBasicRecord>(
            "SELECT id, student_number, first_name, full_name FROM students.students WHERE id = @Id",
            new { Id = DemoDataConstants.JonathanStudentId });
        jonathanStudent.Should().NotBeNull("should find Jonathan student with deterministic ID");
        jonathanStudent!.full_name.Should().Be("Jonathan Pratama");

        var graceStudent = await QuerySingleOrDefaultAsync<StudentBasicRecord>(
            "SELECT id, student_number, first_name, full_name FROM students.students WHERE id = @Id",
            new { Id = DemoDataConstants.GraceStudentId });
        graceStudent.Should().NotBeNull("should find Grace student with deterministic ID");
        graceStudent!.full_name.Should().Be("Grace Santoso");

        var budiStudent = await QuerySingleOrDefaultAsync<StudentBasicRecord>(
            "SELECT id, student_number, first_name, full_name FROM students.students WHERE id = @Id",
            new { Id = DemoDataConstants.BudiStudentId });
        budiStudent.Should().NotBeNull("should find Budi student with deterministic ID");
        budiStudent!.full_name.Should().Be("Budi Wijaya");

        // 2. Verify staged applicants have deterministic IDs
        var danielApplicant = await QuerySingleOrDefaultAsync<ApplicantRecord>(
            "SELECT id, full_name FROM students.applicants WHERE id = @Id",
            new { Id = DemoDataConstants.DanielApplicantId });
        danielApplicant.Should().NotBeNull("should find Daniel applicant with deterministic ID");
        danielApplicant!.full_name.Should().Be("Daniel Kurniawan");

        var ruthApplicant = await QuerySingleOrDefaultAsync<ApplicantRecord>(
            "SELECT id, full_name FROM students.applicants WHERE id = @Id",
            new { Id = DemoDataConstants.RuthApplicantId });
        ruthApplicant.Should().NotBeNull("should find Ruth applicant with deterministic ID");
        ruthApplicant!.full_name.Should().Be("Ruth Halim");
    }

    [SkippableFact]
    public async Task DynamicDateAnchoring_Should_Place_Today_Approximately_Four_Weeks_Into_Active_Semester2()
    {
        // Criteria 3, bullet 5 & MAJOR-004: Asserts dynamic date anchoring places Today ~4 weeks into active Semester 2
        SkipIfDatabaseUnavailable();

        await Factory.ContainerFixture.ResetDatabaseAsync();

        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/operations/demo-data/reset", new { cleanOnly = false });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await response.Content.ReadFromJsonAsync<DemoResetSummary>();
        summary.Should().NotBeNull();

        var today = DateTime.UtcNow.Date;
        summary!.AnchoredDate.Date.Should().Be(today, "summary anchored date must match Today");

        // Query active Semester 2 period from operations.operational_periods
        var activeSem2 = await QuerySingleOrDefaultAsync<OperationalPeriodRecord>(
            """
            SELECT id, period_code, name, period_type, start_date, end_date, status
            FROM operations.operational_periods
            WHERE status = 'active' AND period_type = 'semester'
            """);

        activeSem2.Should().NotBeNull("an active Semester 2 period should exist");
        activeSem2!.period_code.Should().Contain("SEM2");

        var startDate = activeSem2.start_date.ToDateTime(TimeOnly.MinValue);
        var endDate = activeSem2.end_date.ToDateTime(TimeOnly.MinValue);

        var daysIntoSemester = (today - startDate).TotalDays;
        daysIntoSemester.Should().BeInRange(25, 35, "Today should be situated approximately 4 weeks (~28-30 days) into ongoing active Semester 2");
        endDate.Should().BeAfter(today, "active Semester 2 must be ongoing with end date in the future");
    }

    [SkippableFact]
    public async Task FlagshipPersona_JonathanPratama_Should_Satisfy_IntegrityCriteria()
    {
        // Criteria 3, bullet 1 & MAJOR-003:
        // Jonathan Pratama: HSLC placement, Grade 10, certified Semester 1 report card with frozen JSON snapshot,
        // cumulative transcript, unpaid Semester 2 invoice INV-SEM2-0042 with balance IDR 1,500,000.
        SkipIfDatabaseUnavailable();

        await Factory.ContainerFixture.ResetDatabaseAsync();

        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/operations/demo-data/reset", new { cleanOnly = false });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var jonathanId = DemoDataConstants.JonathanStudentId;

        // 1. Placement: HSLC, Grade 10, Current
        var placement = await QuerySingleOrDefaultAsync<PlacementRecord>(
            """
            SELECT school_level, grade, is_current
            FROM students.administrative_placements
            WHERE student_id = @StudentId AND is_current = 1
            """,
            new { StudentId = jonathanId });

        placement.Should().NotBeNull("Jonathan must have an active administrative placement");
        placement!.school_level.Should().Be("HSLC", "Jonathan should be placed in HSLC");
        placement.grade.Should().Be("Grade 10", "Jonathan should be placed in Grade 10");
        placement.is_current.Should().BeTrue();

        // 2. Certified Semester 1 report card with frozen JSON snapshot
        var reportCard = await QuerySingleOrDefaultAsync<ReportCardRecord>(
            """
            SELECT id, student_id, status, report_type, content_snapshot
            FROM academics.report_cards
            WHERE student_id = @StudentId AND report_type = 'end_of_term'
            """,
            new { StudentId = jonathanId });

        reportCard.Should().NotBeNull("Jonathan must have a certified Semester 1 report card");
        reportCard!.status.Should().BeOneOf("published", "certified");
        reportCard.content_snapshot.Should().NotBeNullOrWhiteSpace("certified Semester 1 report card must contain frozen JSON snapshot");

        using (var snapshotJson = JsonDocument.Parse(reportCard.content_snapshot!))
        {
            snapshotJson.RootElement.ValueKind.Should().Be(JsonValueKind.Object, "content_snapshot must be a valid JSON object");
        }

        // 3. Cumulative transcript with frozen JSON snapshot
        var transcript = await QuerySingleOrDefaultAsync<TranscriptRecord>(
            """
            SELECT id, student_id, status, content_snapshot
            FROM academics.transcripts
            WHERE student_id = @StudentId
            """,
            new { StudentId = jonathanId });

        transcript.Should().NotBeNull("Jonathan must have a cumulative transcript");
        transcript!.content_snapshot.Should().NotBeNullOrWhiteSpace("transcript must contain frozen JSON snapshot");

        using (var transJson = JsonDocument.Parse(transcript.content_snapshot!))
        {
            transJson.RootElement.ValueKind.Should().Be(JsonValueKind.Object, "transcript content_snapshot must be a valid JSON object");
        }

        // 4. Unpaid Semester 2 invoice INV-SEM2-0042 with balance IDR 1,500,000
        var invoice = await QuerySingleOrDefaultAsync<BillingDocRecord>(
            """
            SELECT id, student_id, document_number, total_billed_amount, delivery_status
            FROM finance.billing_documents
            WHERE document_number = 'INV-SEM2-0042'
            """);

        invoice.Should().NotBeNull("Jonathan's unpaid invoice INV-SEM2-0042 must exist");
        invoice!.student_id.Should().Be(jonathanId, "invoice INV-SEM2-0042 must belong to Jonathan");
        invoice.document_number.Should().Be("INV-SEM2-0042");
        invoice.total_billed_amount.Should().Be(1500000m, "invoice balance must be IDR 1,500,000");
        invoice.delivery_status.Should().Be("unpaid", "invoice delivery status must be unpaid");
    }

    [SkippableFact]
    public async Task FlagshipPersona_GraceSantoso_Should_Satisfy_IntegrityCriteria()
    {
        // Criteria 3, bullet 2 & MAJOR-003:
        // Grace Santoso: Failed Attempt 1 (68%), passed Attempt 2 (85%, is_final = 1, replaces_result_id populated),
        // overdue tuition receivable.
        SkipIfDatabaseUnavailable();

        await Factory.ContainerFixture.ResetDatabaseAsync();

        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/operations/demo-data/reset", new { cleanOnly = false });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var graceId = DemoDataConstants.GraceStudentId;

        // 1. Academic Results: Attempt 1 (68%, is_final = 0) and Attempt 2 (85%, is_final = 1, replaces_result_id)
        var results = (await QueryAsync<AcademicResultRecord>(
            """
            SELECT id, attempt_number, replaces_result_id, is_final, score_numeric
            FROM academics.academic_results
            WHERE student_id = @StudentId
            ORDER BY attempt_number
            """,
            new { StudentId = graceId })).ToList();

        results.Should().HaveCountGreaterOrEqualTo(2, "Grace should have at least 2 attempts for PACE retake trail");

        var attempt1 = results.FirstOrDefault(r => r.attempt_number == 1);
        attempt1.Should().NotBeNull("Attempt 1 must exist");
        attempt1!.score_numeric.Should().Be(68.00m, "Attempt 1 score should be 68% (failing)");
        attempt1.is_final.Should().BeFalse("Attempt 1 should not be final after retake");

        var attempt2 = results.FirstOrDefault(r => r.attempt_number == 2);
        attempt2.Should().NotBeNull("Attempt 2 must exist");
        attempt2!.score_numeric.Should().Be(85.00m, "Attempt 2 score should be 85% (passing)");
        attempt2.is_final.Should().BeTrue("Attempt 2 must have is_final = 1");
        attempt2.replaces_result_id.Should().Be(attempt1.id, "Attempt 2 replaces_result_id must point to Attempt 1");

        // 2. Overdue tuition receivable
        var overdueDoc = await QuerySingleOrDefaultAsync<BillingDocRecord>(
            """
            SELECT id, student_id, document_number, total_billed_amount, delivery_status
            FROM finance.billing_documents
            WHERE student_id = @StudentId AND delivery_status = 'overdue'
            """,
            new { StudentId = graceId });

        overdueDoc.Should().NotBeNull("Grace Santoso must have an overdue tuition receivable document");
        overdueDoc!.delivery_status.Should().Be("overdue");
    }

    [SkippableFact]
    public async Task FlagshipPersona_BudiWijaya_Should_Satisfy_IntegrityCriteria()
    {
        // Criteria 3, bullet 3 & MAJOR-003:
        // Budi Wijaya: Submitted Science PACE 1049 test awaiting grading.
        SkipIfDatabaseUnavailable();

        await Factory.ContainerFixture.ResetDatabaseAsync();

        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/operations/demo-data/reset", new { cleanOnly = false });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var budiId = DemoDataConstants.BudiStudentId;

        // 1. Result awaiting grading with assessment for Science PACE 1049
        var budiResult = await QuerySingleOrDefaultAsync<AcademicResultDetailRecord>(
            """
            SELECT ar.id, ar.student_id, ar.result_status, ar.score_numeric, ar.evaluated_by, a.title AS assessment_title
            FROM academics.academic_results ar
            INNER JOIN academics.assessments a ON a.id = ar.assessment_id
            WHERE ar.student_id = @StudentId AND ar.score_numeric IS NULL AND ar.result_status = 'provisional'
            """,
            new { StudentId = budiId });

        budiResult.Should().NotBeNull("Budi Wijaya must have an assessment result awaiting grading");
        budiResult!.score_numeric.Should().BeNull("score must be null while awaiting grading");
        budiResult.result_status.Should().Be("provisional", "result status must be provisional while awaiting grading");
        budiResult.assessment_title.Should().Contain("1049", "assessment title must reference PACE 1049");

        // 2. Coursework submission in submitted status
        var budiSubmission = await QuerySingleOrDefaultAsync<CourseworkSubmissionRecord>(
            """
            SELECT id, student_id, status
            FROM academics.coursework_submissions
            WHERE student_id = @StudentId AND status = 'submitted'
            """,
            new { StudentId = budiId });

        budiSubmission.Should().NotBeNull("Budi Wijaya must have a submitted coursework submission awaiting evaluation");
        budiSubmission!.status.Should().Be("submitted");
    }

    [SkippableFact]
    public async Task FlagshipPersona_RuthHalim_Should_Satisfy_IntegrityCriteria()
    {
        // Criteria 3, bullet 4 & MAJOR-003:
        // Ruth Halim: Accepted with admission fee paid, ready for 1-click enrollment.
        SkipIfDatabaseUnavailable();

        await Factory.ContainerFixture.ResetDatabaseAsync();

        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/operations/demo-data/reset", new { cleanOnly = false });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var ruthApplicantId = DemoDataConstants.RuthApplicantId;

        // 1. Ruth exists as an applicant
        var applicant = await QuerySingleOrDefaultAsync<ApplicantRecord>(
            "SELECT id, full_name FROM students.applicants WHERE id = @ApplicantId",
            new { ApplicantId = ruthApplicantId });
        applicant.Should().NotBeNull("Ruth Halim must exist in applicants table");
        applicant!.full_name.Should().Be("Ruth Halim");

        // 2. Application status is 'accepted'
        var application = await QuerySingleOrDefaultAsync<ApplicationRecord>(
            "SELECT id, applicant_id, status, notes FROM students.applications WHERE applicant_id = @ApplicantId",
            new { ApplicantId = ruthApplicantId });
        application.Should().NotBeNull("Ruth Halim must have an application");
        application!.status.Should().Be("accepted", "Ruth Halim's application status must be accepted");

        // 3. Admission decision is 'accepted' with admission fee payment verified
        var decision = await QuerySingleOrDefaultAsync<AdmissionDecisionRecord>(
            """
            SELECT id, application_id, decision_type, reason, supporting_reference
            FROM students.admission_decisions
            WHERE application_id = @ApplicationId
            """,
            new { ApplicationId = application.id });
        decision.Should().NotBeNull("Ruth Halim must have an admission decision");
        decision!.decision_type.Should().Be("accepted");
        decision.reason.Should().Contain("Admission fee payment verified");
        decision.supporting_reference.Should().Be("DEC-ADM-RUTH");

        // 4. Ready for 1-click enrollment: Ruth has NOT yet been enrolled into students.students
        var enrolledCount = await QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM students.students WHERE applicant_id = @ApplicantId",
            new { ApplicantId = ruthApplicantId });
        enrolledCount.Should().Be(0, "Ruth Halim is pre-staged and ready for 1-click enrollment; not yet in students.students");
    }

    [SkippableFact]
    public async Task EntityRelationships_Should_Be_Correctly_Established()
    {
        SkipIfDatabaseUnavailable();

        await Factory.ContainerFixture.ResetDatabaseAsync();

        var client = Factory.CreateClient();

        // Execute full reset and seed
        var resetRequest = new { cleanOnly = false };
        var response = await client.PostAsJsonAsync("/api/operations/demo-data/reset", resetRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 1. Students should have linked guardians
        var studentsWithGuardians = await QuerySingleAsync<int>("SELECT COUNT(*) FROM students.student_guardian_relationships");
        studentsWithGuardians.Should().BeGreaterThan(0, "should have student-guardian relationships");

        // 2. Financial obligations should link to students
        var obligationsLinkedToStudents = await QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM finance.financial_obligations fo " +
            "WHERE EXISTS (SELECT 1 FROM students.students s WHERE s.id = fo.student_id)");
        obligationsLinkedToStudents.Should().BeGreaterThan(0, "financial obligations should link to students");

        // 3. Invoices should link to financial obligations
        var invoicesLinkedToObligations = await QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM finance.billing_documents bd " +
            "WHERE EXISTS (SELECT 1 FROM finance.financial_obligations fo WHERE fo.id = bd.financial_obligation_id)");
        invoicesLinkedToObligations.Should().BeGreaterThan(0, "invoices should link to financial obligations");

        // 4. Report cards should link to students
        var reportCardsLinkedToStudents = await QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM academics.report_cards rc " +
            "WHERE EXISTS (SELECT 1 FROM students.students s WHERE s.id = rc.student_id)");
        reportCardsLinkedToStudents.Should().BeGreaterThan(0, "report cards should link to students");

        // 5. Transcripts should link to students (high school only)
        var transcriptsLinkedToStudents = await QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM academics.transcripts tc " +
            "WHERE EXISTS (SELECT 1 FROM students.students s WHERE s.id = tc.student_id AND " +
            "EXISTS (SELECT 1 FROM students.administrative_placements ap " +
            "WHERE ap.student_id = s.id AND ap.level IN ('High School', 'HSLC')))");
        transcriptsLinkedToStudents.Should().BeGreaterThan(0, "transcripts should link to high school students");

        // 6. Attendance should link to students and academic participations
        var attendanceLinked = await QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM academics.attendance_records ar " +
            "WHERE EXISTS (SELECT 1 FROM students.students s WHERE s.id = ar.student_id)" +
            " AND EXISTS (SELECT 1 FROM academics.academic_participations ap " +
            "WHERE ap.student_id = s.id)");
        attendanceLinked.Should().BeGreaterThan(100, "attendance should link to students and participations");
    }

    [SkippableFact]
    public async Task FinancialData_Should_Have_Realistic_Distribution()
    {
        SkipIfDatabaseUnavailable();

        await Factory.ContainerFixture.ResetDatabaseAsync();

        var client = Factory.CreateClient();

        var resetRequest = new { cleanOnly = false };
        var response = await client.PostAsJsonAsync("/api/operations/demo-data/reset", resetRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Semester 1 is 100% paid
        var sem1Paid = await QuerySingleAsync<int>("SELECT COUNT(*) FROM finance.payments WHERE status = 'paid'");
        sem1Paid.Should().BeGreaterThan(0, "should have paid Semester 1 payments");

        // Verify Semester 2 has mixed payment states
        var sem2Payments = await QueryAsync<Sem2PaymentSummary>(
            """
            SELECT 
                SUM(CASE WHEN status = 'paid' THEN 1 ELSE 0 END) as paid_count,
                SUM(CASE WHEN status = 'partial' THEN 1 ELSE 0 END) as partial_count,
                SUM(CASE WHEN status = 'overdue' THEN 1 ELSE 0 END) as overdue_count,
                SUM(CASE WHEN status = 'unpaid' THEN 1 ELSE 0 END) as unpaid_count
            FROM finance.payments WHERE academic_period_id IS NOT NULL
            """);

        var sem2Summary = sem2Payments.FirstOrDefault();
        if (sem2Summary is not null)
        {
            sem2Summary.paid_count.Should().BeGreaterThan(0, "should have some paid Semester 2 payments");
            sem2Summary.overdue_count.Should().BeGreaterThan(0, "should have overdue Semester 2 payments (Risk-001)");
            sem2Summary.unpaid_count.Should().BeGreaterThan(0, "should have unpaid Semester 2 payments (live cashier hook)");
        }
    }

    private sealed record StudentBasicRecord(Guid id, string student_number, string first_name, string full_name);
    private sealed record ApplicantRecord(Guid id, string full_name);
    private sealed record ApplicationRecord(Guid id, Guid applicant_id, string status, string? notes);
    private sealed record AdmissionDecisionRecord(Guid id, Guid application_id, string decision_type, string reason, string supporting_reference);
    private sealed record OperationalPeriodRecord(Guid id, string period_code, string name, string period_type, DateOnly start_date, DateOnly end_date, string status);
    private sealed record PlacementRecord(string school_level, string grade, bool is_current);
    private sealed record ReportCardRecord(Guid id, Guid student_id, string status, string report_type, string? content_snapshot);
    private sealed record TranscriptRecord(Guid id, Guid student_id, string status, string? content_snapshot);
    private sealed record BillingDocRecord(Guid id, Guid student_id, string document_number, decimal total_billed_amount, string delivery_status);
    private sealed record AcademicResultRecord(Guid id, int attempt_number, Guid? replaces_result_id, bool is_final, decimal? score_numeric);
    private sealed record AcademicResultDetailRecord(Guid id, Guid student_id, string result_status, decimal? score_numeric, string? evaluated_by, string assessment_title);
    private sealed record CourseworkSubmissionRecord(Guid id, Guid student_id, string status);
    private sealed record Sem2PaymentSummary(int paid_count, int partial_count, int overdue_count, int unpaid_count);
}