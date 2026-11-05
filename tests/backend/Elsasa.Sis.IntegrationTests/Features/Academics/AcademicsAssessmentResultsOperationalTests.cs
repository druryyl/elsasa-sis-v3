namespace Elsasa.Sis.IntegrationTests.Features.Academics;

using System.Net;
using System.Net.Http.Json;
using Dapper;
using Elsasa.Sis.Academics.Features.AcademicResults;
using Elsasa.Sis.Academics.Features.AcademicResults.ApproveSummativeResults;
using Elsasa.Sis.Academics.Features.AcademicResults.EvaluateStudentLearning;
using Elsasa.Sis.Academics.Features.AcademicResults.PublishAcademicResults;
using Elsasa.Sis.Academics.Features.AssessmentEvidence;
using Elsasa.Sis.Academics.Features.AssessmentEvidence.RecordEvidence;
using Elsasa.Sis.Academics.Features.Assessments.CreateAssessment;
using Elsasa.Sis.Academics.Features.RecordCorrections.CorrectApprovedResult;
using Elsasa.Sis.Academics.Features.ReportCards;
using Elsasa.Sis.Academics.Features.ReportCards.ApproveReportCard;
using Elsasa.Sis.Academics.Features.ReportCards.BatchCompileCohortReportCards;
using Elsasa.Sis.Academics.Features.Transcripts;
using Elsasa.Sis.Academics.Features.Transcripts.GenerateInterimTranscript;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Operational end-to-end integration test suite verifying Submodule 3 Assessment &amp; Results workflows (ADR-003, P7-S18).
/// Verifies the complete lifecycle: assessment creation, evidence capture, multi-attempt retake evaluation with capped policy,
/// summative certification and publication, batch cohort report card compilation, frozen snapshot persistence,
/// auditable grade corrections, applicant entrance diagnostics cross-domain queries, and transcript issuance.
/// </summary>
public sealed class AcademicsAssessmentResultsOperationalTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    private IAcademicsModule CreateAcademicsModule()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IAcademicsModule>();
    }

    private async Task<(Guid PeriodId, Guid UnitId)> SetupActiveCurriculumUnitAsync(string testSuffix)
    {
        await using var connection = await OpenConnectionAsync();
        var (_, periodId) = await AcademicsIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, testSuffix);

        var structure = await AcademicsIntegrationTestHelpers.CreateDraftStructureAsync(
            Client,
            periodId,
            name: $"Curriculum Framework {testSuffix}",
            version: "1.0");

        var unit = await AcademicsIntegrationTestHelpers.CreateRootUnitAsync(
            Client,
            structure.Id,
            code: $"U-{testSuffix[..Math.Min(testSuffix.Length, 8)]}",
            name: $"Operational Unit {testSuffix}",
            unitType: "course",
            creditWeight: 4.0m);

        var approveResponse = await Client.PostAsync($"/api/v1/academics/learning-structures/{structure.Id}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        return (periodId, unit.Id);
    }

    [SkippableFact]
    public async Task Criterion01_CompleteOperationalWorkflow_Should_SucceedAcrossEntireLifecycle()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (periodId, unitId) = await SetupActiveCurriculumUnitAsync("OP-E2E");

        var studentId = Guid.NewGuid();
        Factory.StudentsModule.MarkStudentEnrolled(studentId);

        await using var connection = await OpenConnectionAsync();

        // Seed student into students.students table for profile snapshot inclusion
        const string insertStudentSql = """
            IF NOT EXISTS (SELECT 1 FROM students.students WHERE id = @StudentId)
            BEGIN
                INSERT INTO students.students (
                    id, student_number, full_name, government_id_number, date_of_birth,
                    enrollment_status, created_at, created_by
                ) VALUES (
                    @StudentId, 'STD-2026-OP01', 'Budi Santoso', 'NISN-99887766', '2010-05-15',
                    'active', SYSUTCDATETIME(), 'TEST_SETUP'
                );
            END;
            """;
        await connection.ExecuteAsync(insertStudentSql, new { StudentId = studentId });

        // Seed active academic participation for cohort section '10-A'
        const string insertParticipationSql = """
            INSERT INTO academics.academic_participations (
                id, student_id, learning_unit_id, academic_period_id, section_label,
                status, start_date, created_at, created_by
            ) VALUES (
                NEWID(), @StudentId, @UnitId, @PeriodId, '10-A',
                'in_progress', '2026-08-15', SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;
        await connection.ExecuteAsync(insertParticipationSql, new
        {
            StudentId = studentId,
            UnitId = unitId,
            PeriodId = periodId
        });

        // 1. Create assessment tied to learning unit (POST /api/v1/academics/assessments)
        var createAssessmentRequest = new CreateAssessmentRequest(
            LearningUnitId: unitId,
            LearningActivityId: null,
            Title: "Midterm Mathematics Examination",
            AssessmentType: "summative",
            AssessmentMethod: "written_test",
            MaxScore: 100m,
            PassingScore: 75m,
            ReassessmentPolicy: "capped_at_passing",
            Criteria: "Algebra and Trigonometry Rubric",
            AssessmentDate: new DateOnly(2026, 10, 15),
            AssessorReference: "Assessor-Teacher-01",
            Notes: "Periodic midterm summative assessment");

        var createAssessmentResponse = await Client.PostAsJsonAsync("/api/v1/academics/assessments", createAssessmentRequest);
        createAssessmentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdAssessment = await createAssessmentResponse.Content.ReadFromJsonAsync<CreateAssessmentResponse>();
        createdAssessment.Should().NotBeNull();
        var assessmentId = createdAssessment!.Id;

        // 2. Record evidence for student (POST /api/v1/academics/assessments/{id}/evidence)
        var recordEvidenceRequest = new RecordEvidenceRequest(
            StudentId: studentId,
            ApplicantId: null,
            SubmissionId: null,
            EvidenceType: "test_paper",
            CollectedDate: new DateOnly(2026, 10, 15),
            EvidenceReference: "evidence_exam_paper_attempt1.pdf",
            Notes: "Physical exam paper filed in archive");

        var evidenceResponse = await Client.PostAsJsonAsync($"/api/v1/academics/assessments/{assessmentId}/evidence", recordEvidenceRequest);
        evidenceResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var evidenceDto = await evidenceResponse.Content.ReadFromJsonAsync<AssessmentEvidenceDto>();
        evidenceDto.Should().NotBeNull();
        evidenceDto!.StudentId.Should().Be(studentId);
        evidenceDto.EvidenceType.Should().Be("test_paper");

        // 3. Teacher records initial evaluation attempt 1 (score = 65, passing_score = 75)
        // Expected: is_final = 1, attempt_number = 1, result_status = 'provisional'
        var attempt1Request = new EvaluateStudentLearningRequest(
            StudentId: studentId,
            ApplicantId: null,
            ScoreNumeric: 65m,
            ScoreMax: 100m,
            GradeLetter: "C",
            GradeLabel: "Cukup",
            NarrativeFeedback: "Needs remediation on algebraic functions",
            EvaluationModel: "numeric",
            PublishImmediately: false,
            Notes: "Initial summative attempt");

        var attempt1Response = await Client.PostAsJsonAsync($"/api/v1/academics/assessments/{assessmentId}/results", attempt1Request);
        attempt1Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var attempt1Dto = await attempt1Response.Content.ReadFromJsonAsync<AcademicResultDto>();
        attempt1Dto.Should().NotBeNull();
        attempt1Dto!.AttemptNumber.Should().Be(1);
        attempt1Dto.IsFinal.Should().BeTrue();
        attempt1Dto.ResultStatus.Should().Be("provisional");
        attempt1Dto.ScoreNumeric.Should().Be(65m);
        attempt1Dto.ReplacesResultId.Should().BeNull();

        // 4. Student retakes: teacher records attempt 2 (score = 85) with capped_at_passing policy
        // Expected: attempt 2 inserted (attempt_number = 2, replaces_result_id = attempt1.Id),
        // is_final = 1, operative score capped at 75, attempt 1 updated to is_final = 0
        var attempt2Request = new EvaluateStudentLearningRequest(
            StudentId: studentId,
            ApplicantId: null,
            ScoreNumeric: 85m,
            ScoreMax: 100m,
            GradeLetter: "B",
            GradeLabel: "Kompeten",
            NarrativeFeedback: "Remediation completed successfully",
            EvaluationModel: "numeric",
            PublishImmediately: false,
            Notes: "Retake attempt 2");

        var attempt2Response = await Client.PostAsJsonAsync($"/api/v1/academics/assessments/{assessmentId}/results", attempt2Request);
        attempt2Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var attempt2Dto = await attempt2Response.Content.ReadFromJsonAsync<AcademicResultDto>();
        attempt2Dto.Should().NotBeNull();
        attempt2Dto!.AttemptNumber.Should().Be(2);
        attempt2Dto.ReplacesResultId.Should().Be(attempt1Dto.Id);
        attempt2Dto.IsFinal.Should().BeTrue();
        attempt2Dto.ScoreNumeric.Should().Be(75m); // Capped at passing_score (75)

        // Verify attempt 1 was atomically demoted to is_final = 0
        var attempt1IsFinalDb = await connection.ExecuteScalarAsync<int>(
            "SELECT is_final FROM academics.academic_results WHERE id = @Id",
            new { attempt1Dto.Id });
        attempt1IsFinalDb.Should().Be(0);

        // 5. Authority approves summative results (POST /api/v1/academics/assessments/{id}/results/approve)
        // Expected: result_status = 'approved', AcademicResultApprovedEvent captured in TestEventCollector
        Factory.EventCollector.Reset();
        var approveResultsRequest = new ApproveSummativeResultsRequest([attempt2Dto.Id]);
        var approveResultsResponse = await Client.PostAsJsonAsync(
            $"/api/v1/academics/assessments/{assessmentId}/results/approve",
            approveResultsRequest);
        approveResultsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var attempt2StatusDb = await connection.ExecuteScalarAsync<string>(
            "SELECT result_status FROM academics.academic_results WHERE id = @Id",
            new { attempt2Dto.Id });
        attempt2StatusDb.Should().Be("approved");

        Factory.EventCollector.AcademicResultApprovedEvents
            .Should().Contain(e => e.ResultId == attempt2Dto.Id && e.AssessmentId == assessmentId && e.ScoreNumeric == 75m);

        // 6. Authority publishes results -> AcademicResultPublishedEvent emitted
        var publishResultsRequest = new PublishAcademicResultsRequest([attempt2Dto.Id]);
        var publishResultsResponse = await Client.PostAsJsonAsync(
            $"/api/v1/academics/assessments/{assessmentId}/results/publish",
            publishResultsRequest);
        publishResultsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        Factory.EventCollector.AcademicResultPublishedEvents
            .Should().Contain(e => e.ResultId == attempt2Dto.Id && e.AssessmentId == assessmentId && e.ScoreNumeric == 75m);

        var attempt2PublishedDb = await connection.ExecuteScalarAsync<string>(
            "SELECT result_status FROM academics.academic_results WHERE id = @Id",
            new { attempt2Dto.Id });
        attempt2PublishedDb.Should().Be("published");

        // 7. Coordinator batch-compiles report cards for cohort -> draft report cards and entries created referencing approved result
        var batchCompileRequest = new BatchCompileCohortReportCardsRequest(
            AcademicPeriodId: periodId,
            AcademicPeriodLabel: "2026-Fall",
            ReportType: "mid_term",
            TemplateName: "Buku Rapor Merdeka",
            SectionLabel: "10-A");

        var batchCompileResponse = await Client.PostAsJsonAsync(
            "/api/v1/academics/report-cards/batch-compile",
            batchCompileRequest);
        batchCompileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var batchSummary = await batchCompileResponse.Content.ReadFromJsonAsync<BatchCompilationSummaryDto>();
        batchSummary.Should().NotBeNull();
        batchSummary!.CompiledCount.Should().BeGreaterOrEqualTo(1);

        var reportCardId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT id FROM academics.report_cards WHERE student_id = @StudentId AND academic_period_id = @PeriodId AND report_type = 'mid_term'",
            new { StudentId = studentId, PeriodId = periodId });
        reportCardId.Should().NotBeEmpty();

        var entryResultId = await connection.ExecuteScalarAsync<Guid?>(
            "SELECT academic_result_id FROM academics.report_card_entries WHERE report_card_id = @ReportCardId AND learning_unit_id = @UnitId",
            new { ReportCardId = reportCardId, UnitId = unitId });
        entryResultId.Should().Be(attempt2Dto.Id);

        // 8. Authority approves report card -> content_snapshot JSON is populated, ReportCardPublishedEvent emitted on publish
        var approveCardRequest = new ApproveReportCardRequest("Menunjukkan pemahaman konsep matematika yang baik.");
        var approveCardResponse = await Client.PostAsJsonAsync(
            $"/api/v1/academics/report-cards/{reportCardId}/approve",
            approveCardRequest);
        approveCardResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var approvedCardDto = await approveCardResponse.Content.ReadFromJsonAsync<ReportCardDto>();
        approvedCardDto.Should().NotBeNull();
        approvedCardDto!.Status.Should().Be("approved");
        approvedCardDto.ContentSnapshot.Should().NotBeNullOrWhiteSpace();

        var snapshotJsonDb = await connection.ExecuteScalarAsync<string>(
            "SELECT content_snapshot FROM academics.report_cards WHERE id = @Id",
            new { Id = reportCardId });
        snapshotJsonDb.Should().NotBeNullOrWhiteSpace();
        snapshotJsonDb.Should().Contain("Budi Santoso");
        snapshotJsonDb.Should().Contain("NISN-99887766");

        // Publish report card -> emits ReportCardPublishedEvent
        var publishCardResponse = await Client.PostAsync(
            $"/api/v1/academics/report-cards/{reportCardId}/publish",
            null);
        publishCardResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        Factory.EventCollector.ReportCardPublishedEvents
            .Should().Contain(e => e.ReportCardId == reportCardId && e.StudentId == studentId && e.ReportType == "mid_term");

        // 9. Auditable correction: Authority executes POST /api/v1/academics/academic-results/{id}/correct
        // with reason "Recalculated essay rubric" -> original_value and corrected_value logged in academics.academic_record_corrections,
        // result updated, AcademicResultCorrectedEvent captured
        var correctRequest = new CorrectApprovedResultRequest(
            ScoreNumeric: 78m,
            Reason: "Recalculated essay rubric",
            IsMaterial: true);

        var correctResponse = await Client.PostAsJsonAsync(
            $"/api/v1/academics/academic-results/{attempt2Dto.Id}/correct",
            correctRequest);
        correctResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var correctedDto = await correctResponse.Content.ReadFromJsonAsync<AcademicResultDto>();
        correctedDto.Should().NotBeNull();
        correctedDto!.ScoreNumeric.Should().Be(78m);

        var correctionDb = await connection.QuerySingleAsync<(string OriginalValue, string CorrectedValue, string Reason, bool IsMaterial)>(
            """
            SELECT
                original_value AS OriginalValue,
                corrected_value AS CorrectedValue,
                reason AS Reason,
                is_material AS IsMaterial
            FROM academics.academic_record_corrections
            WHERE academic_result_id = @ResultId;
            """,
            new { ResultId = attempt2Dto.Id });

        correctionDb.OriginalValue.Should().Contain("75");
        correctionDb.CorrectedValue.Should().Contain("78");
        correctionDb.Reason.Should().Be("Recalculated essay rubric");
        correctionDb.IsMaterial.Should().BeTrue();

        Factory.EventCollector.AcademicResultCorrectedEvents
            .Should().Contain(e => e.ResultId == attempt2Dto.Id && e.Reason == "Recalculated essay rubric" && e.IsMaterial);
    }

    [SkippableFact]
    public async Task Criterion02_ApplicantEntranceAssessment_Should_StoreScore_And_ReturnViaAcademicsModule()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (periodId, unitId) = await SetupActiveCurriculumUnitAsync("APP-ENTRANCE");

        var applicantId = Guid.NewGuid();

        await using var connection = await OpenConnectionAsync();

        // Seed applicant in students.applicants table
        const string insertApplicantSql = """
            IF NOT EXISTS (SELECT 1 FROM students.applicants WHERE id = @ApplicantId)
            BEGIN
                INSERT INTO students.applicants (
                    id, application_number, full_name, email, date_of_birth,
                    status, created_at, created_by
                ) VALUES (
                    @ApplicantId, 'APP-2026-DIAG01', 'Nur Hidayah', 'nur.hidayah@example.com', '2012-04-18',
                    'pending_assessment', SYSUTCDATETIME(), 'TEST_SETUP'
                );
            END;
            """;
        await connection.ExecuteAsync(insertApplicantSql, new { ApplicantId = applicantId });

        // 1. Create entrance assessment
        var assessmentRequest = new CreateAssessmentRequest(
            LearningUnitId: unitId,
            LearningActivityId: null,
            Title: "Admissions Diagnostic Mathematics Exam",
            AssessmentType: "summative",
            AssessmentMethod: "written_test",
            MaxScore: 100m,
            PassingScore: 70m,
            ReassessmentPolicy: "none",
            Criteria: "Admissions diagnostic scoring standards",
            AssessmentDate: new DateOnly(2026, 8, 1),
            AssessorReference: "Admissions-Panel-01",
            Notes: "Entrance diagnostic for placement");

        var createRes = await Client.PostAsJsonAsync("/api/v1/academics/assessments", assessmentRequest);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdAssessment = await createRes.Content.ReadFromJsonAsync<CreateAssessmentResponse>();
        createdAssessment.Should().NotBeNull();
        var assessmentId = createdAssessment!.Id;

        // 2. Record diagnostic entrance evidence for applicant
        var evidenceRequest = new RecordEvidenceRequest(
            StudentId: null,
            ApplicantId: applicantId,
            SubmissionId: null,
            EvidenceType: "test_paper",
            CollectedDate: new DateOnly(2026, 8, 1),
            EvidenceReference: "admissions_entrance_rubric.pdf",
            Notes: "Admissions paper test submission");

        var evidenceRes = await Client.PostAsJsonAsync($"/api/v1/academics/assessments/{assessmentId}/evidence", evidenceRequest);
        evidenceRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // 3. Evaluate applicant score
        var evalRequest = new EvaluateStudentLearningRequest(
            StudentId: null,
            ApplicantId: applicantId,
            ScoreNumeric: 88m,
            ScoreMax: 100m,
            GradeLetter: "A",
            GradeLabel: "Sangat Baik",
            NarrativeFeedback: "Demonstrates strong foundational literacy and numeric aptitude",
            EvaluationModel: "numeric",
            PublishImmediately: false,
            Notes: "Admissions diagnostic evaluation");

        var evalRes = await Client.PostAsJsonAsync($"/api/v1/academics/assessments/{assessmentId}/results", evalRequest);
        evalRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var evalResultDto = await evalRes.Content.ReadFromJsonAsync<AcademicResultDto>();
        evalResultDto.Should().NotBeNull();
        evalResultDto!.ApplicantId.Should().Be(applicantId);
        evalResultDto.IsFinal.Should().BeTrue();
        evalResultDto.ScoreNumeric.Should().Be(88m);

        // 4. Verify IAcademicsModule.GetApplicantAssessmentScoreAsync returns authorized score to Admissions domain (GAP-07)
        var academicsModule = CreateAcademicsModule();
        var scoreQuery = await academicsModule.GetApplicantAssessmentScoreAsync(applicantId, assessmentId);

        scoreQuery.IsSuccess.Should().BeTrue();
        scoreQuery.Value.Should().NotBeNull();
        var applicantScoreDto = scoreQuery.Value!;
        applicantScoreDto.ApplicantId.Should().Be(applicantId);
        applicantScoreDto.AssessmentId.Should().Be(assessmentId);
        applicantScoreDto.AssessmentTitle.Should().Be("Admissions Diagnostic Mathematics Exam");
        applicantScoreDto.ScoreNumeric.Should().Be(88m);
        applicantScoreDto.ScoreMax.Should().Be(100m);
        applicantScoreDto.GradeLetter.Should().Be("A");
        applicantScoreDto.GradeLabel.Should().Be("Sangat Baik");
        applicantScoreDto.EvaluationModel.Should().Be("numeric");
        applicantScoreDto.ResultStatus.Should().Be("provisional");
    }

    [SkippableFact]
    public async Task FormativeAssessment_WithPublishImmediately_Should_AutonomouslyPublish_And_EmitEvent()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (_, unitId) = await SetupActiveCurriculumUnitAsync("FORMATIVE");

        var studentId = Guid.NewGuid();
        Factory.StudentsModule.MarkStudentEnrolled(studentId);

        Factory.EventCollector.Reset();

        // 1. Create formative assessment
        var assessmentRequest = new CreateAssessmentRequest(
            LearningUnitId: unitId,
            LearningActivityId: null,
            Title: "Daily Quiz 1",
            AssessmentType: "formative",
            AssessmentMethod: "quiz",
            MaxScore: 100m,
            PassingScore: 70m,
            ReassessmentPolicy: "highest_score",
            Criteria: "Formative checkpoint rubric",
            AssessmentDate: new DateOnly(2026, 9, 10),
            AssessorReference: "Subject Teacher",
            Notes: "Formative check");

        var createRes = await Client.PostAsJsonAsync("/api/v1/academics/assessments", assessmentRequest);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createRes.Content.ReadFromJsonAsync<CreateAssessmentResponse>();
        var assessmentId = created!.Id;

        // 2. Evaluate with publish_immediately = true
        var evalRequest = new EvaluateStudentLearningRequest(
            StudentId: studentId,
            ApplicantId: null,
            ScoreNumeric: 92m,
            ScoreMax: 100m,
            GradeLetter: "A",
            GradeLabel: "Sangat Mahir",
            NarrativeFeedback: "Great mastery demonstrated in daily quiz",
            EvaluationModel: "numeric",
            PublishImmediately: true,
            Notes: "Autonomous teacher formative release");

        var evalRes = await Client.PostAsJsonAsync($"/api/v1/academics/assessments/{assessmentId}/results", evalRequest);
        evalRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var evalDto = await evalRes.Content.ReadFromJsonAsync<AcademicResultDto>();

        // Assert: Formative autonomous publication succeeds immediately
        evalDto.Should().NotBeNull();
        evalDto!.ResultStatus.Should().Be("published");
        evalDto.PublishedDate.Should().NotBeNull();
        evalDto.ApprovedBy.Should().NotBeNullOrWhiteSpace();

        // Post-commit AcademicResultPublishedEvent emitted
        Factory.EventCollector.AcademicResultPublishedEvents
            .Should().Contain(e => e.ResultId == evalDto.Id && e.AssessmentId == assessmentId && e.ScoreNumeric == 92m);
    }

    [SkippableFact]
    public async Task CumulativeInterimTranscript_GenerateAndIssue_Should_FreezeSnapshot_And_EmitEvent()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (periodId, unitId) = await SetupActiveCurriculumUnitAsync("TRANSCRIPT");

        var studentId = Guid.NewGuid();
        Factory.StudentsModule.MarkStudentEnrolled(studentId);

        await using var connection = await OpenConnectionAsync();

        // Seed participation and student record
        await connection.ExecuteAsync("""
            INSERT INTO students.students (
                id, student_number, full_name, government_id_number, date_of_birth,
                enrollment_status, created_at, created_by
            ) VALUES (
                @StudentId, 'STD-TRANS-01', 'Citra Dewi', 'NISN-11223344', '2009-11-20',
                'active', SYSUTCDATETIME(), 'TEST_SETUP'
            );
            INSERT INTO academics.academic_participations (
                id, student_id, learning_unit_id, academic_period_id, section_label,
                status, start_date, completion_date, created_at, created_by
            ) VALUES (
                NEWID(), @StudentId, @UnitId, @PeriodId, '11-B',
                'completed', '2026-08-15', '2026-12-20', SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """, new { StudentId = studentId, UnitId = unitId, PeriodId = periodId });

        // 1. Generate interim transcript
        var generateRequest = new GenerateInterimTranscriptRequest(
            StudentId: studentId,
            TranscriptType: "official",
            PeriodStart: new DateOnly(2026, 8, 1),
            PeriodEnd: new DateOnly(2026, 12, 31),
            Purpose: "Scholarship Application");

        var generateRes = await Client.PostAsJsonAsync("/api/v1/academics/transcripts/generate", generateRequest);
        generateRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var transcriptDto = await generateRes.Content.ReadFromJsonAsync<TranscriptDto>();
        transcriptDto.Should().NotBeNull();
        transcriptDto!.Status.Should().Be("draft");
        var transcriptId = transcriptDto.Id;

        // 2. Issue transcript -> freezes content_snapshot, emits TranscriptIssuedEvent
        Factory.EventCollector.Reset();
        var issueRes = await Client.PostAsync($"/api/v1/academics/transcripts/{transcriptId}/issue", null);
        issueRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var issuedStatusDb = await connection.ExecuteScalarAsync<string>(
            "SELECT status FROM academics.transcripts WHERE id = @Id",
            new { Id = transcriptId });
        issuedStatusDb.Should().Be("issued");

        var snapshotDb = await connection.ExecuteScalarAsync<string>(
            "SELECT content_snapshot FROM academics.transcripts WHERE id = @Id",
            new { Id = transcriptId });
        snapshotDb.Should().NotBeNullOrWhiteSpace();
        snapshotDb.Should().Contain("Citra Dewi");

        Factory.EventCollector.TranscriptIssuedEvents
            .Should().Contain(e => e.TranscriptId == transcriptId && e.StudentId == studentId && e.TranscriptType == "official");
    }
}
