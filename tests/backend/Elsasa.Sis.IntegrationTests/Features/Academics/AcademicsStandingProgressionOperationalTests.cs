namespace Elsasa.Sis.IntegrationTests.Features.Academics;

using System.Net;
using System.Net.Http.Json;
using Dapper;
using Elsasa.Sis.Academics.Features.AcademicAchievements.ConferAcademicAchievement;
using Elsasa.Sis.Academics.Features.AcademicInterventions;
using Elsasa.Sis.Academics.Features.AcademicInterventions.InitiateAcademicIntervention;
using Elsasa.Sis.Academics.Features.AcademicInterventions.UpdateInterventionStatus;
using Elsasa.Sis.Academics.Features.AcademicStandings.BatchDetermineCohortStanding;
using Elsasa.Sis.Academics.Features.AcademicStandings.DetermineAcademicStanding;
using Elsasa.Sis.Academics.Features.CohortReview;
using Elsasa.Sis.Academics.Features.CohortReview.EvaluateCohortStanding;
using Elsasa.Sis.Academics.Features.LearningProgress.ConcludeParticipation;
using Elsasa.Sis.Academics.Features.LearningProgress.RecordLearningProgress;
using Elsasa.Sis.Academics.Features.PromotionRecommendations.BatchIssuePromotionRecommendations;
using Elsasa.Sis.Academics.Features.PromotionRecommendations.IssuePromotionRecommendation;
using Elsasa.Sis.Academics.Features.RecordCorrections.CorrectAcademicStanding;
using Elsasa.Sis.Academics.Features.RecordCorrections.CorrectPromotionRecommendation;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Operational end-to-end integration test suite verifying Submodule 4 Standing &amp; Progression workflows (IS012, P6-S17).
/// Verifies the complete operational lifecycle:
/// 1. Learning progress recording with explicit out-of-period completion date and formal participation conclusion emitting ParticipationConcludedEvent.
/// 2. Cohort evaluation preview dynamically calculating proposals and remediation condition stipulations under the Completion &amp; Passing Threshold Rule.
/// 3. Single and batch standing determinations and progression issuance committing atomically within SQL transactions with embedded SK Dewan Guru decree references.
/// 4. On-demand at-risk query execution dynamically detecting failing operative scores and attendance concerns (&lt;75%).
/// 5. Academic intervention lifecycle transitions (planned -&gt; in_progress -&gt; completed) with mandatory outcome logging.
/// 6. Academic achievement conferral and multi-dimension queries by student and period.
/// 7. Auditable non-grade record corrections persisting serialized audit entries into academics.academic_record_corrections.
/// 8. Cross-domain synchronous IAcademicsModule inquiry methods returning authoritative data.
/// </summary>
public sealed class AcademicsStandingProgressionOperationalTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    private IAcademicsModule CreateAcademicsModule()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IAcademicsModule>();
    }

    private async Task<(Guid AcademicYearId, Guid OperationalPeriodId, Guid UnitId)> SetupOperationalCurriculumAsync(string suffix)
    {
        await using var connection = await OpenConnectionAsync();
        var (ayId, periodId) = await AcademicsIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, suffix);

        var structure = await AcademicsIntegrationTestHelpers.CreateDraftStructureAsync(
            Client,
            periodId,
            name: $"Curriculum Framework {suffix}",
            version: "1.0");

        var unit = await AcademicsIntegrationTestHelpers.CreateRootUnitAsync(
            Client,
            structure.Id,
            code: $"U-{suffix[..Math.Min(suffix.Length, 8)]}",
            name: $"Operational Unit {suffix}",
            unitType: "course",
            creditWeight: 3.0m);

        var approveResponse = await Client.PostAsync($"/api/v1/academics/learning-structures/{structure.Id}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        return (ayId, periodId, unit.Id);
    }

    private static async Task EnsureStudentAsync(System.Data.Common.DbConnection connection, Guid studentId, string studentNumber, string fullName, string gradeLevel = "Grade 4")
    {
        const string insertStudentSql = """
            IF NOT EXISTS (SELECT 1 FROM students.students WHERE id = @StudentId)
            BEGIN
                INSERT INTO students.students (
                    id, student_number, full_name, government_id_number, date_of_birth,
                    enrollment_status, created_at, created_by
                ) VALUES (
                    @StudentId, @StudentNumber, @FullName, 'NISN-' + CAST(@StudentId AS NVARCHAR(36)), '2015-05-15',
                    'active', SYSUTCDATETIME(), 'TEST_SETUP'
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM students.administrative_placements WHERE student_id = @StudentId AND is_current = 1)
            BEGIN
                INSERT INTO students.administrative_placements (
                    id, student_id, school_level, grade, cohort, section,
                    academic_year, effective_date, is_current, created_at, created_by
                ) VALUES (
                    NEWID(), @StudentId, 'Elementary', @GradeLevel, @GradeLevel, 'A',
                    '2026/2027', '2026-08-01', 1, SYSUTCDATETIME(), 'TEST_SETUP'
                );
            END;
            """;

        await connection.ExecuteAsync(insertStudentSql, new
        {
            StudentId = studentId,
            StudentNumber = studentNumber,
            FullName = fullName,
            GradeLevel = gradeLevel
        });
    }

    private static async Task<Guid> EnsureParticipationAsync(
        System.Data.Common.DbConnection connection,
        Guid studentId,
        Guid unitId,
        Guid periodId,
        string sectionLabel = "Grade 4")
    {
        var participationId = Guid.NewGuid();
        const string insertParticipationSql = """
            INSERT INTO academics.academic_participations (
                id, student_id, learning_unit_id, academic_period_id, section_label,
                status, start_date, created_at, created_by
            ) VALUES (
                @ParticipationId, @StudentId, @UnitId, @PeriodId, @SectionLabel,
                'in_progress', '2026-08-01', SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;

        await connection.ExecuteAsync(insertParticipationSql, new
        {
            ParticipationId = participationId,
            StudentId = studentId,
            UnitId = unitId,
            PeriodId = periodId,
            SectionLabel = sectionLabel
        });

        return participationId;
    }

    [SkippableFact]
    public async Task LearningProgress_RecordAndConclude_Should_UpdateProgressAndEmitParticipationConcludedEvent()
    {
        SkipIfDatabaseUnavailable();
        Factory.EventCollector.Reset();

        var (_, periodId, unitId) = await SetupOperationalCurriculumAsync("LP-01");
        var studentId = Guid.NewGuid();
        Factory.StudentsModule.MarkStudentEnrolled(studentId);

        await using var connection = await OpenConnectionAsync();
        await EnsureStudentAsync(connection, studentId, "STD-LP-01", "Learning Progress Tester");
        var participationId = await EnsureParticipationAsync(connection, studentId, unitId, periodId);

        // 1. Record learning progress with explicit out-of-period completion date (transfer credit or accelerated pacing)
        var outOfPeriodDate = new DateOnly(2027, 2, 28);
        var recordCommand = new RecordLearningProgressCommand(
            StudentId: studentId,
            LearningUnitId: unitId,
            AcademicPeriodId: periodId,
            Status: "completed",
            Basis: "learning_work",
            CompletionDate: outOfPeriodDate,
            Notes: "Out-of-period PACE booklet mastery verification");

        var recordResponse = await Client.PostAsJsonAsync("/api/v1/academics/learning-progress", recordCommand);
        recordResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var recordedProgress = await recordResponse.Content.ReadFromJsonAsync<RecordLearningProgressResponse>();
        recordedProgress.Should().NotBeNull();
        recordedProgress!.Status.Should().Be("completed");
        recordedProgress.Basis.Should().Be("learning_work");
        recordedProgress.CompletionDate.Should().Be(outOfPeriodDate);

        // Verify database persistence
        const string queryProgressSql = """
            SELECT status AS Status, basis AS Basis, completion_date AS CompletionDate, notes AS Notes
            FROM academics.learning_progress
            WHERE student_id = @StudentId AND learning_unit_id = @UnitId AND academic_period_id = @PeriodId;
            """;
        var progressRow = await connection.QuerySingleOrDefaultAsync<ProgressDbRow>(queryProgressSql, new
        {
            StudentId = studentId,
            UnitId = unitId,
            PeriodId = periodId
        });
        progressRow.Should().NotBeNull();
        progressRow!.Status.Should().Be("completed");
        progressRow.Basis.Should().Be("learning_work");
        progressRow.CompletionDate.Should().Be(outOfPeriodDate.ToDateTime(TimeOnly.MinValue));

        // 2. Conclude active participation formally
        // Setup another unit and active participation with passing assessment result
        var structure = await AcademicsIntegrationTestHelpers.CreateDraftStructureAsync(Client, periodId, "Second Framework LP-02", "1.0");
        var unit2 = await AcademicsIntegrationTestHelpers.CreateRootUnitAsync(Client, structure.Id, "U-LP02", "Unit Two LP", creditWeight: 2.0m);
        await Client.PostAsync($"/api/v1/academics/learning-structures/{structure.Id}/approve", null);

        var participationId2 = await EnsureParticipationAsync(connection, studentId, unit2.Id, periodId);

        // Seed an approved passing assessment result so KKM passing criteria is satisfied
        var assessmentId = Guid.NewGuid();
        const string insertAssessmentSql = """
            INSERT INTO academics.assessments (
                id, learning_unit_id, title, assessment_type, assessment_method,
                max_score, passing_score, assessment_date, assessor_reference, status, created_at, created_by
            ) VALUES (
                @AssessmentId, @UnitId, 'Final Mastery Exam', 'summative', 'written_test',
                100.0, 75.0, '2026-11-01', 'Teacher A', 'conducted', SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;
        await connection.ExecuteAsync(insertAssessmentSql, new { AssessmentId = assessmentId, UnitId = unit2.Id });

        const string insertResultSql = """
            INSERT INTO academics.academic_results (
                id, assessment_id, student_id, score_numeric, attempt_number,
                is_final, result_status, evaluated_by, evaluated_at, created_at, created_by
            ) VALUES (
                NEWID(), @AssessmentId, @StudentId, 88.0, 1,
                1, 'approved', 'Teacher A', SYSUTCDATETIME(), SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;
        await connection.ExecuteAsync(insertResultSql, new { AssessmentId = assessmentId, StudentId = studentId });

        var concludeCommand = new ConcludeParticipationCommand(
            ParticipationId: participationId2,
            CompletionDate: new DateOnly(2026, 11, 5),
            Notes: "Student completed unit with score 88.0");

        var concludeResponse = await Client.PostAsJsonAsync("/api/v1/academics/learning-progress/conclude", concludeCommand);
        concludeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var concludeResult = await concludeResponse.Content.ReadFromJsonAsync<ConcludeParticipationResponse>();
        concludeResult.Should().NotBeNull();
        concludeResult!.ParticipationStatus.Should().Be("completed");
        concludeResult.ProgressStatus.Should().Be("completed");

        // Verify participation table status updated
        var partStatus = await connection.ExecuteScalarAsync<string>(
            "SELECT status FROM academics.academic_participations WHERE id = @Id",
            new { Id = participationId2 });
        partStatus.Should().Be("completed");

        // Verify event collector captured ParticipationConcludedEvent
        Factory.EventCollector.ParticipationConcludedEvents.Should().Contain(e =>
            e.ParticipationId == participationId2 &&
            e.StudentId == studentId &&
            e.LearningUnitId == unit2.Id);
    }

    [SkippableFact]
    public async Task EvaluateCohortStanding_Should_DynamicallyCalculateProposalsAndConditions()
    {
        SkipIfDatabaseUnavailable();

        var (_, periodId, unitId) = await SetupOperationalCurriculumAsync("EVAL-01");
        var gradeLevel = "Grade 4";

        var studentPassing = Guid.NewGuid();
        var studentConditional = Guid.NewGuid();
        var studentFailing = Guid.NewGuid();

        await using var connection = await OpenConnectionAsync();
        await EnsureStudentAsync(connection, studentPassing, "STD-PASS-01", "Rina Kusuma", gradeLevel);
        await EnsureStudentAsync(connection, studentConditional, "STD-COND-02", "Budi Santoso", gradeLevel);
        await EnsureStudentAsync(connection, studentFailing, "STD-FAIL-03", "Timotius Chronic", gradeLevel);

        var p1 = await EnsureParticipationAsync(connection, studentPassing, unitId, periodId, gradeLevel);
        var p2 = await EnsureParticipationAsync(connection, studentConditional, unitId, periodId, gradeLevel);
        var p3 = await EnsureParticipationAsync(connection, studentFailing, unitId, periodId, gradeLevel);

        // Seed second unit for conditional student to have 1 pass and 1 fail
        var structure = await AcademicsIntegrationTestHelpers.CreateDraftStructureAsync(Client, periodId, "Structure Math EVAL-02", "1.0");
        var unitMath = await AcademicsIntegrationTestHelpers.CreateRootUnitAsync(Client, structure.Id, "U-MATH", "Mathematics Course", creditWeight: 3.0m);
        await Client.PostAsync($"/api/v1/academics/learning-structures/{structure.Id}/approve", null);

        await EnsureParticipationAsync(connection, studentConditional, unitMath.Id, periodId, gradeLevel);

        // 1. StudentPassing: passed unitId (score 90 >= passing 75)
        var asm1 = Guid.NewGuid();
        const string insertAsmSql = """
            INSERT INTO academics.assessments (
                id, learning_unit_id, title, assessment_type, assessment_method,
                max_score, passing_score, assessment_date, assessor_reference, status, created_at, created_by
            ) VALUES (
                @Id, @UnitId, 'Standard Exam', 'summative', 'written_test',
                100.0, 75.0, '2026-10-10', 'Teacher Assessor', 'conducted', SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;
        await connection.ExecuteAsync(insertAsmSql, new { Id = asm1, UnitId = unitId });

        const string insertResultSql = """
            INSERT INTO academics.academic_results (
                id, assessment_id, student_id, score_numeric, attempt_number,
                is_final, result_status, evaluated_by, evaluated_at, created_at, created_by
            ) VALUES (
                NEWID(), @AssessmentId, @StudentId, @Score, 1,
                1, 'approved', 'Teacher Evaluator', SYSUTCDATETIME(), SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;
        // Passing student: score 92
        await connection.ExecuteAsync(insertResultSql, new { AssessmentId = asm1, StudentId = studentPassing, Score = 92.0m });

        // Conditional student: unitId passed (score 85), but unitMath failed (score 58 < passing 75)
        var asmMath = Guid.NewGuid();
        await connection.ExecuteAsync(insertAsmSql, new { Id = asmMath, UnitId = unitMath.Id });
        await connection.ExecuteAsync(insertResultSql, new { AssessmentId = asm1, StudentId = studentConditional, Score = 85.0m });
        await connection.ExecuteAsync(insertResultSql, new { AssessmentId = asmMath, StudentId = studentConditional, Score = 58.0m });

        // Failing student: unitId failed (score 45 < 75) and chronic non-attendance (<75%)
        await connection.ExecuteAsync(insertResultSql, new { AssessmentId = asm1, StudentId = studentFailing, Score = 45.0m });

        // Add attendance records for Failing student: 1 present, 4 absent = 20% attendance
        var activityId = Guid.NewGuid();
        const string insertActivitySql = """
            INSERT INTO academics.learning_activities (
                id, learning_unit_id, academic_period_id, title, activity_type,
                activity_date, status, created_at, created_by
            ) VALUES (
                @Id, @UnitId, @PeriodId, 'Morning Class Session', 'lecture',
                '2026-10-01', 'conducted', SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;
        await connection.ExecuteAsync(insertActivitySql, new { Id = activityId, UnitId = unitId, PeriodId = periodId });

        const string insertAttendanceSql = """
            INSERT INTO academics.attendance_records (
                id, learning_activity_id, student_id, status, recorded_at, recorded_by, created_at, created_by
            ) VALUES
            (NEWID(), @ActivityId, @StudentId, 'present', SYSUTCDATETIME(), 'Teacher', SYSUTCDATETIME(), 'TEST_SETUP'),
            (NEWID(), @ActivityId, @StudentId, 'absent', SYSUTCDATETIME(), 'Teacher', SYSUTCDATETIME(), 'TEST_SETUP'),
            (NEWID(), @ActivityId, @StudentId, 'absent', SYSUTCDATETIME(), 'Teacher', SYSUTCDATETIME(), 'TEST_SETUP'),
            (NEWID(), @ActivityId, @StudentId, 'absent', SYSUTCDATETIME(), 'Teacher', SYSUTCDATETIME(), 'TEST_SETUP'),
            (NEWID(), @ActivityId, @StudentId, 'absent', SYSUTCDATETIME(), 'Teacher', SYSUTCDATETIME(), 'TEST_SETUP');
            """;
        await connection.ExecuteAsync(insertAttendanceSql, new { ActivityId = activityId, StudentId = studentFailing });

        // Execute dynamic evaluation request: POST /api/v1/academics/cohort-review/evaluate-standing
        var evaluateRequest = new EvaluateCohortStandingRequest(periodId, gradeLevel);
        var evaluateResponse = await Client.PostAsJsonAsync("/api/v1/academics/cohort-review/evaluate-standing", evaluateRequest);
        evaluateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var proposals = await evaluateResponse.Content.ReadFromJsonAsync<IReadOnlyList<CohortStandingProposalDto>>();
        proposals.Should().NotBeNull();
        proposals!.Count.Should().BeGreaterThanOrEqualTo(3);

        var passingProposal = proposals.FirstOrDefault(p => p.StudentId == studentPassing);
        passingProposal.Should().NotBeNull();
        passingProposal!.ProposedStanding.Should().Be("good_standing");

        var conditionalProposal = proposals.FirstOrDefault(p => p.StudentId == studentConditional);
        conditionalProposal.Should().NotBeNull();
        conditionalProposal!.ProposedStanding.Should().Be("conditional_standing");
        conditionalProposal.Conditions.Should().NotBeNullOrWhiteSpace();
        conditionalProposal.Conditions.Should().Contain("Mathematics Course");

        var failingProposal = proposals.FirstOrDefault(p => p.StudentId == studentFailing);
        failingProposal.Should().NotBeNull();
        failingProposal!.ProposedStanding.Should().Be("not_in_good_standing");
    }

    [SkippableFact]
    public async Task BatchDetermineCohortStanding_And_BatchIssuePromotionRecommendations_Should_CommitAtomicallyWithDecrees()
    {
        SkipIfDatabaseUnavailable();
        Factory.EventCollector.Reset();

        var (_, periodId, unitId) = await SetupOperationalCurriculumAsync("BATCH-01");
        var gradeLevel = "Grade 4";

        var student1 = Guid.NewGuid();
        var student2 = Guid.NewGuid();

        await using var connection = await OpenConnectionAsync();
        await EnsureStudentAsync(connection, student1, "STD-BATCH-01", "Batch Student One", gradeLevel);
        await EnsureStudentAsync(connection, student2, "STD-BATCH-02", "Batch Student Two", gradeLevel);

        await EnsureParticipationAsync(connection, student1, unitId, periodId, gradeLevel);
        await EnsureParticipationAsync(connection, student2, unitId, periodId, gradeLevel);

        // 1. Batch determine academic standings with embedded SK Dewan Guru decree reference
        var decreeReference = "SK-DG/2026/06-01";
        var standingItems = new List<StandingItemDto>
        {
            new(
                StudentId: student1,
                Standing: "good_standing",
                PolicyReference: "SK-AKAD/2026/POLICY-01",
                Conditions: null,
                IsException: false),
            new(
                StudentId: student2,
                Standing: "conditional_standing",
                PolicyReference: "SK-AKAD/2026/POLICY-01",
                Conditions: "Must complete remedial Mathematics packet before next term",
                IsException: true,
                ExceptionReason: "Medical exception approved by Dewan Guru committee",
                CommitteeReference: decreeReference)
        };

        var batchStandingCommand = new BatchDetermineCohortStandingCommand(periodId, standingItems);
        var standingResponse = await Client.PostAsJsonAsync("/api/v1/academics/academic-standings/batch", batchStandingCommand);
        standingResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var standingResult = await standingResponse.Content.ReadFromJsonAsync<BatchDetermineCohortStandingResponse>();
        standingResult.Should().NotBeNull();
        standingResult!.TotalProcessed.Should().Be(2);

        // Verify records in database
        const string queryStandingsSql = """
            SELECT student_id, standing, is_exception, exception_reason, committee_reference
            FROM academics.academic_standings
            WHERE academic_period_id = @PeriodId;
            """;
        var standingRows = (await connection.QueryAsync(queryStandingsSql, new { PeriodId = periodId })).ToList();
        standingRows.Should().HaveCount(2);

        var s2Row = standingRows.Single(r => (Guid)r.student_id == student2);
        ((string)s2Row.standing).Should().Be("conditional_standing");
        ((bool)s2Row.is_exception).Should().BeTrue();
        ((string)s2Row.committee_reference).Should().Be(decreeReference);

        // Verify domain events collected
        Factory.EventCollector.AcademicStandingDeterminedEvents.Should().Contain(e => e.StudentId == student1 && e.Standing == "good_standing");
        Factory.EventCollector.AcademicStandingDeterminedEvents.Should().Contain(e => e.StudentId == student2 && e.Standing == "conditional_standing" && e.IsException);

        // 2. Batch issue promotion recommendations linking to standing
        var progressionItems = new List<ProgressionItemDto>
        {
            new(
                StudentId: student1,
                Recommendation: "promotion_eligible",
                FromLevel: "Grade 4",
                RecommendedToLevel: "Grade 5",
                Conditions: null,
                SupportingEvidence: "Passed all Grade 4 units in good standing",
                IsException: false),
            new(
                StudentId: student2,
                Recommendation: "conditional_promotion",
                FromLevel: "Grade 4",
                RecommendedToLevel: "Grade 5",
                Conditions: "Must submit completed Mathematics remediation packet",
                SupportingEvidence: "Committee approved conditional advancement per SK Dewan Guru",
                IsException: true,
                ExceptionReason: "Council override for Grade 4 to Grade 5 advancement",
                CommitteeReference: decreeReference)
        };

        var batchProgressionCommand = new BatchIssuePromotionRecommendationsCommand(periodId, progressionItems);
        var progressionResponse = await Client.PostAsJsonAsync("/api/v1/academics/promotion-recommendations/batch", batchProgressionCommand);
        progressionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var progressionResult = await progressionResponse.Content.ReadFromJsonAsync<BatchIssuePromotionRecommendationsResponse>();
        progressionResult.Should().NotBeNull();
        progressionResult!.TotalProcessed.Should().Be(2);

        // Verify records in database with resolved standing foreign key
        const string queryProgressionSql = """
            SELECT student_id, recommendation, from_level, recommended_to_level, academic_standing_id, is_exception, committee_reference
            FROM academics.promotion_recommendations
            WHERE academic_period_id = @PeriodId;
            """;
        var progressionRows = (await connection.QueryAsync(queryProgressionSql, new { PeriodId = periodId })).ToList();
        progressionRows.Should().HaveCount(2);

        var p2Row = progressionRows.Single(r => (Guid)r.student_id == student2);
        ((string)p2Row.recommendation).Should().Be("conditional_promotion");
        ((string)p2Row.from_level).Should().Be("Grade 4");
        ((string)p2Row.recommended_to_level).Should().Be("Grade 5");
        ((Guid?)p2Row.academic_standing_id).Should().NotBeNull();
        ((bool)p2Row.is_exception).Should().BeTrue();
        ((string)p2Row.committee_reference).Should().Be(decreeReference);

        // Verify domain events collected
        Factory.EventCollector.PromotionRecommendationIssuedEvents.Should().Contain(e => e.StudentId == student1 && e.Recommendation == "promotion_eligible");
        Factory.EventCollector.PromotionRecommendationIssuedEvents.Should().Contain(e => e.StudentId == student2 && e.Recommendation == "conditional_promotion" && e.IsException);
    }

    [SkippableFact]
    public async Task GetAtRiskLearnersQuery_Should_DynamicallyDetectFailingScoresAndLowAttendance()
    {
        SkipIfDatabaseUnavailable();

        var (_, periodId, unitId) = await SetupOperationalCurriculumAsync("ATRISK-01");
        var studentScoreRisk = Guid.NewGuid();
        var studentAttendanceRisk = Guid.NewGuid();

        await using var connection = await OpenConnectionAsync();
        await EnsureStudentAsync(connection, studentScoreRisk, "STD-RISK-SCORE", "Score Risk Learner");
        await EnsureStudentAsync(connection, studentAttendanceRisk, "STD-RISK-ATTN", "Attendance Risk Learner");

        await EnsureParticipationAsync(connection, studentScoreRisk, unitId, periodId);
        await EnsureParticipationAsync(connection, studentAttendanceRisk, unitId, periodId);

        // 1. Trigger: assessment_below_threshold (score 55 < passing 75)
        var asm = Guid.NewGuid();
        const string insertAsmSql = """
            INSERT INTO academics.assessments (
                id, learning_unit_id, title, assessment_type, assessment_method,
                max_score, passing_score, assessment_date, assessor_reference, status, created_at, created_by
            ) VALUES (
                @Id, @UnitId, 'Quarterly Assessment', 'summative', 'written_test',
                100.0, 75.0, '2026-10-15', 'Teacher Assessor', 'conducted', SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;
        await connection.ExecuteAsync(insertAsmSql, new { Id = asm, UnitId = unitId });

        const string insertResultSql = """
            INSERT INTO academics.academic_results (
                id, assessment_id, student_id, score_numeric, attempt_number,
                is_final, result_status, evaluated_by, evaluated_at, created_at, created_by
            ) VALUES (
                NEWID(), @AssessmentId, @StudentId, 55.0, 1,
                1, 'approved', 'Teacher Evaluator', SYSUTCDATETIME(), SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;
        await connection.ExecuteAsync(insertResultSql, new { AssessmentId = asm, StudentId = studentScoreRisk });

        // 2. Trigger: attendance_concern (< 75%)
        var act = Guid.NewGuid();
        const string insertActSql = """
            INSERT INTO academics.learning_activities (
                id, learning_unit_id, academic_period_id, title, activity_type,
                activity_date, status, created_at, created_by
            ) VALUES (
                @Id, @UnitId, @PeriodId, 'Weekly Seminar', 'seminar',
                '2026-10-10', 'conducted', SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;
        await connection.ExecuteAsync(insertActSql, new { Id = act, UnitId = unitId, PeriodId = periodId });

        const string insertAttSql = """
            INSERT INTO academics.attendance_records (
                id, learning_activity_id, student_id, status, recorded_at, recorded_by, created_at, created_by
            ) VALUES
            (NEWID(), @ActivityId, @StudentId, 'present', SYSUTCDATETIME(), 'Teacher', SYSUTCDATETIME(), 'TEST_SETUP'),
            (NEWID(), @ActivityId, @StudentId, 'absent', SYSUTCDATETIME(), 'Teacher', SYSUTCDATETIME(), 'TEST_SETUP'),
            (NEWID(), @ActivityId, @StudentId, 'absent', SYSUTCDATETIME(), 'Teacher', SYSUTCDATETIME(), 'TEST_SETUP'),
            (NEWID(), @ActivityId, @StudentId, 'absent', SYSUTCDATETIME(), 'Teacher', SYSUTCDATETIME(), 'TEST_SETUP');
            """;
        await connection.ExecuteAsync(insertAttSql, new { ActivityId = act, StudentId = studentAttendanceRisk });

        // Execute query: GET /api/v1/academics/interventions/at-risk?academicPeriodId={periodId}
        var atRiskResponse = await Client.GetAsync($"/api/v1/academics/interventions/at-risk?academicPeriodId={periodId}");
        atRiskResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var atRiskLearners = await atRiskResponse.Content.ReadFromJsonAsync<IReadOnlyList<AtRiskLearnerDto>>();
        atRiskLearners.Should().NotBeNull();

        var scoreRiskStudent = atRiskLearners!.FirstOrDefault(s => s.StudentId == studentScoreRisk);
        scoreRiskStudent.Should().NotBeNull();
        scoreRiskStudent!.TriggerType.Should().Be("assessment_below_threshold");
        scoreRiskStudent.OperativeScore.Should().Be(55.0m);
        scoreRiskStudent.PassingScore.Should().Be(75.0m);

        var attendanceRiskStudent = atRiskLearners!.FirstOrDefault(s => s.StudentId == studentAttendanceRisk);
        attendanceRiskStudent.Should().NotBeNull();
        attendanceRiskStudent!.TriggerType.Should().Be("attendance_concern");
        attendanceRiskStudent.AttendanceRate.Should().BeLessThan(75.0m);
    }

    [SkippableFact]
    public async Task AcademicIntervention_InitiateAndUpdateStatus_Should_TransitionFromPlannedToCompletedWithOutcomeLogging()
    {
        SkipIfDatabaseUnavailable();
        Factory.EventCollector.Reset();

        var (_, periodId, unitId) = await SetupOperationalCurriculumAsync("INT-01");
        var studentId = Guid.NewGuid();

        await using var connection = await OpenConnectionAsync();
        await EnsureStudentAsync(connection, studentId, "STD-INT-01", "Intervention Student");
        await EnsureParticipationAsync(connection, studentId, unitId, periodId);

        // 1. Initiate academic intervention: POST /api/v1/academics/interventions
        var initiateCommand = new InitiateAcademicInterventionCommand(
            StudentId: studentId,
            LearningUnitId: unitId,
            AcademicPeriodId: periodId,
            InterventionType: "remediation",
            TriggerType: "assessment_below_threshold",
            TriggerDescription: "Failed initial PACE diagnostic test with score 54",
            PlanDescription: "Bi-weekly after-school tutoring on foundational algebraic concepts");

        var initiateResponse = await Client.PostAsJsonAsync("/api/v1/academics/interventions", initiateCommand);
        initiateResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdIntervention = await initiateResponse.Content.ReadFromJsonAsync<AcademicInterventionDto>();
        createdIntervention.Should().NotBeNull();
        createdIntervention!.Status.Should().Be("planned");
        createdIntervention.InterventionType.Should().Be("remediation");
        createdIntervention.TriggerType.Should().Be("assessment_below_threshold");

        var interventionId = createdIntervention.Id;

        // Verify event published
        Factory.EventCollector.AcademicInterventionInitiatedEvents.Should().Contain(e =>
            e.InterventionId == interventionId &&
            e.StudentId == studentId &&
            e.InterventionType == "remediation");

        // 2. Transition lifecycle: planned -> in_progress
        var inProgressRequest = new UpdateInterventionStatusRequest("in_progress");
        var inProgressResponse = await Client.PatchAsJsonAsync($"/api/v1/academics/interventions/{interventionId}/status", inProgressRequest);
        inProgressResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var inProgressResult = await inProgressResponse.Content.ReadFromJsonAsync<AcademicInterventionDto>();
        inProgressResult.Should().NotBeNull();
        inProgressResult!.Status.Should().Be("in_progress");

        // 3. Complete intervention with mandatory outcome notes: in_progress -> completed
        var outcomeNotes = "Student completed 8 remedial tutoring sessions and successfully retested with score 84.";
        var completeRequest = new UpdateInterventionStatusRequest("completed", outcomeNotes);
        var completeResponse = await Client.PatchAsJsonAsync($"/api/v1/academics/interventions/{interventionId}/status", completeRequest);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var completedResult = await completeResponse.Content.ReadFromJsonAsync<AcademicInterventionDto>();
        completedResult.Should().NotBeNull();
        completedResult!.Status.Should().Be("completed");
        completedResult.OutcomeNotes.Should().Be(outcomeNotes);
        completedResult.ConcludedAt.Should().NotBeNull();

        // Verify database persistence
        const string queryInterventionSql = """
            SELECT status AS Status, concluded_at AS ConcludedAt, outcome_notes AS OutcomeNotes
            FROM academics.academic_interventions
            WHERE id = @Id;
            """;
        var dbRow = await connection.QuerySingleOrDefaultAsync<InterventionDbRow>(queryInterventionSql, new { Id = interventionId });
        dbRow.Should().NotBeNull();
        dbRow!.Status.Should().Be("completed");
        dbRow.OutcomeNotes.Should().Be(outcomeNotes);
        dbRow.ConcludedAt.Should().NotBeNull();

        // Verify event published
        Factory.EventCollector.AcademicInterventionConcludedEvents.Should().Contain(e =>
            e.InterventionId == interventionId &&
            e.StudentId == studentId &&
            e.OutcomeNotes == outcomeNotes);
    }

    [SkippableFact]
    public async Task AcademicAchievements_ConferAndQuery_Should_RecordHonorsAndRetrieveByStudentAndPeriod()
    {
        SkipIfDatabaseUnavailable();
        Factory.EventCollector.Reset();

        var (_, periodId, unitId) = await SetupOperationalCurriculumAsync("ACH-01");
        var studentId = Guid.NewGuid();

        await using var connection = await OpenConnectionAsync();
        await EnsureStudentAsync(connection, studentId, "STD-ACH-01", "Honor Student");
        await EnsureParticipationAsync(connection, studentId, unitId, periodId);

        // 1. Confer academic achievement: POST /api/v1/academics/achievements
        var conferCommand = new ConferAcademicAchievementCommand(
            StudentId: studentId,
            LearningUnitId: unitId,
            AcademicPeriodId: periodId,
            AchievementType: "academic_honors",
            Title: "Juara Umum Grade 4",
            Description: "Highest academic achievement across all core PACE subjects",
            CriteriaMet: "GPA 4.0 and 100% attendance throughout Semester 1",
            SupportingEvidence: "Official Report Card & Transcript Verification",
            RecognizedAt: new DateOnly(2026, 11, 20));

        var conferResponse = await Client.PostAsJsonAsync("/api/v1/academics/achievements", conferCommand);
        conferResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdAchievement = await conferResponse.Content.ReadFromJsonAsync<AcademicAchievementDto>();
        createdAchievement.Should().NotBeNull();
        createdAchievement!.Title.Should().Be("Juara Umum Grade 4");
        createdAchievement.AchievementType.Should().Be("academic_honors");

        var achievementId = createdAchievement.Id;

        // Verify domain event collected
        Factory.EventCollector.AcademicAchievementConferredEvents.Should().Contain(e =>
            e.AchievementId == achievementId &&
            e.StudentId == studentId &&
            e.Title == "Juara Umum Grade 4");

        // 2. Query student achievements: GET /api/v1/academics/students/{studentId}/achievements
        var studentAchResponse = await Client.GetAsync($"/api/v1/academics/students/{studentId}/achievements?academicPeriodId={periodId}");
        studentAchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var studentAchievements = await studentAchResponse.Content.ReadFromJsonAsync<IReadOnlyList<AcademicAchievementDto>>();
        studentAchievements.Should().NotBeNull();
        studentAchievements.Should().ContainSingle(a => a.Id == achievementId);

        // 3. Query cohort period achievements: GET /api/v1/academics/cohort-review/achievements
        var periodAchResponse = await Client.GetAsync($"/api/v1/academics/cohort-review/achievements?academicPeriodId={periodId}");
        periodAchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var periodAchievements = await periodAchResponse.Content.ReadFromJsonAsync<IReadOnlyList<AcademicAchievementDto>>();
        periodAchievements.Should().NotBeNull();
        periodAchievements.Should().Contain(a => a.Id == achievementId);
    }

    [SkippableFact]
    public async Task RecordCorrections_CorrectStandingAndProgression_Should_MutateRecordsAndPersistSerializedAuditEntries()
    {
        SkipIfDatabaseUnavailable();
        Factory.EventCollector.Reset();

        var (_, periodId, unitId) = await SetupOperationalCurriculumAsync("CORR-01");
        var studentId = Guid.NewGuid();

        await using var connection = await OpenConnectionAsync();
        await EnsureStudentAsync(connection, studentId, "STD-CORR-01", "Correction Student");
        await EnsureParticipationAsync(connection, studentId, unitId, periodId);

        // Seed initial standing and progression recommendation
        var standingId = Guid.NewGuid();
        const string insertStandingSql = """
            INSERT INTO academics.academic_standings (
                id, student_id, academic_period_id, standing, policy_reference, conditions,
                is_exception, exception_reason, committee_reference, authorized_by, determined_at,
                created_at, created_by
            ) VALUES (
                @Id, @StudentId, @PeriodId, 'conditional_standing', 'SK-AKAD/2026/POLICY-01', 'Math remediation required',
                0, NULL, NULL, 'Initial Committee', SYSUTCDATETIME(), SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;
        await connection.ExecuteAsync(insertStandingSql, new { Id = standingId, StudentId = studentId, PeriodId = periodId });

        var recommendationId = Guid.NewGuid();
        const string insertRecommendationSql = """
            INSERT INTO academics.promotion_recommendations (
                id, student_id, academic_period_id, recommendation, from_level, recommended_to_level,
                conditions, supporting_evidence, academic_standing_id, is_exception, exception_reason,
                committee_reference, authorized_by, issued_at, created_at, created_by
            ) VALUES (
                @Id, @StudentId, @PeriodId, 'conditional_promotion', 'Grade 4', 'Grade 5',
                'Must complete math packet', 'Conditional standing on record', @StandingId, 0, NULL,
                NULL, 'Initial Committee', SYSUTCDATETIME(), SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;
        await connection.ExecuteAsync(insertRecommendationSql, new
        {
            Id = recommendationId,
            StudentId = studentId,
            PeriodId = periodId,
            StandingId = standingId
        });

        // 1. Correct Academic Standing: POST /api/v1/academics/academic-standings/{id}/correct
        var correctStandingReason = "Discovered administrative score data entry error in Mathematics; corrected gradebook reflects passing standard";
        var correctStandingRequest = new CorrectAcademicStandingRequest(
            Standing: "good_standing",
            PolicyReference: "SK-AKAD/2026/POLICY-01",
            Conditions: null,
            IsException: false,
            ExceptionReason: null,
            CommitteeReference: null,
            Reason: correctStandingReason);

        var correctStandingResponse = await Client.PostAsJsonAsync($"/api/v1/academics/academic-standings/{standingId}/correct", correctStandingRequest);
        correctStandingResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedStandingDto = await correctStandingResponse.Content.ReadFromJsonAsync<StudentAcademicStandingDto>();
        updatedStandingDto.Should().NotBeNull();
        updatedStandingDto!.Standing.Should().Be("good_standing");

        // Verify academics.academic_standings mutated
        var standingState = await connection.ExecuteScalarAsync<string>(
            "SELECT standing FROM academics.academic_standings WHERE id = @Id",
            new { Id = standingId });
        standingState.Should().Be("good_standing");

        // Verify immutable audit entry in academics.academic_record_corrections
        const string queryStandingAuditSql = """
            SELECT original_value AS OriginalValue, corrected_value AS CorrectedValue, reason AS Reason, record_type AS RecordType, is_material AS IsMaterial
            FROM academics.academic_record_corrections
            WHERE record_id = @RecordId AND record_type = 'academic_standing';
            """;
        var standingAudit = await connection.QuerySingleOrDefaultAsync<CorrectionDbRow>(queryStandingAuditSql, new { RecordId = standingId });
        standingAudit.Should().NotBeNull();
        standingAudit!.OriginalValue.Should().Contain("conditional_standing");
        standingAudit.CorrectedValue.Should().Contain("good_standing");
        standingAudit.Reason.Should().Be(correctStandingReason);
        standingAudit.IsMaterial.Should().BeTrue();

        // Verify domain event
        Factory.EventCollector.AcademicStandingCorrectedEvents.Should().Contain(e =>
            e.StandingId == standingId &&
            e.OldStanding == "conditional_standing" &&
            e.NewStanding == "good_standing" &&
            e.Reason == correctStandingReason);

        // 2. Correct Promotion Recommendation: POST /api/v1/academics/promotion-recommendations/{id}/correct
        var correctRecReason = "Upgrading progression recommendation to unconditional promotion following standing correction approval";
        var correctRecRequest = new CorrectPromotionRecommendationRequest(
            Recommendation: "promotion_eligible",
            FromLevel: "Grade 4",
            RecommendedToLevel: "Grade 5",
            Conditions: null,
            SupportingEvidence: "All Grade 4 standards met with standing restored to good standing",
            IsException: false,
            ExceptionReason: null,
            CommitteeReference: null,
            Reason: correctRecReason);

        var correctRecResponse = await Client.PostAsJsonAsync($"/api/v1/academics/promotion-recommendations/{recommendationId}/correct", correctRecRequest);
        correctRecResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedRecDto = await correctRecResponse.Content.ReadFromJsonAsync<PromotionRecommendationDto>();
        updatedRecDto.Should().NotBeNull();
        updatedRecDto!.Recommendation.Should().Be("promotion_eligible");

        // Verify academics.promotion_recommendations mutated
        var recState = await connection.ExecuteScalarAsync<string>(
            "SELECT recommendation FROM academics.promotion_recommendations WHERE id = @Id",
            new { Id = recommendationId });
        recState.Should().Be("promotion_eligible");

        // Verify immutable audit entry in academics.academic_record_corrections
        const string queryRecAuditSql = """
            SELECT original_value AS OriginalValue, corrected_value AS CorrectedValue, reason AS Reason, record_type AS RecordType, is_material AS IsMaterial
            FROM academics.academic_record_corrections
            WHERE record_id = @RecordId AND record_type = 'promotion_recommendation';
            """;
        var recAudit = await connection.QuerySingleOrDefaultAsync<CorrectionDbRow>(queryRecAuditSql, new { RecordId = recommendationId });
        recAudit.Should().NotBeNull();
        recAudit!.OriginalValue.Should().Contain("conditional_promotion");
        recAudit.CorrectedValue.Should().Contain("promotion_eligible");
        recAudit.Reason.Should().Be(correctRecReason);
        recAudit.IsMaterial.Should().BeTrue();

        // Verify domain event
        Factory.EventCollector.PromotionRecommendationCorrectedEvents.Should().Contain(e =>
            e.RecommendationId == recommendationId &&
            e.OldRecommendation == "conditional_promotion" &&
            e.NewRecommendation == "promotion_eligible" &&
            e.Reason == correctRecReason);
    }

    [SkippableFact]
    public async Task AcademicsModule_Inquiries_Should_ReturnExpectedValuesToConsumerDomains()
    {
        SkipIfDatabaseUnavailable();

        var (_, periodId, unitId) = await SetupOperationalCurriculumAsync("MOD-01");
        var studentId = Guid.NewGuid();

        await using var connection = await OpenConnectionAsync();
        await EnsureStudentAsync(connection, studentId, "STD-MOD-01", "Module Inquiries Student");
        await EnsureParticipationAsync(connection, studentId, unitId, periodId);

        // Seed standing
        var standingId = Guid.NewGuid();
        const string insertStandingSql = """
            INSERT INTO academics.academic_standings (
                id, student_id, academic_period_id, standing, policy_reference, conditions,
                is_exception, exception_reason, committee_reference, authorized_by, determined_at,
                created_at, created_by
            ) VALUES (
                @Id, @StudentId, @PeriodId, 'good_standing', 'SK-AKAD/2026/POLICY-01', NULL,
                0, NULL, NULL, 'Principal Head', SYSUTCDATETIME(), SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;
        await connection.ExecuteAsync(insertStandingSql, new { Id = standingId, StudentId = studentId, PeriodId = periodId });

        // Seed promotion recommendation
        var recId = Guid.NewGuid();
        const string insertRecSql = """
            INSERT INTO academics.promotion_recommendations (
                id, student_id, academic_period_id, recommendation, from_level, recommended_to_level,
                conditions, supporting_evidence, academic_standing_id, is_exception, exception_reason,
                committee_reference, authorized_by, issued_at, created_at, created_by
            ) VALUES (
                @Id, @StudentId, @PeriodId, 'promotion_eligible', 'Grade 4', 'Grade 5',
                NULL, 'Exemplary academic progress', @StandingId, 0, NULL,
                NULL, 'Principal Head', SYSUTCDATETIME(), SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;
        await connection.ExecuteAsync(insertRecSql, new { Id = recId, StudentId = studentId, PeriodId = periodId, StandingId = standingId });

        // Seed achievement
        const string insertAchSql = """
            INSERT INTO academics.academic_achievements (
                id, student_id, learning_unit_id, academic_period_id, achievement_type,
                title, description, criteria_met, supporting_evidence, recognized_by, recognized_at,
                created_at, created_by
            ) VALUES (
                NEWID(), @StudentId, @UnitId, @PeriodId, 'subject_distinction',
                'Matematika Terbaik', 'Highest score in class', 'Score 100 on all exams', 'Exam papers',
                'Principal Head', '2026-11-01', SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;
        await connection.ExecuteAsync(insertAchSql, new { StudentId = studentId, UnitId = unitId, PeriodId = periodId });

        // Seed learning progress (2 completed, 1 in_progress)
        const string insertProgressSql = """
            INSERT INTO academics.learning_progress (
                id, student_id, learning_unit_id, academic_period_id, status, basis,
                completion_date, notes, created_at, created_by
            ) VALUES
            (NEWID(), @StudentId, @UnitId, @PeriodId, 'completed', 'assessment_evidence', '2026-11-01', 'Done', SYSUTCDATETIME(), 'TEST_SETUP');
            """;
        await connection.ExecuteAsync(insertProgressSql, new { StudentId = studentId, UnitId = unitId, PeriodId = periodId });

        var academicsModule = CreateAcademicsModule();

        // 1. GetStudentAcademicStandingAsync
        var standingResult = await academicsModule.GetStudentAcademicStandingAsync(studentId, periodId);
        standingResult.IsSuccess.Should().BeTrue();
        standingResult.Value.Should().NotBeNull();
        standingResult.Value!.Standing.Should().Be("good_standing");
        standingResult.Value.PolicyReference.Should().Be("SK-AKAD/2026/POLICY-01");

        // 2. GetPromotionRecommendationAsync
        var recResult = await academicsModule.GetPromotionRecommendationAsync(studentId, periodId);
        recResult.IsSuccess.Should().BeTrue();
        recResult.Value.Should().NotBeNull();
        recResult.Value!.Recommendation.Should().Be("promotion_eligible");
        recResult.Value.RecommendedToLevel.Should().Be("Grade 5");

        // 3. GetStudentAcademicAchievementsAsync
        var achResult = await academicsModule.GetStudentAcademicAchievementsAsync(studentId, periodId);
        achResult.IsSuccess.Should().BeTrue();
        achResult.Value.Should().NotBeNull();
        achResult.Value.Should().ContainSingle(a => a.Title == "Matematika Terbaik");

        // 4. GetStudentLearningProgressSummaryAsync
        var progressSummaryResult = await academicsModule.GetStudentLearningProgressSummaryAsync(studentId, periodId);
        progressSummaryResult.IsSuccess.Should().BeTrue();
        progressSummaryResult.Value.Should().NotBeNull();
        progressSummaryResult.Value.Should().Contain(s => s.Status == "completed" && s.UnitCount >= 1);
    }

    private sealed record ProgressDbRow(string Status, string Basis, DateTime? CompletionDate, string? Notes);

    private sealed record InterventionDbRow(string Status, DateTimeOffset? ConcludedAt, string? OutcomeNotes);

    private sealed record CorrectionDbRow(string OriginalValue, string CorrectedValue, string Reason, string RecordType, bool IsMaterial);
}
