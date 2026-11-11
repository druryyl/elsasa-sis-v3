namespace Elsasa.Sis.Academics.Features.DemoData;

using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using Dapper;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Contracts.Operations;
using Elsasa.Sis.Shared.Database;
using Microsoft.Data.SqlClient;

/// <summary>
/// Autonomous seeder for the Academics domain activities (ADR-002, ADR-004, TD-02, TD-05, OQ-003, OQ-009, OQ-010).
/// Establishes class cohort participations, attendance records, coursework assignments,
/// submissions, historical Semester 1 certified report cards and cumulative transcripts,
/// and Semester 2 live interactive hooks for flagship personas.
/// </summary>
public sealed class AcademicsActivitiesDemoDataSeeder(
    ISqlConnectionFactory connectionFactory) : IAcademicsActivitiesDemoDataSeeder
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerOptions.Default)
    {
        WriteIndented = false
    };

    private static readonly JsonSerializerOptions _webOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private const string _createdBy = "demo-data-seeder";

    // -------------------------------------------------------------------------
    // SQL templates
    // -------------------------------------------------------------------------

    private const string _insertParticipationSql = """
        INSERT INTO academics.academic_participations (
            id, student_id, learning_unit_id, academic_period_id, academic_period_label,
            participation_type, attempt_number, source, section_label, status,
            start_date, end_date, completion_date, created_by
        ) VALUES (
            @Id, @StudentId, @LearningUnitId, @AcademicPeriodId, @AcademicPeriodLabel,
            @ParticipationType, @AttemptNumber, @Source, @SectionLabel, @Status,
            @StartDate, @EndDate, @CompletionDate, @CreatedBy
        );
        """;

    private const string _insertLearningActivitySql = """
        INSERT INTO academics.learning_activities (
            id, learning_unit_id, activity_type, title, activity_date, status,
            is_attendance_required, created_by
        ) VALUES (
            @Id, @LearningUnitId, @ActivityType, @Title, @ActivityDate, @Status,
            @IsAttendanceRequired, @CreatedBy
        );
        """;

    private const string _insertAttendanceSql = """
        INSERT INTO academics.attendance_records (
            id, learning_activity_id, student_id, status, recorded_by, recorded_date,
            created_by
        ) VALUES (
            @Id, @LearningActivityId, @StudentId, @Status, @RecordedBy, @RecordedDate,
            @CreatedBy
        );
        """;

    private const string _insertAssessmentSql = """
        INSERT INTO academics.assessments (
            id, learning_unit_id, title, assessment_type, assessment_method,
            max_score, passing_score, reassessment_policy, status, assessment_date,
            created_by
        ) VALUES (
            @Id, @LearningUnitId, @Title, @AssessmentType, @AssessmentMethod,
            @MaxScore, @PassingScore, @ReassessmentPolicy, @Status, @AssessmentDate,
            @CreatedBy
        );
        """;

    private const string _insertAssignmentSql = """
        INSERT INTO academics.assignments (
            id, learning_activity_id, title, description, release_date, due_date,
            status, created_by
        ) VALUES (
            @Id, @LearningActivityId, @Title, @Description, @ReleaseDate, @DueDate,
            @Status, @CreatedBy
        );
        """;

    private const string _insertSubmissionSql = """
        INSERT INTO academics.submissions (
            id, assignment_id, student_id, status, submitted_date,
            is_resubmission, created_by
        ) VALUES (
            @Id, @AssignmentId, @StudentId, @Status, @SubmittedDate,
            @IsResubmission, @CreatedBy
        );
        """;

    private const string _insertEvidenceSql = """
        INSERT INTO academics.assessment_evidence (
            id, assessment_id, student_id, evidence_type, collected_date,
            evidence_reference, created_by
        ) VALUES (
            @Id, @AssessmentId, @StudentId, @EvidenceType, @CollectedDate,
            @EvidenceReference, @CreatedBy
        );
        """;

    private const string _insertResultSql = """
        INSERT INTO academics.academic_results (
            id, assessment_id, student_id, attempt_number, replaces_result_id,
            is_final, score_numeric, score_max, grade_letter, grade_label,
            evaluation_model, evaluated_by, evaluated_date, result_status,
            approved_by, approved_date, published_date, notes, created_by
        ) VALUES (
            @Id, @AssessmentId, @StudentId, @AttemptNumber, @ReplacesResultId,
            @IsFinal, @ScoreNumeric, @ScoreMax, @GradeLetter, @GradeLabel,
            @EvaluationModel, @EvaluatedBy, @EvaluatedDate, @ResultStatus,
            @ApprovedBy, @ApprovedDate, @PublishedDate, @Notes, @CreatedBy
        );
        """;

    private const string _updateResultSql = """
        UPDATE academics.academic_results SET
            attempt_number = @AttemptNumber,
            replaces_result_id = @ReplacesResultId,
            is_final = @IsFinal,
            score_numeric = @ScoreNumeric,
            score_max = @ScoreMax,
            grade_letter = @GradeLetter,
            grade_label = @GradeLabel,
            evaluation_model = @EvaluationModel,
            evaluated_by = @EvaluatedBy,
            evaluated_date = @EvaluatedDate,
            result_status = @ResultStatus,
            approved_by = @ApprovedBy,
            approved_date = @ApprovedDate,
            published_date = @PublishedDate,
            notes = @Notes
        WHERE id = @Id;
        """;

    private const string _insertReportCardSql = """
        INSERT INTO academics.report_cards (
            id, student_id, academic_period_id, academic_period_label,
            report_type, template_name, status, approved_by, approved_date,
            published_date, narrative_comments, content_snapshot, created_by
        ) VALUES (
            @Id, @StudentId, @AcademicPeriodId, @AcademicPeriodLabel,
            @ReportType, @TemplateName, @Status, @ApprovedBy, @ApprovedDate,
            @PublishedDate, @NarrativeComments, @ContentSnapshot, @CreatedBy
        );
        """;

    private const string _insertReportCardEntrySql = """
        INSERT INTO academics.report_card_entries (
            id, report_card_id, learning_unit_id, academic_result_id,
            display_order, teacher_comment, created_by
        ) VALUES (
            @Id, @ReportCardId, @LearningUnitId, @AcademicResultId,
            @DisplayOrder, @TeacherComment, @CreatedBy
        );
        """;

    private const string _insertTranscriptSql = """
        INSERT INTO academics.transcripts (
            id, student_id, transcript_type, period_start, period_end,
            status, approved_by, approved_date, issued_date, purpose,
            content_snapshot, created_by
        ) VALUES (
            @Id, @StudentId, @TranscriptType, @PeriodStart, @PeriodEnd,
            @Status, @ApprovedBy, @ApprovedDate, @IssuedDate, @Purpose,
            @ContentSnapshot, @CreatedBy
        );
        """;

    private const string _insertLearningProgressSql = """
        INSERT INTO academics.learning_progress (
            id, student_id, learning_unit_id, academic_period_id, status,
            basis, completion_date, notes, created_at, created_by
        ) VALUES (
            @Id, @StudentId, @LearningUnitId, @AcademicPeriodId, @Status,
            @Basis, @CompletionDate, @Notes, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertAcademicStandingSql = """
        INSERT INTO academics.academic_standings (
            id, student_id, academic_period_id, standing, policy_reference,
            conditions, is_exception, exception_reason, committee_reference,
            authorized_by, determined_at, created_at, created_by
        ) VALUES (
            @Id, @StudentId, @AcademicPeriodId, @Standing, @PolicyReference,
            @Conditions, @IsException, @ExceptionReason, @CommitteeReference,
            @AuthorizedBy, @DeterminedAt, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertPromotionRecommendationSql = """
        INSERT INTO academics.promotion_recommendations (
            id, student_id, academic_period_id, academic_standing_id,
            recommendation, from_level, recommended_to_level, conditions,
            supporting_evidence, is_exception, exception_reason,
            committee_reference, authorized_by, issued_at, created_at, created_by
        ) VALUES (
            @Id, @StudentId, @AcademicPeriodId, @AcademicStandingId,
            @Recommendation, @FromLevel, @RecommendedToLevel, @Conditions,
            @SupportingEvidence, @IsException, @ExceptionReason,
            @CommitteeReference, @AuthorizedBy, @IssuedAt, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertInterventionSql = """
        INSERT INTO academics.academic_interventions (
            id, student_id, learning_unit_id, academic_period_id,
            intervention_type, trigger_type, trigger_description,
            plan_description, status, initiated_by, initiated_at,
            concluded_at, outcome_notes, created_at, created_by
        ) VALUES (
            @Id, @StudentId, @LearningUnitId, @AcademicPeriodId,
            @InterventionType, @TriggerType, @TriggerDescription,
            @PlanDescription, @Status, @InitiatedBy, @InitiatedAt,
            @ConcludedAt, @OutcomeNotes, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertAchievementSql = """
        INSERT INTO academics.academic_achievements (
            id, student_id, learning_unit_id, academic_period_id,
            achievement_type, title, description, criteria_met,
            supporting_evidence, recognized_by, recognized_at,
            created_at, created_by
        ) VALUES (
            @Id, @StudentId, @LearningUnitId, @AcademicPeriodId,
            @AchievementType, @Title, @Description, @CriteriaMet,
            @SupportingEvidence, @RecognizedBy, @RecognizedAt,
            @CreatedAt, @CreatedBy
        );
        """;

    // Submodule 5: Student Learning Plans SQL templates (IS013, P4-S12)
    private const string _insertStudentLearningPlanSql = """
        INSERT INTO academics.student_learning_plans (
            id, student_id, academic_period_id, academic_period_label,
            status, version, notes, created_at, created_by, updated_at, updated_by
        ) VALUES (
            @Id, @StudentId, @AcademicPeriodId, @AcademicPeriodLabel,
            @Status, @Version, @Notes, @CreatedAt, @CreatedBy, @UpdatedAt, @UpdatedBy
        );
        """;

    private const string _insertSlpLearningUnitSql = """
        INSERT INTO academics.slp_learning_units (
            id, student_learning_plan_id, learning_unit_id, sequence_order,
            is_required, notes, created_at, created_by
        ) VALUES (
            @Id, @StudentLearningPlanId, @LearningUnitId, @SequenceOrder,
            @IsRequired, @Notes, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertAcademicPlanAdjustmentSql = """
        INSERT INTO academics.academic_plan_adjustments (
            id, student_learning_plan_id, adjustment_type, description,
            rationale, effective_date, end_date, approved_by, approved_date,
            notes, created_at, created_by
        ) VALUES (
            @Id, @StudentLearningPlanId, @AdjustmentType, @Description,
            @Rationale, @EffectiveDate, @EndDate, @ApprovedBy, @ApprovedDate,
            @Notes, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertSlpApprovalSql = """
        INSERT INTO academics.slp_approvals (
            id, student_learning_plan_id, plan_version, approved_by,
            approved_date, approval_type, notes, created_at, created_by
        ) VALUES (
            @Id, @StudentLearningPlanId, @PlanVersion, @ApprovedBy,
            @ApprovedDate, @ApprovalType, @Notes, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertParticipationWithSlpSql = """
        INSERT INTO academics.academic_participations (
            id, student_id, learning_unit_id, academic_period_id, academic_period_label,
            participation_type, attempt_number, source, section_label, status,
            start_date, end_date, completion_date, student_learning_plan_id, created_by
        ) VALUES (
            @Id, @StudentId, @LearningUnitId, @AcademicPeriodId, @AcademicPeriodLabel,
            @ParticipationType, @AttemptNumber, @Source, @SectionLabel, @Status,
            @StartDate, @EndDate, @CompletionDate, @StudentLearningPlanId, @CreatedBy
        );
        """;

    private const string _insertLearningUnitSql = """
        INSERT INTO academics.learning_units (
            id, learning_structure_id, parent_unit_id, code, name, unit_type,
            level, description, credit_weight, status, notes,
            created_at, created_by
        ) VALUES (
            @Id, @LearningStructureId, @ParentUnitId, @Code, @Name, @UnitType,
            @Level, @Description, @CreditWeight, @Status, @Notes,
            @CreatedAt, @CreatedBy
        );
        """;

    // Flagship student personas for Submodule 4 Standing & Progression (TD-08, GAP-008)
    public static readonly Guid RinaKusumaStudentId = new("00000000-0000-0000-0004-000000000108");
    public static readonly Guid BudiSantosoStudentId = DemoDataConstants.BudiStudentId;
    public static readonly Guid TimotiusStudentId = DemoDataConstants.TimotiusApplicantId;

    // Deterministic constants for Submodule 5 SLP Personas (IS013, P4-S12)
    public static readonly Guid BudiSlpV1Id = new("00000000-0000-0000-0006-000000000201");
    public static readonly Guid BudiSlpV2Id = new("00000000-0000-0000-0006-000000000202");
    public static readonly Guid TimotiusSlpV1Id = new("00000000-0000-0000-0006-000000000203");

    public static readonly Guid LuMat3S1Id = new("00000000-0000-0000-0006-000000000101");
    public static readonly Guid LuBi4S1Id = new("00000000-0000-0000-0006-000000000102");
    public static readonly Guid LuPjok4S1Id = new("00000000-0000-0000-0006-000000000103");

    // -------------------------------------------------------------------------
    // Seed entry point
    // -------------------------------------------------------------------------

    public async Task<AcademicsActivitiesSeedResult> SeedAcademicsActivitiesAsync(
        OperationsSeedResult operationsContext,
        AcademicsStructureSeedResult academicsContext,
        StudentsSeedResult studentsContext,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var sem1Id = operationsContext.HistoricalPeriodId;
        var sem2Id = operationsContext.ActivePeriodId;
        var baseDate = operationsContext.BaseDate;

        // Date ranges for semesters
        var sem1StartDate = baseDate.AddDays(-180);
        var sem1EndDate = baseDate.AddDays(-35);
        var sem1Label = $"Semester 1 {operationsContext.AcademicYearCode}";

        var sem2StartDate = baseDate.AddDays(-30);
        var sem2EndDate = baseDate.AddDays(120);
        var sem2Label = $"Semester 2 {operationsContext.AcademicYearCode}";

        var enrolledStudentIds = studentsContext.EnrolledStudentIds;
        var jonathanId = studentsContext.Personas.JonathanStudentId;
        var graceId = studentsContext.Personas.GraceStudentId;
        var budiId = studentsContext.Personas.BudiStudentId;

        // Collect PACE units (unit_type = 'pace')
        var paceUnitIds = academicsContext.PaceUnitIds; // 30 PACE unit IDs

        // Build a lookup of PACE units by subject for selection
        var random = new Random(42);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var unitLookup = (await connection.QueryAsync<(Guid Id, string Code, string Name)>(
                new CommandDefinition(
                    "SELECT id, code, name FROM academics.learning_units",
                    transaction: transaction,
                    cancellationToken: cancellationToken)))
                .ToDictionary(u => u.Id);

            var studentsList = new List<(Guid Id, string FullName, string? GovId, string CurrentStatus)>();
            var studentLookup = new Dictionary<Guid, (string Name, string? Nisn)>();
            var activeStudentSet = new HashSet<Guid>();
            try
            {
                var students = await connection.QueryAsync<(Guid Id, string FullName, string? GovId, string CurrentStatus)>(
                    new CommandDefinition(
                        "SELECT id, full_name, government_id_number, current_status FROM students.students",
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                foreach (var s in students)
                {
                    studentsList.Add(s);
                    studentLookup[s.Id] = (s.FullName, s.GovId);
                    if (string.Equals(s.CurrentStatus, "active", StringComparison.OrdinalIgnoreCase))
                    {
                        activeStudentSet.Add(s.Id);
                    }
                }
            }
            catch
            {
                // Fallback if students table query is not available
            }

            var rinaStudent = studentsList.FirstOrDefault(s => s.FullName.Contains("Rina", StringComparison.OrdinalIgnoreCase));
            var rinaId = rinaStudent != default && rinaStudent.Id != Guid.Empty ? rinaStudent.Id : RinaKusumaStudentId;

            var budiStudent = studentsList.FirstOrDefault(s => s.FullName.Contains("Budi", StringComparison.OrdinalIgnoreCase));
            var budiStudentId = budiStudent != default && budiStudent.Id != Guid.Empty ? budiStudent.Id : BudiSantosoStudentId;

            var timotiusId = TimotiusStudentId;

            if (!studentLookup.ContainsKey(rinaId)) studentLookup[rinaId] = ("Rina Kusuma", "0061234567");
            if (!studentLookup.ContainsKey(budiStudentId)) studentLookup[budiStudentId] = ("Budi Santoso", "0071234567");
            studentLookup[timotiusId] = ("Timotius", "0051234567");

            // Enrolled students only (48 students) so there are 0 FK orphans
            var allTargetStudentIds = enrolledStudentIds.ToList();
            var activeStudentIds = allTargetStudentIds
                .Where(id => activeStudentSet.Count == 0 || activeStudentSet.Contains(id))
                .Take(45)
                .ToList();

            // Select campus-east active students if available so at-risk/standing spans campuses
            var eastCampusStudentIds = new HashSet<Guid>();
            try
            {
                var eastIds = await connection.QueryAsync<Guid>(
                    new CommandDefinition(
                        "SELECT student_id FROM students.administrative_placements WHERE is_current = 1 AND campus = 'campus-east'",
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                foreach (var eid in eastIds) eastCampusStudentIds.Add(eid);
            }
            catch
            {
                // Ignore if cross-schema table unavailable
            }

            var nonFlagshipActiveIds = activeStudentIds
                .Where(id => id != jonathanId && id != graceId && id != budiStudentId && id != rinaId)
                .ToList();

            // Pick 5 unaddressed at-risk learners (including 1 on campus-east if present)
            var eastCandidate = nonFlagshipActiveIds.FirstOrDefault(id => eastCampusStudentIds.Contains(id));
            var orderedNonFlagship = eastCandidate != Guid.Empty
                ? new[] { eastCandidate }.Concat(nonFlagshipActiveIds.Where(id => id != eastCandidate)).ToList()
                : nonFlagshipActiveIds;

            var unaddressedConditionalIds = orderedNonFlagship.Take(3).ToList();
            var unaddressedNotInGoodStandingIds = orderedNonFlagship.Skip(3).Take(2).ToList();
            var s1ConditionalImprovedIds = orderedNonFlagship.Skip(5).Take(1).ToList();
            var s1NotInGoodImprovedIds = orderedNonFlagship.Skip(6).Take(1).ToList();
            var chronicAbsenteeismIds = new HashSet<Guid>(
                unaddressedNotInGoodStandingIds.Concat(unaddressedConditionalIds.Take(1)));

            var mathUnits = paceUnitIds
                .Where(id => unitLookup.TryGetValue(id, out var u) && (u.Code.StartsWith("M", StringComparison.OrdinalIgnoreCase) || u.Name.Contains("Math", StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (mathUnits.Count == 0) mathUnits = paceUnitIds.Take(6).ToList();

            var bibleUnits = paceUnitIds
                .Where(id => unitLookup.TryGetValue(id, out var u) && (u.Code.StartsWith("B", StringComparison.OrdinalIgnoreCase) || u.Name.Contains("Bible", StringComparison.OrdinalIgnoreCase) || u.Name.Contains("Christian", StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (bibleUnits.Count == 0) bibleUnits = paceUnitIds.Skip(24).Take(6).ToList();

            (string Code, string Name) GetUnitInfo(Guid unitId)
            {
                return unitLookup.TryGetValue(unitId, out var u)
                    ? (u.Code, u.Name)
                    : ($"PACE-{unitId:D}", $"PACE Unit {unitId:D}");
            }

            (string Name, string? Nisn) GetStudentInfo(Guid studentId)
            {
                if (studentId == rinaId) return ("Rina Kusuma", "0061234567");
                if (studentId == budiStudentId) return ("Budi Santoso", "0071234567");
                if (studentId == timotiusId) return ("Timotius", "0051234567");
                if (studentLookup.TryGetValue(studentId, out var info) && !string.IsNullOrWhiteSpace(info.Name))
                {
                    return info;
                }
                if (studentId == jonathanId) return ("Jonathan Pratama", "0091234567");
                if (studentId == graceId) return ("Grace Santoso", "0081234567");
                if (studentId == budiId) return ("Budi Santoso", "0071234567");
                return ($"Student {studentId.ToString()[..8]}", null);
            }

            int participationsCreated = 0;
            int attendanceRecordsCreated = 0;
            int resultsCreated = 0;
            int reportCardsCreated = 0;
            int transcriptsCreated = 0;

            // ====================================================================
            // 1. Create learning activities for S1 and S2 (attendance tracking)
            // ====================================================================
            var sem1ActivityIds = new List<Guid>();
            var sem2ActivityIds = new List<Guid>();
            var learningActivityRows = new List<object>(60);

            // S1: Create 30 learning activities spread across the semester
            for (int i = 0; i < 30; i++)
            {
                var unitId = paceUnitIds[i % paceUnitIds.Count];
                var activityId = Guid.NewGuid();
                sem1ActivityIds.Add(activityId);

                learningActivityRows.Add(new
                {
                    Id = activityId,
                    LearningUnitId = unitId,
                    ActivityType = "lesson",
                    Title = $"S1 Lesson {i + 1}",
                    ActivityDate = sem1StartDate.AddDays((i * 5) % 100),
                    Status = "conducted",
                    IsAttendanceRequired = true,
                    CreatedBy = _createdBy
                });
            }

            // S2: Create 30 learning activities spread across the semester
            for (int i = 0; i < 30; i++)
            {
                var unitId = paceUnitIds[(i + 10) % paceUnitIds.Count];
                var activityId = Guid.NewGuid();
                sem2ActivityIds.Add(activityId);

                learningActivityRows.Add(new
                {
                    Id = activityId,
                    LearningUnitId = unitId,
                    ActivityType = "lesson",
                    Title = $"S2 Lesson {i + 1}",
                    ActivityDate = sem2StartDate.AddDays((i * 5) % 28),
                    Status = "conducted",
                    IsAttendanceRequired = true,
                    CreatedBy = _createdBy
                });
            }

            await connection.ExecuteAsync(new CommandDefinition(
                _insertLearningActivitySql,
                learningActivityRows,
                transaction: transaction,
                cancellationToken: cancellationToken));

            // ====================================================================
            // 2. Create academic participations for all 48 enrolled students (batched)
            //    S1: completed participations (historical)
            //    S2: in_progress participations (active)
            // ====================================================================
            var allParticipationIds = new Dictionary<Guid, List<Guid>>();
            var allS1ParticipationUnitLinks = new Dictionary<Guid, List<(Guid participationId, Guid unitId)>>();
            var allS2ParticipationUnitLinks = new Dictionary<Guid, List<(Guid participationId, Guid unitId)>>();
            var participationRows = new List<ParticipationBatchRow>(allTargetStudentIds.Count * paceUnitIds.Count * 2);

            foreach (var studentId in allTargetStudentIds)
            {
                var s1ParticipationIds = new List<Guid>();
                var s2ParticipationIds = new List<Guid>();
                var s1Links = new List<(Guid participationId, Guid unitId)>();
                var s2Links = new List<(Guid participationId, Guid unitId)>();
                var sectionLabel = GetSectionLabel(studentId, allTargetStudentIds, random, rinaId, budiStudentId, timotiusId);

                // S1: Complete all 30 PACE units as completed participations
                for (int i = 0; i < paceUnitIds.Count; i++)
                {
                    var participationId = Guid.NewGuid();
                    s1ParticipationIds.Add(participationId);
                    s1Links.Add((participationId, paceUnitIds[i]));

                    var startDate = sem1StartDate.AddDays(random.Next(0, 30));
                    var endDate = sem1EndDate.AddDays(-random.Next(0, 15));
                    if (endDate < startDate) endDate = startDate.AddDays(1);

                    participationRows.Add(new ParticipationBatchRow(
                        Id: participationId,
                        StudentId: studentId,
                        LearningUnitId: paceUnitIds[i],
                        AcademicPeriodId: sem1Id,
                        AcademicPeriodLabel: sem1Label,
                        ParticipationType: "standard",
                        AttemptNumber: 1,
                        Source: "assigned",
                        SectionLabel: sectionLabel,
                        Status: "completed",
                        StartDate: startDate,
                        EndDate: endDate,
                        CompletionDate: endDate));
                }

                // S2: In-progress participations in all 30 PACE units
                for (int i = 0; i < paceUnitIds.Count; i++)
                {
                    var participationId = Guid.NewGuid();
                    s2ParticipationIds.Add(participationId);
                    s2Links.Add((participationId, paceUnitIds[i]));

                    participationRows.Add(new ParticipationBatchRow(
                        Id: participationId,
                        StudentId: studentId,
                        LearningUnitId: paceUnitIds[i],
                        AcademicPeriodId: sem2Id,
                        AcademicPeriodLabel: sem2Label,
                        ParticipationType: "standard",
                        AttemptNumber: 1,
                        Source: "assigned",
                        SectionLabel: sectionLabel,
                        Status: "in_progress",
                        StartDate: sem2StartDate,
                        EndDate: null,
                        CompletionDate: null));
                }

                allParticipationIds[studentId] = s1ParticipationIds.Concat(s2ParticipationIds).ToList();
                allS1ParticipationUnitLinks[studentId] = s1Links;
                allS2ParticipationUnitLinks[studentId] = s2Links;
                participationsCreated += s1ParticipationIds.Count + s2ParticipationIds.Count;
            }

            await BatchInsertAcademicParticipationsAsync(connection, transaction, participationRows, _createdBy, cancellationToken);

            // ====================================================================
            // 3. Create ~1,200 attendance records across S1 and S2 (batched)
            //    S1: 700 records, S2: 499 records (with 3 chronic absenteeism < 80%)
            // ====================================================================
            attendanceRecordsCreated += await CreateS1AttendanceRecordsAsync(
                connection, transaction, sem1ActivityIds, allTargetStudentIds,
                jonathanId, sem1StartDate, _createdBy, random, targetCount: 700,
                cancellationToken);

            attendanceRecordsCreated += await CreateS2AttendanceRecordsAsync(
                connection, transaction, sem2ActivityIds, activeStudentIds,
                jonathanId, chronicAbsenteeismIds, sem2StartDate, _createdBy, random,
                cancellationToken);

            // ====================================================================
            // 4. Create PACE test assessments and academic results (batched, < 200 total)
            //    S1: 112 certified published results (6 for flagships+Rina, 2 for others)
            //    S2: 51 results (3 provisional with is_final=1, 6 approved, 41 published, + Grace Attempt 1)
            // ====================================================================
            var allS1Results = new Dictionary<Guid, List<(Guid resultId, Guid assessmentId, Guid unitId, decimal score, string gradeLetter, string gradeLabel)>>();
            var assessmentRows = new List<object>(170);
            var resultRows = new List<object>(170);
            var evidenceRows = new List<object>(120);

            foreach (var studentId in allTargetStudentIds)
            {
                var s1Links = allS1ParticipationUnitLinks[studentId];
                var isHighlight = studentId == jonathanId || studentId == graceId || studentId == budiStudentId || studentId == rinaId;
                var selectedS1Links = s1Links.Take(isHighlight ? 6 : 2).ToList();
                var s1Results = new List<(Guid resultId, Guid assessmentId, Guid unitId, decimal score, string gradeLetter, string gradeLabel)>();

                foreach (var (_, unitId) in selectedS1Links)
                {
                    var assessmentId = Guid.NewGuid();
                    var evaluatedDate = sem1EndDate.AddDays(-random.Next(0, 10));

                    assessmentRows.Add(new
                    {
                        Id = assessmentId,
                        LearningUnitId = unitId,
                        Title = $"PACE Test - {GetUnitInfo(unitId).Code}",
                        AssessmentType = "summative",
                        AssessmentMethod = "written_test",
                        MaxScore = 100.00m,
                        PassingScore = 80.00m,
                        ReassessmentPolicy = "capped_at_passing",
                        Status = "conducted",
                        AssessmentDate = evaluatedDate,
                        CreatedBy = _createdBy
                    });

                    var score = GenerateS1Score(studentId, unitId, random, rinaId, budiStudentId, timotiusId, mathUnits, bibleUnits);
                    var resultId = Guid.NewGuid();
                    var isPassing = score >= 80.0m;
                    var gradeLetter = GetGradeLetter(score);
                    var gradeLabel = GetGradeLabel(score);

                    resultRows.Add(new
                    {
                        Id = resultId,
                        AssessmentId = assessmentId,
                        StudentId = studentId,
                        AttemptNumber = 1,
                        ReplacesResultId = (Guid?)null,
                        IsFinal = 1,
                        ScoreNumeric = (decimal?)score,
                        ScoreMax = 100.00m,
                        GradeLetter = (string?)gradeLetter,
                        GradeLabel = (string?)gradeLabel,
                        EvaluationModel = "numeric",
                        EvaluatedBy = "academic-coordinator",
                        EvaluatedDate = evaluatedDate,
                        ResultStatus = "published",
                        ApprovedBy = (string?)"Academic Authority",
                        ApprovedDate = (DateTime?)evaluatedDate.AddDays(1),
                        PublishedDate = (DateTime?)evaluatedDate.AddDays(2),
                        Notes = isPassing ? "Mastery achieved (KKM 80%)" : "Below KKM - requires remediation",
                        CreatedBy = _createdBy
                    });

                    evidenceRows.Add(new
                    {
                        Id = Guid.NewGuid(),
                        AssessmentId = assessmentId,
                        StudentId = studentId,
                        EvidenceType = "test_paper",
                        CollectedDate = evaluatedDate,
                        EvidenceReference = $"evidence/s1/{studentId}/{assessmentId}/paper.pdf",
                        CreatedBy = _createdBy
                    });

                    s1Results.Add((resultId, assessmentId, unitId, score, gradeLetter, gradeLabel));
                    resultsCreated++;
                }

                allS1Results[studentId] = s1Results;
            }

            // --- S2: Active results with exactly 3 provisional (is_final = 1) + flagship hooks ---
            var nonFlagshipProvisionalStudentId = orderedNonFlagship.FirstOrDefault();
            int nonFlagshipApprovedCounter = 0;

            foreach (var studentId in allTargetStudentIds)
            {
                var s2Links = allS2ParticipationUnitLinks[studentId];

                if (studentId == jonathanId)
                {
                    // Result 1: provisional awaiting approval (provisional #1)
                    var a1 = Guid.NewGuid();
                    var d1 = baseDate.AddDays(-2);
                    assessmentRows.Add(new
                    {
                        Id = a1,
                        LearningUnitId = s2Links[0].unitId,
                        Title = $"PACE Test - {GetUnitInfo(s2Links[0].unitId).Code}",
                        AssessmentType = "summative",
                        AssessmentMethod = "written_test",
                        MaxScore = 100.00m,
                        PassingScore = 80.00m,
                        ReassessmentPolicy = "capped_at_passing",
                        Status = "conducted",
                        AssessmentDate = d1,
                        CreatedBy = _createdBy
                    });
                    resultRows.Add(new
                    {
                        Id = Guid.NewGuid(),
                        AssessmentId = a1,
                        StudentId = jonathanId,
                        AttemptNumber = 1,
                        ReplacesResultId = (Guid?)null,
                        IsFinal = 1,
                        ScoreNumeric = (decimal?)92.00m,
                        ScoreMax = 100.00m,
                        GradeLetter = (string?)"A",
                        GradeLabel = (string?)"Sangat Mahir",
                        EvaluationModel = "numeric",
                        EvaluatedBy = "teacher-01",
                        EvaluatedDate = d1,
                        ResultStatus = "provisional",
                        ApprovedBy = (string?)null,
                        ApprovedDate = (DateTime?)null,
                        PublishedDate = (DateTime?)null,
                        Notes = "PACE score awaiting supervisor approval (live hook for SCR-ACD-03)",
                        CreatedBy = _createdBy
                    });
                    resultsCreated++;

                    // Result 2: published S2 result
                    var a2 = Guid.NewGuid();
                    var d2 = baseDate.AddDays(-6);
                    assessmentRows.Add(new
                    {
                        Id = a2,
                        LearningUnitId = s2Links[1].unitId,
                        Title = $"PACE Test - {GetUnitInfo(s2Links[1].unitId).Code}",
                        AssessmentType = "summative",
                        AssessmentMethod = "written_test",
                        MaxScore = 100.00m,
                        PassingScore = 80.00m,
                        ReassessmentPolicy = "capped_at_passing",
                        Status = "conducted",
                        AssessmentDate = d2,
                        CreatedBy = _createdBy
                    });
                    resultRows.Add(new
                    {
                        Id = Guid.NewGuid(),
                        AssessmentId = a2,
                        StudentId = jonathanId,
                        AttemptNumber = 1,
                        ReplacesResultId = (Guid?)null,
                        IsFinal = 1,
                        ScoreNumeric = (decimal?)94.00m,
                        ScoreMax = 100.00m,
                        GradeLetter = (string?)"A",
                        GradeLabel = (string?)"Sangat Mahir",
                        EvaluationModel = "numeric",
                        EvaluatedBy = "teacher-01",
                        EvaluatedDate = d2,
                        ResultStatus = "published",
                        ApprovedBy = (string?)"Academic Authority",
                        ApprovedDate = (DateTime?)d2.AddDays(1),
                        PublishedDate = (DateTime?)d2.AddDays(2),
                        Notes = "Mastery achieved (KKM 80%)",
                        CreatedBy = _createdBy
                    });
                    resultsCreated++;
                }
                else if (studentId == graceId)
                {
                    // Attempt 1: 68% failing (is_final = 0)
                    var a1 = Guid.NewGuid();
                    var r1 = Guid.NewGuid();
                    var d1 = baseDate.AddDays(-10);
                    assessmentRows.Add(new
                    {
                        Id = a1,
                        LearningUnitId = s2Links[0].unitId,
                        Title = $"PACE Test - {GetUnitInfo(s2Links[0].unitId).Code}",
                        AssessmentType = "summative",
                        AssessmentMethod = "written_test",
                        MaxScore = 100.00m,
                        PassingScore = 80.00m,
                        ReassessmentPolicy = "capped_at_passing",
                        Status = "conducted",
                        AssessmentDate = d1,
                        CreatedBy = _createdBy
                    });
                    resultRows.Add(new
                    {
                        Id = r1,
                        AssessmentId = a1,
                        StudentId = graceId,
                        AttemptNumber = 1,
                        ReplacesResultId = (Guid?)null,
                        IsFinal = 0,
                        ScoreNumeric = (decimal?)68.00m,
                        ScoreMax = 100.00m,
                        GradeLetter = (string?)"D",
                        GradeLabel = (string?)"Kurang",
                        EvaluationModel = "numeric",
                        EvaluatedBy = "teacher-01",
                        EvaluatedDate = d1,
                        ResultStatus = "approved",
                        ApprovedBy = (string?)"Academic Authority",
                        ApprovedDate = (DateTime?)d1.AddDays(1),
                        PublishedDate = (DateTime?)null,
                        Notes = "Initial attempt - below KKM (68%), failed. Remediation completed.",
                        CreatedBy = _createdBy
                    });
                    resultsCreated++;

                    // Attempt 2: 85% passing (is_final = 1, replaces Attempt 1)
                    var a2 = Guid.NewGuid();
                    var d2 = baseDate.AddDays(-5);
                    assessmentRows.Add(new
                    {
                        Id = a2,
                        LearningUnitId = s2Links[1].unitId,
                        Title = $"PACE Test - {GetUnitInfo(s2Links[1].unitId).Code}",
                        AssessmentType = "summative",
                        AssessmentMethod = "written_test",
                        MaxScore = 100.00m,
                        PassingScore = 80.00m,
                        ReassessmentPolicy = "capped_at_passing",
                        Status = "conducted",
                        AssessmentDate = d2,
                        CreatedBy = _createdBy
                    });
                    resultRows.Add(new
                    {
                        Id = Guid.NewGuid(),
                        AssessmentId = a2,
                        StudentId = graceId,
                        AttemptNumber = 2,
                        ReplacesResultId = (Guid?)r1,
                        IsFinal = 1,
                        ScoreNumeric = (decimal?)85.00m,
                        ScoreMax = 100.00m,
                        GradeLetter = (string?)"B+",
                        GradeLabel = (string?)"Memuaskan",
                        EvaluationModel = "numeric",
                        EvaluatedBy = "teacher-01",
                        EvaluatedDate = d2,
                        ResultStatus = "published",
                        ApprovedBy = (string?)"Academic Authority",
                        ApprovedDate = (DateTime?)d2.AddDays(1),
                        PublishedDate = (DateTime?)d2.AddDays(2),
                        Notes = "Retake attempt - remediation completed, score 85% (passing).",
                        CreatedBy = _createdBy
                    });
                    resultsCreated++;
                }
                else if (studentId == budiStudentId)
                {
                    // Result 1: Science PACE 1049 submitted awaiting grading (provisional #2, score_numeric = null)
                    var a1 = Guid.NewGuid();
                    var d1 = baseDate.AddDays(-2);
                    assessmentRows.Add(new
                    {
                        Id = a1,
                        LearningUnitId = s2Links[0].unitId,
                        Title = "PACE Test - Science PACE 1049",
                        AssessmentType = "summative",
                        AssessmentMethod = "written_test",
                        MaxScore = 100.00m,
                        PassingScore = 80.00m,
                        ReassessmentPolicy = "capped_at_passing",
                        Status = "conducted",
                        AssessmentDate = d1,
                        CreatedBy = _createdBy
                    });
                    resultRows.Add(new
                    {
                        Id = Guid.NewGuid(),
                        AssessmentId = a1,
                        StudentId = budiStudentId,
                        AttemptNumber = 1,
                        ReplacesResultId = (Guid?)null,
                        IsFinal = 1,
                        ScoreNumeric = (decimal?)null,
                        ScoreMax = 100.00m,
                        GradeLetter = (string?)null,
                        GradeLabel = (string?)null,
                        EvaluationModel = "numeric",
                        EvaluatedBy = "teacher-01",
                        EvaluatedDate = d1,
                        ResultStatus = "provisional",
                        ApprovedBy = (string?)null,
                        ApprovedDate = (DateTime?)null,
                        PublishedDate = (DateTime?)null,
                        Notes = "Live submitted PACE test awaiting supervisor evaluation (live hook for SCR-ACD-04)",
                        CreatedBy = _createdBy
                    });
                    resultsCreated++;

                    // Result 2: published S2 result
                    var a2 = Guid.NewGuid();
                    var d2 = baseDate.AddDays(-7);
                    assessmentRows.Add(new
                    {
                        Id = a2,
                        LearningUnitId = s2Links[1].unitId,
                        Title = $"PACE Test - {GetUnitInfo(s2Links[1].unitId).Code}",
                        AssessmentType = "summative",
                        AssessmentMethod = "written_test",
                        MaxScore = 100.00m,
                        PassingScore = 80.00m,
                        ReassessmentPolicy = "capped_at_passing",
                        Status = "conducted",
                        AssessmentDate = d2,
                        CreatedBy = _createdBy
                    });
                    resultRows.Add(new
                    {
                        Id = Guid.NewGuid(),
                        AssessmentId = a2,
                        StudentId = budiStudentId,
                        AttemptNumber = 1,
                        ReplacesResultId = (Guid?)null,
                        IsFinal = 1,
                        ScoreNumeric = (decimal?)88.00m,
                        ScoreMax = 100.00m,
                        GradeLetter = (string?)"B+",
                        GradeLabel = (string?)"Mahir",
                        EvaluationModel = "numeric",
                        EvaluatedBy = "teacher-01",
                        EvaluatedDate = d2,
                        ResultStatus = "published",
                        ApprovedBy = (string?)"Academic Authority",
                        ApprovedDate = (DateTime?)d2.AddDays(1),
                        PublishedDate = (DateTime?)d2.AddDays(2),
                        Notes = "Mastery achieved (KKM 80%)",
                        CreatedBy = _createdBy
                    });
                    resultsCreated++;
                }
                else
                {
                    var a1 = Guid.NewGuid();
                    var d1 = baseDate.AddDays(-random.Next(2, 14));
                    var unitId = s2Links[0].unitId;
                    assessmentRows.Add(new
                    {
                        Id = a1,
                        LearningUnitId = unitId,
                        Title = $"PACE Test - {GetUnitInfo(unitId).Code}",
                        AssessmentType = "summative",
                        AssessmentMethod = "written_test",
                        MaxScore = 100.00m,
                        PassingScore = 80.00m,
                        ReassessmentPolicy = "capped_at_passing",
                        Status = "conducted",
                        AssessmentDate = d1,
                        CreatedBy = _createdBy
                    });

                    var isThirdProvisional = studentId == nonFlagshipProvisionalStudentId;
                    var score = GenerateS2Score(studentId, unitId, random);
                    string resultStatus;
                    if (isThirdProvisional)
                    {
                        resultStatus = "provisional"; // provisional #3
                    }
                    else if (nonFlagshipApprovedCounter < 6)
                    {
                        resultStatus = "approved";
                        nonFlagshipApprovedCounter++;
                    }
                    else
                    {
                        resultStatus = "published";
                    }

                    resultRows.Add(new
                    {
                        Id = Guid.NewGuid(),
                        AssessmentId = a1,
                        StudentId = studentId,
                        AttemptNumber = 1,
                        ReplacesResultId = (Guid?)null,
                        IsFinal = 1,
                        ScoreNumeric = (decimal?)score,
                        ScoreMax = 100.00m,
                        GradeLetter = (string?)GetGradeLetter(score),
                        GradeLabel = (string?)GetGradeLabel(score),
                        EvaluationModel = "numeric",
                        EvaluatedBy = "teacher-01",
                        EvaluatedDate = d1,
                        ResultStatus = resultStatus,
                        ApprovedBy = resultStatus == "provisional" ? (string?)null : "Academic Authority",
                        ApprovedDate = resultStatus == "provisional" ? (DateTime?)null : d1.AddDays(1),
                        PublishedDate = resultStatus == "published" ? (DateTime?)d1.AddDays(2) : null,
                        Notes = resultStatus == "provisional" ? "Awaiting coordinator review" : "S2 summative assessment",
                        CreatedBy = _createdBy
                    });
                    resultsCreated++;
                }
            }

            await BatchInsertAsync(
                connection,
                transaction,
                "academics.assessments (id, learning_unit_id, title, assessment_type, assessment_method, max_score, passing_score, reassessment_policy, status, assessment_date, created_by)",
                ["Id", "LearningUnitId", "Title", "AssessmentType", "AssessmentMethod", "MaxScore", "PassingScore", "ReassessmentPolicy", "Status", "AssessmentDate", "CreatedBy"],
                assessmentRows,
                cancellationToken);
            await BatchInsertAsync(
                connection,
                transaction,
                "academics.academic_results (id, assessment_id, student_id, attempt_number, replaces_result_id, is_final, score_numeric, score_max, grade_letter, grade_label, evaluation_model, evaluated_by, evaluated_date, result_status, approved_by, approved_date, published_date, notes, created_by)",
                ["Id", "AssessmentId", "StudentId", "AttemptNumber", "ReplacesResultId", "IsFinal", "ScoreNumeric", "ScoreMax", "GradeLetter", "GradeLabel", "EvaluationModel", "EvaluatedBy", "EvaluatedDate", "ResultStatus", "ApprovedBy", "ApprovedDate", "PublishedDate", "Notes", "CreatedBy"],
                resultRows,
                cancellationToken);
            await BatchInsertAsync(
                connection,
                transaction,
                "academics.assessment_evidence (id, assessment_id, student_id, evidence_type, collected_date, evidence_reference, created_by)",
                ["Id", "AssessmentId", "StudentId", "EvidenceType", "CollectedDate", "EvidenceReference", "CreatedBy"],
                evidenceRows,
                cancellationToken);

            // ====================================================================
            // 5. Create coursework assignments and submissions for S2 (batched)
            //    6 missing submissions, ~10% late, ~87% submitted
            // ====================================================================
            var s2AssignmentIds = new List<Guid>();
            var assignmentRows = new List<object>(10);
            for (int aIdx = 0; aIdx < 10; aIdx++)
            {
                var assignmentId = Guid.NewGuid();
                s2AssignmentIds.Add(assignmentId);
                var releaseDate = sem2StartDate.AddDays(aIdx * 3);
                // First 4 assignments are past due (< Today); remaining 6 are upcoming (>= Today)
                var dueDate = aIdx < 4 ? baseDate.AddDays(-(4 - aIdx)) : baseDate.AddDays(aIdx - 1);

                assignmentRows.Add(new
                {
                    Id = assignmentId,
                    LearningActivityId = sem2ActivityIds[aIdx],
                    Title = $"Coursework Assignment {aIdx + 1}",
                    Description = "Regular coursework task for S2",
                    ReleaseDate = releaseDate,
                    DueDate = dueDate,
                    Status = "released",
                    CreatedBy = _createdBy
                });
            }

            await connection.ExecuteAsync(new CommandDefinition(
                _insertAssignmentSql, assignmentRows, transaction: transaction, cancellationToken: cancellationToken));

            // Build deterministic missing submission assignments for the 5 unaddressed at-risk learners (6 total missing)
            var unaddressedList = unaddressedNotInGoodStandingIds.Concat(unaddressedConditionalIds).ToList();
            var missingPairs = new HashSet<(Guid StudentId, int AssignmentIdx)>();
            if (unaddressedList.Count >= 5)
            {
                missingPairs.Add((unaddressedList[0], 0));
                missingPairs.Add((unaddressedList[0], 1));
                missingPairs.Add((unaddressedList[1], 0));
                missingPairs.Add((unaddressedList[2], 1));
                missingPairs.Add((unaddressedList[3], 2));
                missingPairs.Add((unaddressedList[4], 3));
            }

            var submissionRows = new List<object>(200);
            // All 45 active students on past-due assignments 0..3 (180 rows)
            for (int aIdx = 0; aIdx < 4; aIdx++)
            {
                var assignmentId = s2AssignmentIds[aIdx];
                for (int sIdx = 0; sIdx < activeStudentIds.Count; sIdx++)
                {
                    var studentId = activeStudentIds[sIdx];
                    string status;
                    DateTime? submittedDate;

                    if (studentId == budiStudentId)
                    {
                        status = "submitted";
                        submittedDate = DateTime.SpecifyKind(baseDate.AddDays(-1), DateTimeKind.Utc);
                    }
                    else if (missingPairs.Contains((studentId, aIdx)))
                    {
                        status = "missing";
                        submittedDate = null;
                    }
                    else if ((sIdx + aIdx) % 9 == 0)
                    {
                        status = "late";
                        submittedDate = DateTime.SpecifyKind(baseDate.AddDays(-1), DateTimeKind.Utc);
                    }
                    else
                    {
                        status = "submitted";
                        submittedDate = DateTime.SpecifyKind(baseDate.AddDays(-random.Next(2, 7)), DateTimeKind.Utc);
                    }

                    submissionRows.Add(new
                    {
                        Id = Guid.NewGuid(),
                        AssignmentId = assignmentId,
                        StudentId = studentId,
                        Status = status,
                        SubmittedDate = submittedDate,
                        IsResubmission = 0,
                        CreatedBy = _createdBy
                    });
                }
            }

            // Early submissions on upcoming assignment 4 (20 rows -> 200 total submissions)
            foreach (var studentId in activeStudentIds.Take(20))
            {
                submissionRows.Add(new
                {
                    Id = Guid.NewGuid(),
                    AssignmentId = s2AssignmentIds[4],
                    StudentId = studentId,
                    Status = "submitted",
                    SubmittedDate = (DateTime?)DateTime.SpecifyKind(baseDate.AddDays(-1), DateTimeKind.Utc),
                    IsResubmission = 0,
                    CreatedBy = _createdBy
                });
            }

            await BatchInsertAsync(
                connection,
                transaction,
                "academics.submissions (id, assignment_id, student_id, status, submitted_date, is_resubmission, created_by)",
                ["Id", "AssignmentId", "StudentId", "Status", "SubmittedDate", "IsResubmission", "CreatedBy"],
                submissionRows,
                cancellationToken);

            // ====================================================================
            // 6. Semester 1: Create certified report cards ("Buku Rapor Merdeka")
            // ====================================================================
            var reportCardRows = new List<object>(allTargetStudentIds.Count);
            var reportCardEntryRows = new List<object>(160);

            foreach (var studentId in allTargetStudentIds)
            {
                var s1Results = allS1Results.TryGetValue(studentId, out var results) ? results : [];
                var cardId = Guid.NewGuid();
                var (studentName, studentNisn) = GetStudentInfo(studentId);

                // Build content snapshot matching ReportCardSnapshotDto contract
                var entries = new List<ReportCardSubjectEntryDto>();
                var displayOrder = 1;
                foreach (var (resultId, assessmentId, unitId, score, gradeLetter, gradeLabel) in s1Results)
                {
                    var (unitCode, unitName) = GetUnitInfo(unitId);
                    entries.Add(new ReportCardSubjectEntryDto(
                        LearningUnitId: unitId,
                        SubjectCode: unitCode,
                        SubjectTitle: unitName,
                        ScoreNumeric: score,
                        GradeLetter: gradeLetter,
                        GradeLabel: gradeLabel,
                        TeacherComment: "Good progress in this PACE unit.",
                        DisplayOrder: displayOrder++));
                }

                var attendanceSummary = studentId == jonathanId
                    ? new ReportCardAttendanceSummaryDto(PresentDays: 30, AbsentDays: 0, LateDays: 0, ExcusedDays: 0)
                    : new ReportCardAttendanceSummaryDto(PresentDays: 28, AbsentDays: 1, LateDays: 1, ExcusedDays: 0);

                var snapshotDto = new ReportCardSnapshotDto(
                    ReportCardId: cardId,
                    StudentId: studentId,
                    StudentName: studentName,
                    StudentNisn: studentNisn,
                    AcademicPeriodId: sem1Id,
                    PeriodLabel: sem1Label,
                    ReportType: "end_of_term",
                    TemplateName: "Buku Rapor Merdeka",
                    Status: "published",
                    ApprovedBy: "Academic Authority",
                    ApprovedDate: DateTime.SpecifyKind(sem1EndDate.AddDays(2), DateTimeKind.Utc),
                    NarrativeComments: "Student has completed all required PACE units for Semester 1 with satisfactory mastery.",
                    SubjectEntries: entries,
                    AttendanceSummary: attendanceSummary);

                var contentSnapshot = JsonSerializer.Serialize(snapshotDto, _webOptions);

                reportCardRows.Add(new
                {
                    Id = cardId,
                    StudentId = studentId,
                    AcademicPeriodId = sem1Id,
                    AcademicPeriodLabel = sem1Label,
                    ReportType = "end_of_term",
                    TemplateName = "Buku Rapor Merdeka",
                    Status = "published",
                    ApprovedBy = "Academic Authority",
                    ApprovedDate = sem1EndDate.AddDays(2),
                    PublishedDate = sem1EndDate.AddDays(3),
                    NarrativeComments = "Student has completed all required PACE units for Semester 1 with satisfactory mastery.",
                    ContentSnapshot = contentSnapshot,
                    CreatedBy = _createdBy
                });
                reportCardsCreated++;

                displayOrder = 1;
                foreach (var (resultId, _, unitId, _, _, _) in s1Results)
                {
                    reportCardEntryRows.Add(new
                    {
                        Id = Guid.NewGuid(),
                        ReportCardId = cardId,
                        LearningUnitId = unitId,
                        AcademicResultId = resultId,
                        DisplayOrder = displayOrder++,
                        TeacherComment = "Satisfactory mastery demonstrated.",
                        CreatedBy = _createdBy
                    });
                }
            }

            await BatchInsertAsync(
                connection,
                transaction,
                "academics.report_cards (id, student_id, academic_period_id, academic_period_label, report_type, template_name, status, approved_by, approved_date, published_date, narrative_comments, content_snapshot, created_by)",
                ["Id", "StudentId", "AcademicPeriodId", "AcademicPeriodLabel", "ReportType", "TemplateName", "Status", "ApprovedBy", "ApprovedDate", "PublishedDate", "NarrativeComments", "ContentSnapshot", "CreatedBy"],
                reportCardRows,
                cancellationToken);
            await BatchInsertAsync(
                connection,
                transaction,
                "academics.report_card_entries (id, report_card_id, learning_unit_id, academic_result_id, display_order, teacher_comment, created_by)",
                ["Id", "ReportCardId", "LearningUnitId", "AcademicResultId", "DisplayOrder", "TeacherComment", "CreatedBy"],
                reportCardEntryRows,
                cancellationToken);

            // ====================================================================
            // 7. Semester 1: Create cumulative transcripts
            // ====================================================================
            var transcriptRows = new List<object>(allTargetStudentIds.Count);
            foreach (var studentId in allTargetStudentIds)
            {
                var s1Results = allS1Results.TryGetValue(studentId, out var results) ? results : [];
                var (studentName, studentNisn) = GetStudentInfo(studentId);

                var courses = new List<TranscriptCourseEntryDto>();
                decimal totalScore = 0m;
                int resultCount = 0;

                foreach (var (resultId, _, unitId, score, gradeLetter, _) in s1Results)
                {
                    var (unitCode, unitName) = GetUnitInfo(unitId);
                    courses.Add(new TranscriptCourseEntryDto(
                        LearningUnitId: unitId,
                        SubjectCode: unitCode,
                        SubjectTitle: unitName,
                        Credits: 1.0m,
                        ScoreNumeric: score,
                        GradeLetter: gradeLetter,
                        IsTransferred: false));
                    totalScore += score;
                    resultCount++;
                }

                decimal? gpa = resultCount > 0 ? Math.Round(totalScore / resultCount / 20m, 2) : (decimal?)null;

                var periodSummary = new TranscriptPeriodSummaryDto(
                    AcademicPeriodId: sem1Id,
                    PeriodLabel: sem1Label,
                    Courses: courses,
                    Gpa: gpa);

                var transcriptId = Guid.NewGuid();

                var transcriptDto = new TranscriptSnapshotDto(
                    TranscriptId: transcriptId,
                    StudentId: studentId,
                    StudentName: studentName,
                    StudentNisn: studentNisn,
                    TranscriptType: "official",
                    PeriodStart: DateTime.SpecifyKind(sem1StartDate, DateTimeKind.Utc),
                    PeriodEnd: DateTime.SpecifyKind(sem1EndDate, DateTimeKind.Utc),
                    Status: "issued",
                    Purpose: "Academic Record - Semester 1 Final Transcript",
                    CompletedPeriods: [periodSummary]);

                var transcriptSnapshot = JsonSerializer.Serialize(transcriptDto, _webOptions);

                transcriptRows.Add(new
                {
                    Id = transcriptId,
                    StudentId = studentId,
                    TranscriptType = "official",
                    PeriodStart = sem1StartDate,
                    PeriodEnd = sem1EndDate,
                    Status = "issued",
                    ApprovedBy = "Academic Authority",
                    ApprovedDate = sem1EndDate.AddDays(4),
                    IssuedDate = sem1EndDate.AddDays(5),
                    Purpose = "Academic Record - Semester 1 Final Transcript",
                    ContentSnapshot = transcriptSnapshot,
                    CreatedBy = _createdBy
                });
                transcriptsCreated++;
            }

            await BatchInsertAsync(
                connection,
                transaction,
                "academics.transcripts (id, student_id, transcript_type, period_start, period_end, status, approved_by, approved_date, issued_date, purpose, content_snapshot, created_by)",
                ["Id", "StudentId", "TranscriptType", "PeriodStart", "PeriodEnd", "Status", "ApprovedBy", "ApprovedDate", "IssuedDate", "Purpose", "ContentSnapshot", "CreatedBy"],
                transcriptRows,
                cancellationToken);

            // ====================================================================
            // 8. Seed Submodule 4: Standings, Progression, Interventions & Achievements (IS012, TD-08)
            //    Batch-inserted across all 45 active students for S1 & S2
            // ====================================================================
            await SeedStandingAndProgressionSubmoduleAsync(
                connection,
                transaction,
                operationsContext,
                academicsContext,
                unitLookup,
                activeStudentIds,
                jonathanId,
                graceId,
                rinaId,
                budiStudentId,
                unaddressedConditionalIds,
                unaddressedNotInGoodStandingIds,
                s1ConditionalImprovedIds,
                s1NotInGoodImprovedIds,
                now,
                cancellationToken);

            // ====================================================================
            // 9. Seed Submodule 5: Student Learning Plans (IS013, P4-S12)
            // ====================================================================
            participationsCreated += await SeedStudentLearningPlansSubmoduleAsync(
                connection,
                transaction,
                operationsContext,
                academicsContext,
                budiStudentId,
                timotiusId,
                now,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new AcademicsActivitiesSeedResult(
                ParticipationsCreated: participationsCreated,
                AttendanceRecordsCreated: attendanceRecordsCreated,
                ResultsCreated: resultsCreated,
                ReportCardsCreated: reportCardsCreated,
                TranscriptsCreated: transcriptsCreated);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string GetSectionLabel(
        Guid studentId,
        List<Guid> enrolledStudentIds,
        Random random,
        Guid rinaId,
        Guid budiStudentId,
        Guid timotiusId)
    {
        if (studentId == rinaId || studentId == budiStudentId || studentId == timotiusId)
        {
            return "ELC-A";
        }

        var index = enrolledStudentIds.IndexOf(studentId);
        if (index < 16) return "ELC-A";
        if (index < 32) return "JHLC-A";
        return "HSLC-A";
    }

    private static string GetPaceCode(Guid unitId)
    {
        // Return a placeholder code - in production this would query the database
        return $"PACE-{unitId:D}";
    }

    private static decimal GenerateS1Score(
        Guid studentId,
        Guid unitId,
        Random random,
        Guid rinaId,
        Guid budiStudentId,
        Guid timotiusId,
        List<Guid> mathUnits,
        List<Guid> bibleUnits)
    {
        if (studentId == rinaId)
        {
            return 98.00m + random.Next(0, 3);
        }

        if (studentId == budiStudentId && mathUnits.Contains(unitId))
        {
            return 68.00m; // Below KKM 80% (needs remediation)
        }

        if (studentId == timotiusId)
        {
            if (bibleUnits.Contains(unitId))
            {
                return 88.00m;
            }
            return 65.00m; // Multiple failing units
        }

        // Deterministic score: mostly passing (80-100), some borderline (75-85)
        var hash = (studentId.GetHashCode() ^ unitId.GetHashCode()) & 0xFFFF;
        var bucket = hash % 100;

        if (bucket < 70) return 85.00m + random.Next(0, 15); // 70% pass comfortably
        if (bucket < 85) return 80.00m + random.Next(0, 5);  // 15% barely pass
        return 75.00m + random.Next(0, 5);                    // 15% fail (need remediation)
    }

    private static async Task SeedStandingAndProgressionSubmoduleAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        OperationsSeedResult operationsContext,
        AcademicsStructureSeedResult academicsContext,
        Dictionary<Guid, (Guid Id, string Code, string Name)> unitLookup,
        IReadOnlyList<Guid> activeStudentIds,
        Guid jonathanId,
        Guid graceId,
        Guid rinaId,
        Guid budiStudentId,
        IReadOnlyList<Guid> unaddressedConditionalIds,
        IReadOnlyList<Guid> unaddressedNotInGoodStandingIds,
        IReadOnlyList<Guid> s1ConditionalImprovedIds,
        IReadOnlyList<Guid> s1NotInGoodImprovedIds,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var sem1Id = operationsContext.HistoricalPeriodId;
        var sem2Id = operationsContext.ActivePeriodId;
        var baseDate = operationsContext.BaseDate;
        var sem1EndDate = DateOnly.FromDateTime(baseDate.AddDays(-35));

        var paceUnitIds = academicsContext.PaceUnitIds;
        var mathUnits = paceUnitIds
            .Where(id => unitLookup.TryGetValue(id, out var u) && (u.Code.StartsWith("M", StringComparison.OrdinalIgnoreCase) || u.Name.Contains("Math", StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (mathUnits.Count == 0) mathUnits = paceUnitIds.Take(6).ToList();
        var mathUnitSet = new HashSet<Guid>(mathUnits);

        var mathSubjectId = academicsContext.SubjectUnitIds.GetValueOrDefault("MATH", mathUnits.FirstOrDefault());

        var unaddressedCondSet = new HashSet<Guid>(unaddressedConditionalIds);
        var unaddressedNgsSet = new HashSet<Guid>(unaddressedNotInGoodStandingIds);
        var s1CondImpSet = new HashSet<Guid>(s1ConditionalImprovedIds);
        var s1NgsImpSet = new HashSet<Guid>(s1NotInGoodImprovedIds);

        // ---------------------------------------------------------------------
        // 1. Learning Progress (batched across all 45 active students for S1 & S2)
        // ---------------------------------------------------------------------
        var progressRows = new List<LearningProgressBatchRow>(activeStudentIds.Count * paceUnitIds.Count * 2);
        var s2CompletedDate = DateOnly.FromDateTime(baseDate.AddDays(-5));

        foreach (var studentId in activeStudentIds)
        {
            var isBudi = studentId == budiStudentId;
            var isRina = studentId == rinaId;

            foreach (var unitId in paceUnitIds)
            {
                var isMath = mathUnitSet.Contains(unitId);

                // S1 learning progress
                if (isBudi && isMath)
                {
                    progressRows.Add(new LearningProgressBatchRow(
                        Guid.NewGuid(), studentId, unitId, sem1Id,
                        "needs_remediation", "assessment_evidence", null,
                        "Assessment score below KKM (80%) in Mathematics; requires remediation packet", now));
                }
                else if (unaddressedNgsSet.Contains(studentId) && isMath)
                {
                    progressRows.Add(new LearningProgressBatchRow(
                        Guid.NewGuid(), studentId, unitId, sem1Id,
                        "needs_remediation", "assessment_evidence", null,
                        "Unfulfilled PACE requirements; below passing threshold", now));
                }
                else
                {
                    progressRows.Add(new LearningProgressBatchRow(
                        Guid.NewGuid(), studentId, unitId, sem1Id,
                        "completed", "assessment_evidence", sem1EndDate,
                        "Completed Semester 1 PACE unit with mastery", now));
                }

                // S2 learning progress
                if (isBudi && isMath)
                {
                    progressRows.Add(new LearningProgressBatchRow(
                        Guid.NewGuid(), studentId, unitId, sem2Id,
                        "needs_remediation", "assessment_evidence", null,
                        "Active Mathematics remediation plan underway", now));
                }
                else if (isRina)
                {
                    progressRows.Add(new LearningProgressBatchRow(
                        Guid.NewGuid(), studentId, unitId, sem2Id,
                        "completed", "assessment_evidence", s2CompletedDate,
                        "Completed PACE unit expectations with mastery", now));
                }
                else
                {
                    progressRows.Add(new LearningProgressBatchRow(
                        Guid.NewGuid(), studentId, unitId, sem2Id,
                        "in_progress", "learning_work", null,
                        "In progress S2 PACE unit", now));
                }
            }
        }

        await BatchInsertLearningProgressAsync(connection, transaction, progressRows, _createdBy, cancellationToken);

        // ---------------------------------------------------------------------
        // 2. Academic Standings (batched across all 45 active students for S1 & S2)
        //    S1: 36 good_standing (80.0%), 6 conditional_standing, 3 not_in_good_standing
        //    S2: 38 good_standing (84.4%, +4.4% delta), 5 conditional_standing, 2 not_in_good_standing
        // ---------------------------------------------------------------------
        var standingRows = new List<object>(activeStudentIds.Count * 2);
        var recRows = new List<object>(6);
        var sem1DeterminedAt = baseDate.AddDays(-33);
        var s2DeterminedAt = baseDate.AddDays(-2);

        foreach (var studentId in activeStudentIds)
        {
            string s1Standing;
            string s2Standing;

            if (studentId == budiStudentId || studentId == graceId || unaddressedCondSet.Contains(studentId))
            {
                s1Standing = "conditional_standing";
                s2Standing = "conditional_standing";
            }
            else if (unaddressedNgsSet.Contains(studentId))
            {
                s1Standing = "not_in_good_standing";
                s2Standing = "not_in_good_standing";
            }
            else if (s1CondImpSet.Contains(studentId))
            {
                s1Standing = "conditional_standing";
                s2Standing = "good_standing";
            }
            else if (s1NgsImpSet.Contains(studentId))
            {
                s1Standing = "not_in_good_standing";
                s2Standing = "good_standing";
            }
            else
            {
                s1Standing = "good_standing";
                s2Standing = "good_standing";
            }

            var s1Id = Guid.NewGuid();
            var s2Id = Guid.NewGuid();
            var isBudi = studentId == budiStudentId;

            standingRows.Add(new
            {
                Id = s1Id,
                StudentId = studentId,
                AcademicPeriodId = sem1Id,
                Standing = s1Standing,
                PolicyReference = "SK-AKAD/2026/POLICY-01",
                Conditions = s1Standing == "good_standing"
                    ? (string?)null
                    : isBudi
                        ? "Must complete Mathematics remediation during holiday packet and pass reassessment before Grade 5 commencement."
                        : "Subject to academic progress monitoring and PACE mastery target.",
                IsException = isBudi,
                ExceptionReason = isBudi ? "Academic council approval for conditional standing subject to mathematics remediation agreement" : (string?)null,
                CommitteeReference = isBudi ? "SK-DG/2026/06-01" : (string?)null,
                AuthorizedBy = "Academic Authority",
                DeterminedAt = sem1DeterminedAt,
                CreatedAt = now,
                CreatedBy = _createdBy
            });

            standingRows.Add(new
            {
                Id = s2Id,
                StudentId = studentId,
                AcademicPeriodId = sem2Id,
                Standing = s2Standing,
                PolicyReference = "SK-AKAD/2026/POLICY-01",
                Conditions = s2Standing == "good_standing"
                    ? (string?)null
                    : isBudi
                        ? "Must complete Mathematics remediation during holiday packet and pass reassessment before Grade 5 commencement."
                        : "Subject to academic progress monitoring and PACE mastery target.",
                IsException = isBudi,
                ExceptionReason = isBudi ? "Academic council approval for conditional standing subject to mathematics remediation agreement" : (string?)null,
                CommitteeReference = isBudi ? "SK-DG/2026/06-01" : (string?)null,
                AuthorizedBy = "Academic Authority",
                DeterminedAt = s2DeterminedAt,
                CreatedAt = now,
                CreatedBy = _createdBy
            });

            var firstUnaddressedNgsId = unaddressedNotInGoodStandingIds.Count > 0 ? unaddressedNotInGoodStandingIds[0] : Guid.Empty;
            if (studentId == rinaId || isBudi || studentId == firstUnaddressedNgsId)
            {
                var recType = s2Standing switch
                {
                    "good_standing" => "promotion_eligible",
                    "conditional_standing" => "conditional_promotion",
                    _ => "retention_recommended"
                };

                recRows.Add(new
                {
                    Id = Guid.NewGuid(),
                    StudentId = studentId,
                    AcademicPeriodId = sem1Id,
                    AcademicStandingId = (Guid?)s1Id,
                    Recommendation = recType,
                    FromLevel = "Grade 4",
                    RecommendedToLevel = recType == "retention_recommended" ? (string?)null : "Grade 5",
                    Conditions = isBudi ? "Conditional advancement granted subject to completion of Math remediation tutoring packet prior to term start." : (string?)null,
                    SupportingEvidence = $"Semester 1 determination ({s1Standing}) per SK-AKAD/2026/POLICY-01.",
                    IsException = isBudi,
                    ExceptionReason = isBudi ? "Dewan Guru certified conditional promotion with mandatory mathematics tutoring contract" : (string?)null,
                    CommitteeReference = isBudi ? "SK-DG/2026/06-01" : (string?)null,
                    AuthorizedBy = "Academic Authority",
                    IssuedAt = baseDate.AddDays(-32),
                    CreatedAt = now,
                    CreatedBy = _createdBy
                });

                recRows.Add(new
                {
                    Id = Guid.NewGuid(),
                    StudentId = studentId,
                    AcademicPeriodId = sem2Id,
                    AcademicStandingId = (Guid?)s2Id,
                    Recommendation = recType,
                    FromLevel = "Grade 4",
                    RecommendedToLevel = recType == "retention_recommended" ? (string?)null : "Grade 5",
                    Conditions = isBudi ? "Conditional advancement granted subject to completion of Math remediation tutoring packet prior to term start." : (string?)null,
                    SupportingEvidence = $"Semester 2 live progression review ({s2Standing}).",
                    IsException = isBudi,
                    ExceptionReason = isBudi ? "Dewan Guru certified conditional promotion with mandatory mathematics tutoring contract" : (string?)null,
                    CommitteeReference = isBudi ? "SK-DG/2026/06-01" : (string?)null,
                    AuthorizedBy = "Academic Authority",
                    IssuedAt = baseDate.AddDays(-1),
                    CreatedAt = now,
                    CreatedBy = _createdBy
                });
            }
        }

        await BatchInsertAsync(
            connection,
            transaction,
            "academics.academic_standings (id, student_id, academic_period_id, standing, policy_reference, conditions, is_exception, exception_reason, committee_reference, authorized_by, determined_at, created_at, created_by)",
            ["Id", "StudentId", "AcademicPeriodId", "Standing", "PolicyReference", "Conditions", "IsException", "ExceptionReason", "CommitteeReference", "AuthorizedBy", "DeterminedAt", "CreatedAt", "CreatedBy"],
            standingRows,
            cancellationToken);

        if (recRows.Count > 0)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                _insertPromotionRecommendationSql, recRows, transaction: transaction, cancellationToken: cancellationToken));
        }

        // ---------------------------------------------------------------------
        // 4. Academic Interventions
        //    Cover 2 of the 7 at-risk learners (Budi + Grace), leaving 5 unaddressed
        //    Plus Rina's extension_work (Rina is good_standing, not at-risk)
        // ---------------------------------------------------------------------
        await connection.ExecuteAsync(new CommandDefinition(
            _insertInterventionSql,
            new[]
            {
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = budiStudentId,
                    LearningUnitId = mathSubjectId,
                    AcademicPeriodId = sem2Id,
                    InterventionType = "remediation",
                    TriggerType = "assessment_below_threshold",
                    TriggerDescription = "Operative score 68/100 below KKM threshold (80%) in Mathematics",
                    PlanDescription = "Weekly 1-on-1 tutoring sessions and holiday remediation packet",
                    Status = "in_progress",
                    InitiatedBy = "Academic Coordinator",
                    InitiatedAt = now.AddDays(-14),
                    ConcludedAt = (DateTimeOffset?)null,
                    OutcomeNotes = (string?)null,
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = graceId,
                    LearningUnitId = mathSubjectId,
                    AcademicPeriodId = sem2Id,
                    InterventionType = "tutoring_recommendation",
                    TriggerType = "standing_concern",
                    TriggerDescription = "Conditional standing following PACE Attempt 1 below 80% KKM",
                    PlanDescription = "Guided PACE mastery coaching and reassessment preparation",
                    Status = "in_progress",
                    InitiatedBy = "Academic Coordinator",
                    InitiatedAt = now.AddDays(-10),
                    ConcludedAt = (DateTimeOffset?)null,
                    OutcomeNotes = (string?)null,
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = rinaId,
                    LearningUnitId = mathSubjectId,
                    AcademicPeriodId = sem2Id,
                    InterventionType = "extension_work",
                    TriggerType = "teacher_referral",
                    TriggerDescription = "Demonstrated advanced mastery in Mathematics PACE series; recommended for Olympiad enrichment",
                    PlanDescription = "Advanced Math enrichment curriculum and weekly Olympiad problem solving packet",
                    Status = "in_progress",
                    InitiatedBy = "Academic Coordinator",
                    InitiatedAt = now.AddDays(-21),
                    ConcludedAt = (DateTimeOffset?)null,
                    OutcomeNotes = (string?)null,
                    CreatedAt = now,
                    CreatedBy = _createdBy
                }
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // ---------------------------------------------------------------------
        // 5. Academic Achievements
        // ---------------------------------------------------------------------
        // 5a. Rina Kusuma: subject_distinction Math
        await connection.ExecuteAsync(new CommandDefinition(
            _insertAchievementSql,
            new
            {
                Id = Guid.NewGuid(),
                StudentId = rinaId,
                LearningUnitId = (Guid?)mathSubjectId,
                AcademicPeriodId = sem1Id,
                AchievementType = "subject_distinction",
                Title = "Nilai Tertinggi Matematika",
                Description = "Highest cumulative score in Mathematics across Grade 4 cohort",
                CriteriaMet = "Score > 95% across all Grade 4 Mathematics PACE units",
                SupportingEvidence = "Certified official grade report 98.5%",
                RecognizedBy = "Academic Authority",
                RecognizedAt = sem1EndDate,
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            _insertAchievementSql,
            new
            {
                Id = Guid.NewGuid(),
                StudentId = rinaId,
                LearningUnitId = (Guid?)mathSubjectId,
                AcademicPeriodId = sem2Id,
                AchievementType = "subject_distinction",
                Title = "Nilai Tertinggi Matematika",
                Description = "Highest cumulative score in Mathematics across Grade 4 cohort",
                CriteriaMet = "Score > 95% across all Grade 4 Mathematics PACE units",
                SupportingEvidence = "Certified official grade report 98.5%",
                RecognizedBy = "Academic Authority",
                RecognizedAt = DateOnly.FromDateTime(baseDate.AddDays(-5)),
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 5b. Rina Kusuma: academic_honors Juara Umum Grade 4
        await connection.ExecuteAsync(new CommandDefinition(
            _insertAchievementSql,
            new
            {
                Id = Guid.NewGuid(),
                StudentId = rinaId,
                LearningUnitId = (Guid?)null,
                AcademicPeriodId = sem1Id,
                AchievementType = "academic_honors",
                Title = "Juara Umum Grade 4",
                Description = "Overall top academic achievement across all subject units in Grade 4 cohort",
                CriteriaMet = "Cumulative GPA 4.0 / 100% mastery across all Grade 4 core subjects",
                SupportingEvidence = "Official certified Semester 1 transcript snapshot",
                RecognizedBy = "Academic Authority",
                RecognizedAt = sem1EndDate.AddDays(5),
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            _insertAchievementSql,
            new
            {
                Id = Guid.NewGuid(),
                StudentId = rinaId,
                LearningUnitId = (Guid?)null,
                AcademicPeriodId = sem2Id,
                AchievementType = "academic_honors",
                Title = "Juara Umum Grade 4",
                Description = "Overall top academic achievement across all subject units in Grade 4 cohort",
                CriteriaMet = "Cumulative GPA 4.0 / 100% mastery across all Grade 4 core subjects",
                SupportingEvidence = "Official certified Semester 1 transcript snapshot",
                RecognizedBy = "Academic Authority",
                RecognizedAt = DateOnly.FromDateTime(baseDate.AddDays(-2)),
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));
    }

    private static decimal GenerateS2Score(Guid studentId, Guid unitId, Random random)
    {
        // For non-flagship students, generate provisional scores
        var hash = (studentId.GetHashCode() ^ unitId.GetHashCode() ^ 0xBEEF) & 0xFFFF;
        var bucket = hash % 100;

        if (bucket < 60) return 82.00m + random.Next(0, 13);
        if (bucket < 85) return 75.00m + random.Next(0, 7);
        return 65.00m + random.Next(0, 10);
    }

    private static string GetGradeLetter(decimal score)
    {
        return score switch
        {
            >= 90 => "A",
            >= 80 => "B+",
            >= 70 => "B",
            >= 60 => "C+",
            >= 50 => "C",
            >= 40 => "D",
            _ => "E"
        };
    }

    private static string GetGradeLabel(decimal score)
    {
        return score switch
        {
            >= 90 => "Sangat Mahir",
            >= 80 => "Mahir",
            >= 70 => "Cukup Mahir",
            >= 60 => "Membangun",
            >= 50 => "Perlu Bimbingan",
            _ => "Belum Memenuhi KKM"
        };
    }

    private static async Task BatchInsertAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        string tableAndColumns,
        string[] propertyNames,
        IReadOnlyList<object> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0) return;

        if (connection is SqlConnection sqlConn && transaction is SqlTransaction sqlTx)
        {
            var parenIdx = tableAndColumns.IndexOf('(', StringComparison.Ordinal);
            var tableName = tableAndColumns[..parenIdx].Trim();
            var colList = tableAndColumns[(parenIdx + 1)..^1]
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            await SqlBulkInsertAsync(sqlConn, sqlTx, tableName, colList, propertyNames, rows, cancellationToken);
            return;
        }

        var rowType = rows[0].GetType();
        var props = propertyNames.Select(n => rowType.GetProperty(n)!).ToArray();
        var batchSize = Math.Max(1, Math.Min(150, 1900 / props.Length));

        for (int offset = 0; offset < rows.Count; offset += batchSize)
        {
            var chunkCount = Math.Min(batchSize, rows.Count - offset);
            var sb = new StringBuilder();
            sb.Append("INSERT INTO ").Append(tableAndColumns).AppendLine(" VALUES");

            var parameters = new DynamicParameters();
            for (int i = 0; i < chunkCount; i++)
            {
                var row = rows[offset + i];
                if (i > 0) sb.Append(',');
                sb.Append('(');
                for (int p = 0; p < props.Length; p++)
                {
                    if (p > 0) sb.Append(',');
                    var paramName = $"p{i}_{p}";
                    sb.Append('@').Append(paramName);
                    parameters.Add(paramName, props[p].GetValue(row));
                }
                sb.Append(')');
            }

            sb.Append(';');
            await connection.ExecuteAsync(new CommandDefinition(
                sb.ToString(), parameters, transaction: transaction, cancellationToken: cancellationToken));
        }
    }

    private static async Task SqlBulkInsertAsync<T>(
        SqlConnection sqlConn,
        SqlTransaction sqlTx,
        string tableName,
        string[] sqlColumns,
        string[] propertyNames,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken,
        string? createdByConstant = null,
        string? createdByColumn = null)
    {
        if (rows.Count == 0) return;

        var rowType = rows[0]!.GetType();
        var props = propertyNames.Select(n => rowType.GetProperty(n)!).ToArray();

        using var dt = new DataTable();
        using var bulk = new SqlBulkCopy(sqlConn, SqlBulkCopyOptions.CheckConstraints, sqlTx)
        {
            DestinationTableName = tableName,
            BatchSize = 1000
        };

        for (int i = 0; i < props.Length; i++)
        {
            var underlying = Nullable.GetUnderlyingType(props[i].PropertyType) ?? props[i].PropertyType;
            var colType = underlying == typeof(DateOnly) ? typeof(DateTime) : underlying;
            dt.Columns.Add(sqlColumns[i], colType);
            bulk.ColumnMappings.Add(sqlColumns[i], sqlColumns[i]);
        }

        if (createdByColumn is not null)
        {
            dt.Columns.Add(createdByColumn, typeof(string));
            bulk.ColumnMappings.Add(createdByColumn, createdByColumn);
        }

        foreach (var row in rows)
        {
            var values = new object[dt.Columns.Count];
            for (int i = 0; i < props.Length; i++)
            {
                var val = props[i].GetValue(row);
                values[i] = val switch
                {
                    null => DBNull.Value,
                    DateOnly d => d.ToDateTime(TimeOnly.MinValue),
                    _ => val
                };
            }
            if (createdByColumn is not null)
            {
                values[props.Length] = createdByConstant ?? _createdBy;
            }
            dt.Rows.Add(values);
        }

        await bulk.WriteToServerAsync(dt, cancellationToken);
    }

    private sealed record ParticipationBatchRow(
        Guid Id,
        Guid StudentId,
        Guid LearningUnitId,
        Guid AcademicPeriodId,
        string AcademicPeriodLabel,
        string ParticipationType,
        int AttemptNumber,
        string Source,
        string SectionLabel,
        string Status,
        DateTime StartDate,
        DateTime? EndDate,
        DateTime? CompletionDate);

    private sealed record LearningProgressBatchRow(
        Guid Id,
        Guid StudentId,
        Guid LearningUnitId,
        Guid AcademicPeriodId,
        string Status,
        string Basis,
        DateOnly? CompletionDate,
        string Notes,
        DateTimeOffset CreatedAt);

    private sealed record AttendanceBatchRow(
        Guid Id,
        Guid LearningActivityId,
        Guid StudentId,
        string Status,
        string RecordedBy,
        DateTime RecordedDate);

    private static async Task BatchInsertAcademicParticipationsAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        List<ParticipationBatchRow> rows,
        string createdBy,
        CancellationToken cancellationToken)
    {
        if (connection is SqlConnection sqlConn && transaction is SqlTransaction sqlTx)
        {
            await SqlBulkInsertAsync(
                sqlConn,
                sqlTx,
                "academics.academic_participations",
                ["id", "student_id", "learning_unit_id", "academic_period_id", "academic_period_label", "participation_type", "attempt_number", "source", "section_label", "status", "start_date", "end_date", "completion_date"],
                ["Id", "StudentId", "LearningUnitId", "AcademicPeriodId", "AcademicPeriodLabel", "ParticipationType", "AttemptNumber", "Source", "SectionLabel", "Status", "StartDate", "EndDate", "CompletionDate"],
                rows,
                cancellationToken,
                createdBy,
                "created_by");
            return;
        }

        const int batchSize = 100;
        for (int offset = 0; offset < rows.Count; offset += batchSize)
        {
            var chunkCount = Math.Min(batchSize, rows.Count - offset);
            var sb = new StringBuilder();
            sb.AppendLine("""
                INSERT INTO academics.academic_participations (
                    id, student_id, learning_unit_id, academic_period_id, academic_period_label,
                    participation_type, attempt_number, source, section_label, status,
                    start_date, end_date, completion_date, created_by
                ) VALUES
                """);

            var parameters = new DynamicParameters();
            parameters.Add("CreatedBy", createdBy);

            for (int i = 0; i < chunkCount; i++)
            {
                var r = rows[offset + i];
                if (i > 0) sb.Append(',');
                sb.Append(CultureInfo.InvariantCulture, $"(@Id{i}, @StudentId{i}, @UnitId{i}, @PeriodId{i}, @PeriodLabel{i}, @PType{i}, @Attempt{i}, @Source{i}, @Section{i}, @Status{i}, @Start{i}, @End{i}, @Comp{i}, @CreatedBy)");
                parameters.Add($"Id{i}", r.Id);
                parameters.Add($"StudentId{i}", r.StudentId);
                parameters.Add($"UnitId{i}", r.LearningUnitId);
                parameters.Add($"PeriodId{i}", r.AcademicPeriodId);
                parameters.Add($"PeriodLabel{i}", r.AcademicPeriodLabel);
                parameters.Add($"PType{i}", r.ParticipationType);
                parameters.Add($"Attempt{i}", r.AttemptNumber);
                parameters.Add($"Source{i}", r.Source);
                parameters.Add($"Section{i}", r.SectionLabel);
                parameters.Add($"Status{i}", r.Status);
                parameters.Add($"Start{i}", r.StartDate);
                parameters.Add($"End{i}", r.EndDate);
                parameters.Add($"Comp{i}", r.CompletionDate);
            }

            sb.Append(';');
            await connection.ExecuteAsync(new CommandDefinition(
                sb.ToString(), parameters, transaction: transaction, cancellationToken: cancellationToken));
        }
    }

    private static async Task BatchInsertLearningProgressAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        List<LearningProgressBatchRow> rows,
        string createdBy,
        CancellationToken cancellationToken)
    {
        if (connection is SqlConnection sqlConn && transaction is SqlTransaction sqlTx)
        {
            await SqlBulkInsertAsync(
                sqlConn,
                sqlTx,
                "academics.learning_progress",
                ["id", "student_id", "learning_unit_id", "academic_period_id", "status", "basis", "completion_date", "notes", "created_at"],
                ["Id", "StudentId", "LearningUnitId", "AcademicPeriodId", "Status", "Basis", "CompletionDate", "Notes", "CreatedAt"],
                rows,
                cancellationToken,
                createdBy,
                "created_by");
            return;
        }

        const int batchSize = 150;
        for (int offset = 0; offset < rows.Count; offset += batchSize)
        {
            var chunkCount = Math.Min(batchSize, rows.Count - offset);
            var sb = new StringBuilder();
            sb.AppendLine("""
                INSERT INTO academics.learning_progress (
                    id, student_id, learning_unit_id, academic_period_id, status,
                    basis, completion_date, notes, created_at, created_by
                ) VALUES
                """);

            var parameters = new DynamicParameters();
            parameters.Add("CreatedBy", createdBy);

            for (int i = 0; i < chunkCount; i++)
            {
                var r = rows[offset + i];
                if (i > 0) sb.Append(',');
                sb.Append(CultureInfo.InvariantCulture, $"(@Id{i}, @StudentId{i}, @UnitId{i}, @PeriodId{i}, @Status{i}, @Basis{i}, @CompDate{i}, @Notes{i}, @CreatedAt{i}, @CreatedBy)");
                parameters.Add($"Id{i}", r.Id);
                parameters.Add($"StudentId{i}", r.StudentId);
                parameters.Add($"UnitId{i}", r.LearningUnitId);
                parameters.Add($"PeriodId{i}", r.AcademicPeriodId);
                parameters.Add($"Status{i}", r.Status);
                parameters.Add($"Basis{i}", r.Basis);
                parameters.Add($"CompDate{i}", r.CompletionDate);
                parameters.Add($"Notes{i}", r.Notes);
                parameters.Add($"CreatedAt{i}", r.CreatedAt);
            }

            sb.Append(';');
            await connection.ExecuteAsync(new CommandDefinition(
                sb.ToString(), parameters, transaction: transaction, cancellationToken: cancellationToken));
        }
    }

    private static async Task<int> CreateS1AttendanceRecordsAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        List<Guid> activityIds,
        List<Guid> studentIds,
        Guid jonathanId,
        DateTime periodStart,
        string createdBy,
        Random random,
        int targetCount,
        CancellationToken cancellationToken)
    {
        var rows = new List<AttendanceBatchRow>(targetCount);
        foreach (var activityId in activityIds)
        {
            if (rows.Count >= targetCount) break;

            var activityDate = periodStart.AddDays(random.Next(0, 100));
            var countForActivity = Math.Min(25, targetCount - rows.Count);
            var selectedStudents = studentIds
                .OrderBy(_ => random.Next())
                .Take(countForActivity)
                .ToList();

            foreach (var studentId in selectedStudents)
            {
                var status = studentId == jonathanId
                    ? "present"
                    : (random.Next(100) switch
                    {
                        < 94 => "present",
                        < 97 => "excused_absence",
                        < 99 => "late",
                        _ => "absent"
                    });

                rows.Add(new AttendanceBatchRow(
                    Guid.NewGuid(),
                    activityId,
                    studentId,
                    status,
                    "teacher-01",
                    activityDate));
            }
        }

        await BatchInsertAttendanceRecordsAsync(connection, transaction, rows, createdBy, cancellationToken);
        return rows.Count;
    }

    private static async Task<int> CreateS2AttendanceRecordsAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        List<Guid> activityIds,
        IReadOnlyList<Guid> activeStudentIds,
        Guid jonathanId,
        HashSet<Guid> chronicAbsenteeismIds,
        DateTime periodStart,
        string createdBy,
        Random random,
        CancellationToken cancellationToken)
    {
        var rows = new List<AttendanceBatchRow>(520);

        for (int sIdx = 0; sIdx < activeStudentIds.Count; sIdx++)
        {
            var studentId = activeStudentIds[sIdx];
            var isChronic = chronicAbsenteeismIds.Contains(studentId);
            var isJonathan = studentId == jonathanId;
            var sessionCount = (isChronic || isJonathan) ? 12 : 11;

            for (int k = 0; k < sessionCount; k++)
            {
                var activityId = activityIds[(sIdx + k) % activityIds.Count];
                var recordedDate = periodStart.AddDays((k * 2) + (sIdx % 3));

                string status;
                if (isJonathan)
                {
                    status = "present";
                }
                else if (isChronic)
                {
                    // 8 present, 4 absent -> 66.7% (< 80% threshold)
                    status = k < 8 ? "present" : "absent";
                }
                else
                {
                    // >= 90.9% attendance (10 present + 1 present/excused_absence/late)
                    status = k < 10
                        ? "present"
                        : ((sIdx % 4) switch
                        {
                            0 => "excused_absence",
                            1 => "late",
                            _ => "present"
                        });
                }

                rows.Add(new AttendanceBatchRow(
                    Guid.NewGuid(),
                    activityId,
                    studentId,
                    status,
                    "teacher-01",
                    recordedDate));
            }
        }

        await BatchInsertAttendanceRecordsAsync(connection, transaction, rows, createdBy, cancellationToken);
        return rows.Count;
    }

    private static async Task BatchInsertAttendanceRecordsAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        List<AttendanceBatchRow> rows,
        string createdBy,
        CancellationToken cancellationToken)
    {
        if (connection is SqlConnection sqlConn && transaction is SqlTransaction sqlTx)
        {
            await SqlBulkInsertAsync(
                sqlConn,
                sqlTx,
                "academics.attendance_records",
                ["id", "learning_activity_id", "student_id", "status", "recorded_by", "recorded_date"],
                ["Id", "LearningActivityId", "StudentId", "Status", "RecordedBy", "RecordedDate"],
                rows,
                cancellationToken,
                createdBy,
                "created_by");
            return;
        }

        const int batchSize = 200;
        for (int offset = 0; offset < rows.Count; offset += batchSize)
        {
            var chunkCount = Math.Min(batchSize, rows.Count - offset);
            var sb = new StringBuilder();
            sb.AppendLine("""
                INSERT INTO academics.attendance_records (
                    id, learning_activity_id, student_id, status, recorded_by, recorded_date,
                    created_by
                ) VALUES
                """);

            var parameters = new DynamicParameters();
            parameters.Add("CreatedBy", createdBy);

            for (int i = 0; i < chunkCount; i++)
            {
                var r = rows[offset + i];
                if (i > 0) sb.Append(',');
                sb.Append(CultureInfo.InvariantCulture, $"(@Id{i}, @ActId{i}, @StdId{i}, @Status{i}, @RecBy{i}, @RecDate{i}, @CreatedBy)");
                parameters.Add($"Id{i}", r.Id);
                parameters.Add($"ActId{i}", r.LearningActivityId);
                parameters.Add($"StdId{i}", r.StudentId);
                parameters.Add($"Status{i}", r.Status);
                parameters.Add($"RecBy{i}", r.RecordedBy);
                parameters.Add($"RecDate{i}", r.RecordedDate);
            }

            sb.Append(';');
            await connection.ExecuteAsync(new CommandDefinition(
                sb.ToString(), parameters, transaction: transaction, cancellationToken: cancellationToken));
        }
    }

    private static async Task<int> SeedStudentLearningPlansSubmoduleAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        OperationsSeedResult operationsContext,
        AcademicsStructureSeedResult academicsContext,
        Guid budiStudentId,
        Guid timotiusId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var sem1Id = operationsContext.HistoricalPeriodId;
        var baseDate = operationsContext.BaseDate;
        var sem1StartDate = baseDate.AddDays(-180);
        var sem1EndDate = baseDate.AddDays(-35);
        var slpPeriodLabel = "Semester 1 TP 2029/2030";

        // 1. Ensure required learning units exist in academics.learning_units
        var mathSubjectId = academicsContext.SubjectUnitIds.TryGetValue("MATH", out var mId) ? (Guid?)mId : null;

        var mat3Id = await EnsureLearningUnitAsync(
            connection,
            transaction,
            academicsContext.LearningStructureId,
            LuMat3S1Id,
            parentUnitId: mathSubjectId,
            code: "lu-mat-3-s1",
            name: "Matematika Kelas 3 Remedial",
            unitType: "subject",
            level: "Grade 3",
            description: "Remedial Grade 3 Mathematics unit for individualized learning plan",
            creditWeight: 4.0m,
            now: now,
            cancellationToken: cancellationToken);

        var bi4Id = await EnsureLearningUnitAsync(
            connection,
            transaction,
            academicsContext.LearningStructureId,
            LuBi4S1Id,
            parentUnitId: null,
            code: "lu-bi-4-s1",
            name: "Bahasa Indonesia Kelas 4",
            unitType: "subject",
            level: "Grade 4",
            description: "Standard Grade 4 Bahasa Indonesia constituent unit",
            creditWeight: 4.0m,
            now: now,
            cancellationToken: cancellationToken);

        var pjok4Id = await EnsureLearningUnitAsync(
            connection,
            transaction,
            academicsContext.LearningStructureId,
            LuPjok4S1Id,
            parentUnitId: null,
            code: "lu-pjok-4-s1",
            name: "PJOK Kelas 4 (Adaptive)",
            unitType: "subject",
            level: "Grade 4",
            description: "Adaptive Physical Education (PJOK) substitution unit",
            creditWeight: 2.0m,
            now: now,
            cancellationToken: cancellationToken);

        // ====================================================================
        // Persona 1: Budi Santoso (Active revised v2 plan + historical revised v1 plan)
        // ====================================================================
        var v1CreatedAt = new DateTimeOffset(sem1StartDate.AddDays(-14), TimeSpan.Zero);
        var v1ApprovedDate = DateOnly.FromDateTime(sem1StartDate.AddDays(-7));
        var v2CreatedAt = new DateTimeOffset(sem1StartDate.AddDays(5), TimeSpan.Zero);
        var v2ApprovedDate = DateOnly.FromDateTime(sem1StartDate.AddDays(7));
        var v2ApprovedTimestamp = new DateTimeOffset(v2ApprovedDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        // 1a. Budi Santoso v1 SLP (revised)
        await connection.ExecuteAsync(new CommandDefinition(
            _insertStudentLearningPlanSql,
            new
            {
                Id = BudiSlpV1Id,
                StudentId = budiStudentId,
                AcademicPeriodId = sem1Id,
                AcademicPeriodLabel = slpPeriodLabel,
                Status = "revised",
                Version = 1,
                Notes = "Individualized learning plan for Budi Santoso - Grade 3 Remedial Mathematics and Grade 4 Bahasa Indonesia.",
                CreatedAt = v1CreatedAt,
                CreatedBy = "academic-coordinator",
                UpdatedAt = (DateTimeOffset?)v2ApprovedTimestamp,
                UpdatedBy = "Ibu Ratna (VP Academics)"
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 1b. Budi Santoso v1 Constituent Learning Units
        await connection.ExecuteAsync(new CommandDefinition(
            _insertSlpLearningUnitSql,
            new
            {
                Id = Guid.NewGuid(),
                StudentLearningPlanId = BudiSlpV1Id,
                LearningUnitId = mat3Id,
                SequenceOrder = (int?)1,
                IsRequired = true,
                Notes = "Remediation: Grade 3 level",
                CreatedAt = v1CreatedAt,
                CreatedBy = "academic-coordinator"
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            _insertSlpLearningUnitSql,
            new
            {
                Id = Guid.NewGuid(),
                StudentLearningPlanId = BudiSlpV1Id,
                LearningUnitId = bi4Id,
                SequenceOrder = (int?)2,
                IsRequired = true,
                Notes = "Normal Grade 4 level",
                CreatedAt = v1CreatedAt,
                CreatedBy = "academic-coordinator"
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 1c. Budi Santoso v1 Initial Approval Record
        await connection.ExecuteAsync(new CommandDefinition(
            _insertSlpApprovalSql,
            new
            {
                Id = Guid.NewGuid(),
                StudentLearningPlanId = BudiSlpV1Id,
                PlanVersion = 1,
                ApprovedBy = "Ibu Ratna (VP Academics)",
                ApprovedDate = v1ApprovedDate,
                ApprovalType = "initial_approval",
                Notes = "Initial approval for Budi Santoso individualized learning plan v1.",
                CreatedAt = new DateTimeOffset(v1ApprovedDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                CreatedBy = "Ibu Ratna (VP Academics)"
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 1d. Budi Santoso v2 SLP (active)
        await connection.ExecuteAsync(new CommandDefinition(
            _insertStudentLearningPlanSql,
            new
            {
                Id = BudiSlpV2Id,
                StudentId = budiStudentId,
                AcademicPeriodId = sem1Id,
                AcademicPeriodLabel = slpPeriodLabel,
                Status = "active",
                Version = 2,
                Notes = "Revised learning plan: Added Adaptive PJOK substitution and 50% extra exam time accommodation.",
                CreatedAt = v2CreatedAt,
                CreatedBy = "academic-coordinator",
                UpdatedAt = (DateTimeOffset?)v2ApprovedTimestamp,
                UpdatedBy = "Ibu Ratna (VP Academics)"
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 1e. Budi Santoso v2 Constituent Learning Units (remedial math, bahasa indonesia, adaptive pjok)
        await connection.ExecuteAsync(new CommandDefinition(
            _insertSlpLearningUnitSql,
            new
            {
                Id = Guid.NewGuid(),
                StudentLearningPlanId = BudiSlpV2Id,
                LearningUnitId = mat3Id,
                SequenceOrder = (int?)1,
                IsRequired = true,
                Notes = "Remediation: Grade 3 level",
                CreatedAt = v2CreatedAt,
                CreatedBy = "academic-coordinator"
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            _insertSlpLearningUnitSql,
            new
            {
                Id = Guid.NewGuid(),
                StudentLearningPlanId = BudiSlpV2Id,
                LearningUnitId = bi4Id,
                SequenceOrder = (int?)2,
                IsRequired = true,
                Notes = "Normal Grade 4 level",
                CreatedAt = v2CreatedAt,
                CreatedBy = "academic-coordinator"
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            _insertSlpLearningUnitSql,
            new
            {
                Id = Guid.NewGuid(),
                StudentLearningPlanId = BudiSlpV2Id,
                LearningUnitId = pjok4Id,
                SequenceOrder = (int?)3,
                IsRequired = true,
                Notes = "Adaptive PJOK substitution (lu-pjok-4-s1 adaptive)",
                CreatedAt = v2CreatedAt,
                CreatedBy = "academic-coordinator"
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 1f. Budi Santoso v2 Academic Plan Adjustments (substitution + accommodation)
        await connection.ExecuteAsync(new CommandDefinition(
            _insertAcademicPlanAdjustmentSql,
            new
            {
                Id = Guid.NewGuid(),
                StudentLearningPlanId = BudiSlpV2Id,
                AdjustmentType = "substitution",
                Description = "Replace regular PJOK with Adaptive PJOK (lu-pjok-4-s1 adaptive)",
                Rationale = "Medical recommendation for modified physical activity curriculum",
                EffectiveDate = DateOnly.FromDateTime(sem1StartDate),
                EndDate = (DateOnly?)DateOnly.FromDateTime(sem1EndDate),
                ApprovedBy = "Ibu Ratna (VP Academics)",
                ApprovedDate = v2ApprovedDate,
                Notes = "Approved alongside SLP v2 revision",
                CreatedAt = v2CreatedAt,
                CreatedBy = "academic-coordinator"
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            _insertAcademicPlanAdjustmentSql,
            new
            {
                Id = Guid.NewGuid(),
                StudentLearningPlanId = BudiSlpV2Id,
                AdjustmentType = "accommodation",
                Description = "50% extra exam time accommodation for written assessments",
                Rationale = "Documented learning need assessment and processing speed accommodation",
                EffectiveDate = DateOnly.FromDateTime(sem1StartDate),
                EndDate = (DateOnly?)DateOnly.FromDateTime(sem1EndDate),
                ApprovedBy = "Ibu Ratna (VP Academics)",
                ApprovedDate = v2ApprovedDate,
                Notes = "Extra time accommodation across all written tests",
                CreatedAt = v2CreatedAt,
                CreatedBy = "academic-coordinator"
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 1g. Budi Santoso v2 Revision Approval Record
        await connection.ExecuteAsync(new CommandDefinition(
            _insertSlpApprovalSql,
            new
            {
                Id = Guid.NewGuid(),
                StudentLearningPlanId = BudiSlpV2Id,
                PlanVersion = 2,
                ApprovedBy = "Ibu Ratna (VP Academics)",
                ApprovedDate = v2ApprovedDate,
                ApprovalType = "revision_approval",
                Notes = "Revision approval incorporating Adaptive PJOK substitution and 50% extra exam time accommodation.",
                CreatedAt = v2ApprovedTimestamp,
                CreatedBy = "Ibu Ratna (VP Academics)"
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 1h. Linked academic_participations for Budi Santoso v2 active plan
        var budiUnits = new[]
        {
            mat3Id,
            bi4Id,
            pjok4Id
        };

        foreach (var unitId in budiUnits)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                _insertParticipationWithSlpSql,
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = budiStudentId,
                    LearningUnitId = unitId,
                    AcademicPeriodId = sem1Id,
                    AcademicPeriodLabel = slpPeriodLabel,
                    ParticipationType = "standard",
                    AttemptNumber = 1,
                    Source = "learning_plan",
                    SectionLabel = "ELC-A",
                    Status = "in_progress",
                    StartDate = DateOnly.FromDateTime(sem1StartDate),
                    EndDate = (DateOnly?)DateOnly.FromDateTime(sem1EndDate),
                    CompletionDate = (DateOnly?)null,
                    StudentLearningPlanId = (Guid?)BudiSlpV2Id,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));
        }

        // ====================================================================
        // Persona 2: Timotius (Draft v1 plan awaiting review)
        // ====================================================================
        await connection.ExecuteAsync(new CommandDefinition(
            _insertStudentLearningPlanSql,
            new
            {
                Id = TimotiusSlpV1Id,
                StudentId = timotiusId,
                AcademicPeriodId = sem1Id,
                AcademicPeriodLabel = slpPeriodLabel,
                Status = "draft",
                Version = 1,
                Notes = "Draft individualized learning plan for Timotius with proposed accommodations awaiting Academic Authority review.",
                CreatedAt = now,
                CreatedBy = "academic-coordinator",
                UpdatedAt = (DateTimeOffset?)null,
                UpdatedBy = (string?)null
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            _insertSlpLearningUnitSql,
            new
            {
                Id = Guid.NewGuid(),
                StudentLearningPlanId = TimotiusSlpV1Id,
                LearningUnitId = bi4Id,
                SequenceOrder = (int?)1,
                IsRequired = true,
                Notes = "Standard Grade 4 curriculum unit with proposed accommodations",
                CreatedAt = now,
                CreatedBy = "academic-coordinator"
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            _insertAcademicPlanAdjustmentSql,
            new
            {
                Id = Guid.NewGuid(),
                StudentLearningPlanId = TimotiusSlpV1Id,
                AdjustmentType = "accommodation",
                Description = "Proposed 25% extra exam time accommodation for written assessments",
                Rationale = "Awaiting final medical evaluation and formal Academic Authority review",
                EffectiveDate = DateOnly.FromDateTime(sem1StartDate),
                EndDate = (DateOnly?)null,
                ApprovedBy = "",
                ApprovedDate = new DateOnly(1900, 1, 1),
                Notes = "Pending review by Academic Authority",
                CreatedAt = now,
                CreatedBy = "academic-coordinator"
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        return budiUnits.Length;
    }

    private static async Task<Guid> EnsureLearningUnitAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        Guid learningStructureId,
        Guid preferredId,
        Guid? parentUnitId,
        string code,
        string name,
        string unitType,
        string level,
        string description,
        decimal creditWeight,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await connection.QueryFirstOrDefaultAsync<Guid?>(
            new CommandDefinition(
                "SELECT id FROM academics.learning_units WHERE learning_structure_id = @StructureId AND code = @Code",
                new { StructureId = learningStructureId, Code = code },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (existing.HasValue && existing.Value != Guid.Empty)
        {
            return existing.Value;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            _insertLearningUnitSql,
            new
            {
                Id = preferredId,
                LearningStructureId = learningStructureId,
                ParentUnitId = parentUnitId,
                Code = code,
                Name = name,
                UnitType = unitType,
                Level = level,
                Description = description,
                CreditWeight = creditWeight,
                Status = "active",
                Notes = $"Seeded learning unit {code} for Hagios Student Learning Plans",
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        return preferredId;
    }
}
