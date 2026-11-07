namespace Elsasa.Sis.Academics.Features.DemoData;

using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using Dapper;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Contracts.Operations;
using Elsasa.Sis.Shared.Database;

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

    // Flagship student personas for Submodule 4 Standing & Progression (TD-08, GAP-008)
    public static readonly Guid RinaKusumaStudentId = new("00000000-0000-0000-0004-000000000108");
    public static readonly Guid BudiSantosoStudentId = DemoDataConstants.BudiStudentId;
    public static readonly Guid TimotiusStudentId = DemoDataConstants.TimotiusApplicantId;

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

            var studentsList = new List<(Guid Id, string FullName, string? GovId)>();
            var studentLookup = new Dictionary<Guid, (string Name, string? Nisn)>();
            try
            {
                var students = await connection.QueryAsync<(Guid Id, string FullName, string? GovId)>(
                    new CommandDefinition(
                        "SELECT id, full_name, government_id_number FROM students.students",
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                foreach (var s in students)
                {
                    studentsList.Add(s);
                    studentLookup[s.Id] = (s.FullName, s.GovId);
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

            var timotiusStudent = studentsList.FirstOrDefault(s => s.FullName.Contains("Timotius", StringComparison.OrdinalIgnoreCase));
            var timotiusId = timotiusStudent != default && timotiusStudent.Id != Guid.Empty ? timotiusStudent.Id : TimotiusStudentId;

            studentLookup[rinaId] = ("Rina Kusuma", "0061234567");
            studentLookup[budiStudentId] = ("Budi Santoso", "0071234567");
            studentLookup[timotiusId] = ("Timotius", "0051234567");

            var allTargetStudentIds = enrolledStudentIds.ToList();
            if (!allTargetStudentIds.Contains(rinaId)) allTargetStudentIds.Add(rinaId);
            if (!allTargetStudentIds.Contains(timotiusId)) allTargetStudentIds.Add(timotiusId);

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

            // S1: Create 30 learning activities spread across the semester
            for (int i = 0; i < 30; i++)
            {
                var unitId = paceUnitIds[i % paceUnitIds.Count];
                var activityId = Guid.NewGuid();
                sem1ActivityIds.Add(activityId);

                var activityDate = sem1StartDate.AddDays((i * 5) % 100);
                await connection.ExecuteAsync(new CommandDefinition(
                    _insertLearningActivitySql,
                    new
                    {
                        Id = activityId,
                        LearningUnitId = unitId,
                        ActivityType = "lesson",
                        Title = $"S1 Lesson {i + 1}",
                        ActivityDate = activityDate,
                        Status = "conducted",
                        IsAttendanceRequired = true,
                        CreatedBy = _createdBy
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
            }

            // S2: Create 30 learning activities spread across the semester
            for (int i = 0; i < 30; i++)
            {
                var unitId = paceUnitIds[(i + 10) % paceUnitIds.Count];
                var activityId = Guid.NewGuid();
                sem2ActivityIds.Add(activityId);

                var activityDate = sem2StartDate.AddDays((i * 5) % 100);
                await connection.ExecuteAsync(new CommandDefinition(
                    _insertLearningActivitySql,
                    new
                    {
                        Id = activityId,
                        LearningUnitId = unitId,
                        ActivityType = "lesson",
                        Title = $"S2 Lesson {i + 1}",
                        ActivityDate = activityDate,
                        Status = "conducted",
                        IsAttendanceRequired = true,
                        CreatedBy = _createdBy
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
            }

            // ====================================================================
            // 2. Create academic participations for all ~48 enrolled students
            //    S1: completed participations (historical)
            //    S2: in_progress participations (active)
            // ====================================================================
            var allParticipationIds = new Dictionary<Guid, List<Guid>>(); // studentId -> [participationIds]
            var allS1ParticipationUnitLinks = new Dictionary<Guid, List<(Guid participationId, Guid unitId)>>();
            var allS2ParticipationUnitLinks = new Dictionary<Guid, List<(Guid participationId, Guid unitId)>>();

            foreach (var studentId in allTargetStudentIds)
            {
                var s1ParticipationIds = new List<Guid>();
                var s2ParticipationIds = new List<Guid>();
                var s1Links = new List<(Guid participationId, Guid unitId)>();
                var s2Links = new List<(Guid participationId, Guid unitId)>();

                // S1: Complete all 30 PACE units as completed participations
                for (int i = 0; i < paceUnitIds.Count; i++)
                {
                    var participationId = Guid.NewGuid();
                    s1ParticipationIds.Add(participationId);
                    s1Links.Add((participationId, paceUnitIds[i]));

                    var startDate = sem1StartDate.AddDays(random.Next(0, 30));
                    var endDate = sem1EndDate.AddDays(-random.Next(0, 15));
                    if (endDate < startDate) endDate = startDate.AddDays(1);

                    await connection.ExecuteAsync(new CommandDefinition(
                        _insertParticipationSql,
                        new
                        {
                            Id = participationId,
                            StudentId = studentId,
                            LearningUnitId = paceUnitIds[i],
                            AcademicPeriodId = sem1Id,
                            AcademicPeriodLabel = sem1Label,
                            ParticipationType = "standard",
                            AttemptNumber = 1,
                            Source = "assigned",
                            SectionLabel = GetSectionLabel(studentId, allTargetStudentIds, random, rinaId, budiStudentId, timotiusId),
                            Status = "completed",
                            StartDate = startDate,
                            EndDate = endDate,
                            CompletionDate = endDate,
                            CreatedBy = _createdBy
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                }

                // S2: In-progress participations in all 30 PACE units
                for (int i = 0; i < paceUnitIds.Count; i++)
                {
                    var participationId = Guid.NewGuid();
                    s2ParticipationIds.Add(participationId);
                    s2Links.Add((participationId, paceUnitIds[i]));

                    await connection.ExecuteAsync(new CommandDefinition(
                        _insertParticipationSql,
                        new
                        {
                            Id = participationId,
                            StudentId = studentId,
                            LearningUnitId = paceUnitIds[i],
                            AcademicPeriodId = sem2Id,
                            AcademicPeriodLabel = sem2Label,
                            ParticipationType = "standard",
                            AttemptNumber = 1,
                            Source = "assigned",
                            SectionLabel = GetSectionLabel(studentId, allTargetStudentIds, random, rinaId, budiStudentId, timotiusId),
                            Status = "in_progress",
                            StartDate = sem2StartDate,
                            EndDate = (DateOnly?)null,
                            CompletionDate = (DateOnly?)null,
                            CreatedBy = _createdBy
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                }

                allParticipationIds[studentId] = s1ParticipationIds.Concat(s2ParticipationIds).ToList();
                allS1ParticipationUnitLinks[studentId] = s1Links;
                allS2ParticipationUnitLinks[studentId] = s2Links;
                participationsCreated += s1ParticipationIds.Count + s2ParticipationIds.Count;
            }

            // ====================================================================
            // 3. Create ~1,200 attendance records across S1 and S2
            //    S1: ~700 records, S2: ~500 records
            // ====================================================================
            await CreateAttendanceRecordsAsync(
                connection, transaction, sem1ActivityIds, allTargetStudentIds,
                jonathanId,
                sem1StartDate, now, _createdBy, random, targetCount: 700,
                cancellationToken);
            attendanceRecordsCreated += 700;

            await CreateAttendanceRecordsAsync(
                connection, transaction, sem2ActivityIds, allTargetStudentIds,
                jonathanId,
                sem2StartDate, now, _createdBy, random, targetCount: 500,
                cancellationToken);
            attendanceRecordsCreated += 500;

            // ====================================================================
            // 4. Create PACE test assessments and academic results
            // ====================================================================
            var allS1Results = new Dictionary<Guid, List<(Guid resultId, Guid assessmentId, Guid unitId, decimal score, string gradeLetter, string gradeLabel)>>();

            // --- S1: Certified results (approved & published) for all students ---
            foreach (var studentId in allTargetStudentIds)
            {
                var s1Links = allS1ParticipationUnitLinks[studentId];
                var s1Results = new List<(Guid resultId, Guid assessmentId, Guid unitId, decimal score, string gradeLetter, string gradeLabel)>();

                foreach (var (participationId, unitId) in s1Links)
                {
                    // Create assessment (PACE Test)
                    var assessmentId = Guid.NewGuid();
                    var evaluatedDate = sem1EndDate.AddDays(-random.Next(0, 10));

                    await connection.ExecuteAsync(new CommandDefinition(
                        _insertAssessmentSql,
                        new
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
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                    // Generate deterministic score: majority passing, some failing
                    var score = GenerateS1Score(studentId, unitId, random, rinaId, budiStudentId, timotiusId, mathUnits, bibleUnits);

                    var resultId = Guid.NewGuid();
                    var isPassing = score >= 80.0m;
                    var gradeLetter = GetGradeLetter(score);
                    var gradeLabel = GetGradeLabel(score);

                    await connection.ExecuteAsync(new CommandDefinition(
                        _insertResultSql,
                        new
                        {
                            Id = resultId,
                            AssessmentId = assessmentId,
                            StudentId = studentId,
                            AttemptNumber = 1,
                            ReplacesResultId = (Guid?)null,
                            IsFinal = 1,
                            ScoreNumeric = score,
                            ScoreMax = 100.00m,
                            GradeLetter = gradeLetter,
                            GradeLabel = gradeLabel,
                            EvaluationModel = "numeric",
                            EvaluatedBy = "academic-coordinator",
                            EvaluatedDate = evaluatedDate,
                            ResultStatus = "approved",
                            ApprovedBy = "Academic Authority",
                            ApprovedDate = evaluatedDate.AddDays(1),
                            PublishedDate = evaluatedDate.AddDays(2),
                            Notes = isPassing ? "Mastery achieved (KKM 80%)" : "Below KKM - requires remediation",
                            CreatedBy = _createdBy
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                    // Create assessment evidence (test paper)
                    await connection.ExecuteAsync(new CommandDefinition(
                        _insertEvidenceSql,
                        new
                        {
                            Id = Guid.NewGuid(),
                            AssessmentId = assessmentId,
                            StudentId = studentId,
                            EvidenceType = "test_paper",
                            CollectedDate = evaluatedDate,
                            EvidenceReference = $"evidence/s1/{studentId}/{assessmentId}/paper.pdf",
                            CreatedBy = _createdBy
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                    s1Results.Add((resultId, assessmentId, unitId, score, gradeLetter, gradeLabel));
                    resultsCreated++;
                }

                allS1Results[studentId] = s1Results;
            }

            // --- S2: Active results with live hooks for flagship personas ---
            var allS2Results = new Dictionary<Guid, List<(Guid resultId, Guid assessmentId, Guid unitId)>>();
            foreach (var studentId in allTargetStudentIds)
            {
                var s2Links = allS2ParticipationUnitLinks[studentId];
                var s2Results = new List<(Guid resultId, Guid assessmentId, Guid unitId)>();

                // Only create results for the first 6 PACE units per student for S2
                // (to keep results manageable while still having the live hooks)
                var selectedUnits = s2Links.Take(6).ToList();

                foreach (var (participationId, unitId) in selectedUnits)
                {
                    var assessmentId = Guid.NewGuid();
                    var evaluatedDate = baseDate.AddDays(-random.Next(0, 15));

                    await connection.ExecuteAsync(new CommandDefinition(
                        _insertAssessmentSql,
                        new
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
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                    // Default: provisional result awaiting evaluation
                    var resultId = Guid.NewGuid();
                    var score = GenerateS2Score(studentId, unitId, random);
                    var gradeLetter = GetGradeLetter(score);
                    var gradeLabel = GetGradeLabel(score);

                    await connection.ExecuteAsync(new CommandDefinition(
                        _insertResultSql,
                        new
                        {
                            Id = resultId,
                            AssessmentId = assessmentId,
                            StudentId = studentId,
                            AttemptNumber = 1,
                            ReplacesResultId = (Guid?)null,
                            IsFinal = 0,
                            ScoreNumeric = score,
                            ScoreMax = 100.00m,
                            GradeLetter = gradeLetter,
                            GradeLabel = gradeLabel,
                            EvaluationModel = "numeric",
                            EvaluatedBy = "teacher-01",
                            EvaluatedDate = evaluatedDate,
                            ResultStatus = "provisional",
                            ApprovedBy = (string?)null,
                            ApprovedDate = (DateOnly?)null,
                            PublishedDate = (DateOnly?)null,
                            Notes = "Awaiting supervisor evaluation",
                            CreatedBy = _createdBy
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                    s2Results.Add((resultId, assessmentId, unitId));
                    resultsCreated++;
                }

                // Override for Jonathan Pratama: one result awaiting supervisor approval
                if (studentId == jonathanId)
                {
                    // Find a result to make it await approval (provisional, not evaluated)
                    if (s2Results.Count > 0)
                    {
                        var (jonathanResultId, jonathanAssessmentId, _) = s2Results[0];
                        await connection.ExecuteAsync(new CommandDefinition(
                            _updateResultSql,
                            new
                            {
                                Id = jonathanResultId,
                                AssessmentId = jonathanAssessmentId,
                                StudentId = jonathanId,
                                AttemptNumber = 1,
                                ReplacesResultId = (Guid?)null,
                                IsFinal = 0,
                                ScoreNumeric = 92.00m,
                                ScoreMax = 100.00m,
                                GradeLetter = "A",
                                GradeLabel = "Sangat Mahir",
                                EvaluationModel = "numeric",
                                EvaluatedBy = "teacher-01",
                                EvaluatedDate = baseDate.AddDays(-2),
                                ResultStatus = "provisional",
                                ApprovedBy = (string?)null,
                                ApprovedDate = (DateOnly?)null,
                                PublishedDate = (DateOnly?)null,
                                Notes = "PACE score awaiting supervisor approval (live hook for SCR-ACD-03)",
                                CreatedBy = _createdBy
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken));
                    }
                }

                // Override for Grace Santoso: retake trail
                if (studentId == graceId)
                {
                    // First attempt: score 68% (failing) - provisional
                    if (s2Results.Count > 0)
                    {
                        var (graceResultId1, graceAssessmentId1, graceUnitId1) = s2Results[0];
                        await connection.ExecuteAsync(new CommandDefinition(
                            _updateResultSql,
                            new
                            {
                                Id = graceResultId1,
                                AssessmentId = graceAssessmentId1,
                                StudentId = graceId,
                                AttemptNumber = 1,
                                ReplacesResultId = (Guid?)null,
                                IsFinal = 0,
                                ScoreNumeric = 68.00m,
                                ScoreMax = 100.00m,
                                GradeLetter = "D",
                                GradeLabel = "Kurang",
                                EvaluationModel = "numeric",
                                EvaluatedBy = "teacher-01",
                                EvaluatedDate = baseDate.AddDays(-10),
                                ResultStatus = "provisional",
                                ApprovedBy = (string?)null,
                                ApprovedDate = (DateOnly?)null,
                                PublishedDate = (DateOnly?)null,
                                Notes = "Initial attempt - below KKM (68%), failed. Remediation completed.",
                                CreatedBy = _createdBy
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken));
                    }

                    // Second attempt (retake): score 85% (passing) with replaces_result_id
                    if (s2Results.Count > 1)
                    {
                        var (graceResultId2, graceAssessmentId2, graceUnitId2) = s2Results[1];
                        var firstResultId = s2Results[0].resultId;
                        await connection.ExecuteAsync(new CommandDefinition(
                            _updateResultSql,
                            new
                            {
                                Id = graceResultId2,
                                AssessmentId = graceAssessmentId2,
                                StudentId = graceId,
                                AttemptNumber = 2,
                                ReplacesResultId = firstResultId,
                                IsFinal = 1,
                                ScoreNumeric = 85.00m,
                                ScoreMax = 100.00m,
                                GradeLetter = "B+",
                                GradeLabel = "Memuaskan",
                                EvaluationModel = "numeric",
                                EvaluatedBy = "teacher-01",
                                EvaluatedDate = baseDate.AddDays(-5),
                                ResultStatus = "provisional",
                                ApprovedBy = (string?)null,
                                ApprovedDate = (DateOnly?)null,
                                PublishedDate = (DateOnly?)null,
                                Notes = "Retake attempt - remediation completed, score 85% (passing). Awaiting final approval.",
                                CreatedBy = _createdBy
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken));
                    }
                }

                // Override for Budi Wijaya: submitted PACE test awaiting evaluation
                if (studentId == budiId)
                {
                    if (s2Results.Count > 0)
                    {
                        var (budiResultId, budiAssessmentId, _) = s2Results[0];

                        // Update assessment title to explicitly match Science PACE 1049
                        await connection.ExecuteAsync(new CommandDefinition(
                            "UPDATE academics.assessments SET title = @Title WHERE id = @AssessmentId",
                            new { Title = "PACE Test - Science PACE 1049", AssessmentId = budiAssessmentId },
                            transaction: transaction,
                            cancellationToken: cancellationToken));

                        await connection.ExecuteAsync(new CommandDefinition(
                            _updateResultSql,
                            new
                            {
                                Id = budiResultId,
                                AssessmentId = budiAssessmentId,
                                StudentId = budiId,
                                AttemptNumber = 1,
                                ReplacesResultId = (Guid?)null,
                                IsFinal = 0,
                                ScoreNumeric = (decimal?)null,
                                ScoreMax = 100.00m,
                                GradeLetter = (string?)null,
                                GradeLabel = (string?)null,
                                EvaluationModel = "numeric",
                                EvaluatedBy = "teacher-01",
                                EvaluatedDate = baseDate.AddDays(-2),
                                ResultStatus = "provisional",
                                ApprovedBy = (string?)null,
                                ApprovedDate = (DateOnly?)null,
                                PublishedDate = (DateOnly?)null,
                                Notes = "Live submitted PACE test awaiting supervisor evaluation (live hook for SCR-ACD-04)",
                                CreatedBy = _createdBy
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken));
                    }
                }

                allS2Results[studentId] = s2Results;
            }

            // ====================================================================
            // 5. Create coursework assignments and submissions for S2
            // ====================================================================
            var s2AssignmentIds = new List<Guid>();
            foreach (var activityId in sem2ActivityIds.GetRange(0, 10))
            {
                var assignmentId = Guid.NewGuid();
                s2AssignmentIds.Add(assignmentId);

                await connection.ExecuteAsync(new CommandDefinition(
                    _insertAssignmentSql,
                    new
                    {
                        Id = assignmentId,
                        LearningActivityId = activityId,
                        Title = $"Coursework Assignment {s2AssignmentIds.Count}",
                        Description = "Regular coursework task for S2",
                        ReleaseDate = sem2StartDate.AddDays((s2AssignmentIds.Count - 1) * 3),
                        DueDate = sem2StartDate.AddDays((s2AssignmentIds.Count - 1) * 3 + 14),
                        Status = "released",
                        CreatedBy = _createdBy
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
            }

            // Create submissions for a subset of students
            foreach (var assignmentId in s2AssignmentIds)
            {
                var selectedStudents = enrolledStudentIds
                    .OrderBy(_ => random.Next())
                    .Take(Math.Min(20, enrolledStudentIds.Count))
                    .ToList();

                foreach (var studentId in selectedStudents)
                {
                    var isBudi = studentId == budiId;
                    await connection.ExecuteAsync(new CommandDefinition(
                        _insertSubmissionSql,
                        new
                        {
                            Id = Guid.NewGuid(),
                            AssignmentId = assignmentId,
                            StudentId = studentId,
                            Status = isBudi ? "submitted" : "submitted",
                            SubmittedDate = DateTime.SpecifyKind(
                                baseDate.AddDays(-random.Next(0, 7)), DateTimeKind.Utc),
                            IsResubmission = 0,
                            CreatedBy = _createdBy
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                }
            }

            // ====================================================================
            // 6. Semester 1: Create certified report cards ("Buku Rapor Merdeka")
            // ====================================================================
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

                await connection.ExecuteAsync(new CommandDefinition(
                    _insertReportCardSql,
                    new
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
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
                reportCardsCreated++;

                // Create report card entries
                displayOrder = 1;
                foreach (var (resultId, _, unitId, _, _, _) in s1Results)
                {
                    var entryId = Guid.NewGuid();
                    await connection.ExecuteAsync(new CommandDefinition(
                        _insertReportCardEntrySql,
                        new
                        {
                            Id = entryId,
                            ReportCardId = cardId,
                            LearningUnitId = unitId,
                            AcademicResultId = resultId,
                            DisplayOrder = displayOrder++,
                            TeacherComment = "Satisfactory mastery demonstrated.",
                            CreatedBy = _createdBy
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                }
            }

            // ====================================================================
            // 7. Semester 1: Create cumulative transcripts
            // ====================================================================
            foreach (var studentId in allTargetStudentIds)
            {
                var s1Results = allS1Results.TryGetValue(studentId, out var results) ? results : [];
                var (studentName, studentNisn) = GetStudentInfo(studentId);

                // Calculate GPA and build course entries from S1 results
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

                decimal? gpa = resultCount > 0 ? Math.Round(totalScore / resultCount / 20m, 2) : (decimal?)null; // Scale 100 to 5.0

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

                await connection.ExecuteAsync(new CommandDefinition(
                    _insertTranscriptSql,
                    new
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
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
                transcriptsCreated++;
            }

            // ====================================================================
            // 8. Seed Submodule 4: Standings, Progression, Interventions & Achievements (IS012, TD-08)
            // ====================================================================
            await SeedStandingAndProgressionSubmoduleAsync(
                connection,
                transaction,
                operationsContext,
                academicsContext,
                unitLookup,
                rinaId,
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
        Guid rinaId,
        Guid budiStudentId,
        Guid timotiusId,
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

        var bibleUnits = paceUnitIds
            .Where(id => unitLookup.TryGetValue(id, out var u) && (u.Code.StartsWith("B", StringComparison.OrdinalIgnoreCase) || u.Name.Contains("Bible", StringComparison.OrdinalIgnoreCase) || u.Name.Contains("Christian", StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (bibleUnits.Count == 0) bibleUnits = paceUnitIds.Skip(24).Take(6).ToList();

        var mathSubjectId = academicsContext.SubjectUnitIds.GetValueOrDefault("MATH", mathUnits.FirstOrDefault());

        // ---------------------------------------------------------------------
        // 1. Learning Progress
        // ---------------------------------------------------------------------

        // 1a. Rina Kusuma: completed all Grade 4 units
        foreach (var unitId in paceUnitIds)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                _insertLearningProgressSql,
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = rinaId,
                    LearningUnitId = unitId,
                    AcademicPeriodId = sem1Id,
                    Status = "completed",
                    Basis = "assessment_evidence",
                    CompletionDate = (DateOnly?)sem1EndDate,
                    Notes = "Completed Grade 4 PACE unit expectations with mastery",
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                _insertLearningProgressSql,
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = rinaId,
                    LearningUnitId = unitId,
                    AcademicPeriodId = sem2Id,
                    Status = "completed",
                    Basis = "assessment_evidence",
                    CompletionDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-5)),
                    Notes = "Completed Grade 4 PACE unit expectations with mastery",
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));
        }

        // 1b. Budi Santoso: needs_remediation Math; other units completed
        foreach (var unitId in paceUnitIds)
        {
            var isMath = mathUnits.Contains(unitId);
            var status = isMath ? "needs_remediation" : "completed";
            var compDate = isMath ? (DateOnly?)null : sem1EndDate;
            var notes = isMath
                ? "Assessment score below KKM (80%) in Mathematics; requires remediation packet"
                : "Completed Grade 4 PACE unit";

            await connection.ExecuteAsync(new CommandDefinition(
                _insertLearningProgressSql,
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = budiStudentId,
                    LearningUnitId = unitId,
                    AcademicPeriodId = sem1Id,
                    Status = status,
                    Basis = "assessment_evidence",
                    CompletionDate = compDate,
                    Notes = notes,
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                _insertLearningProgressSql,
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = budiStudentId,
                    LearningUnitId = unitId,
                    AcademicPeriodId = sem2Id,
                    Status = isMath ? "needs_remediation" : "in_progress",
                    Basis = isMath ? "assessment_evidence" : "learning_work",
                    CompletionDate = (DateOnly?)null,
                    Notes = isMath ? "Active Mathematics remediation plan underway" : "In progress unit",
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));
        }

        // 1c. Timotius: completed Christian Education with explicit out-of-period completion date
        var outOfPeriodDate = sem1EndDate.AddDays(14); // 14 days after S1 end date (holiday packet completion)
        foreach (var unitId in paceUnitIds)
        {
            var isBible = bibleUnits.Contains(unitId);
            var status = isBible ? "completed" : "needs_remediation";
            var compDate = isBible ? (DateOnly?)outOfPeriodDate : (DateOnly?)null;
            var notes = isBible
                ? $"Completed out-of-period via authorized holiday study session on {outOfPeriodDate:yyyy-MM-dd}"
                : "Unfulfilled PACE requirements; below passing threshold";

            await connection.ExecuteAsync(new CommandDefinition(
                _insertLearningProgressSql,
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = timotiusId,
                    LearningUnitId = unitId,
                    AcademicPeriodId = sem1Id,
                    Status = status,
                    Basis = "assessment_evidence",
                    CompletionDate = compDate,
                    Notes = notes,
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                _insertLearningProgressSql,
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = timotiusId,
                    LearningUnitId = unitId,
                    AcademicPeriodId = sem2Id,
                    Status = isBible ? "completed" : "not_started",
                    Basis = isBible ? "assessment_evidence" : "learning_work",
                    CompletionDate = isBible ? (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-10)) : (DateOnly?)null,
                    Notes = isBible ? "Christian Education completed" : "Pending study session",
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));
        }

        // ---------------------------------------------------------------------
        // 2. Academic Standings
        // ---------------------------------------------------------------------
        // S1 Determinations
        var rinaStandingS1Id = Guid.NewGuid();
        var budiStandingS1Id = Guid.NewGuid();
        var timotiusStandingS1Id = Guid.NewGuid();

        // 2a. Rina Kusuma: good_standing
        await connection.ExecuteAsync(new CommandDefinition(
            _insertAcademicStandingSql,
            new
            {
                Id = rinaStandingS1Id,
                StudentId = rinaId,
                AcademicPeriodId = sem1Id,
                Standing = "good_standing",
                PolicyReference = "SK-AKAD/2026/POLICY-01",
                Conditions = (string?)null,
                IsException = false,
                ExceptionReason = (string?)null,
                CommitteeReference = (string?)null,
                AuthorizedBy = "Academic Authority",
                DeterminedAt = sem1EndDate.AddDays(2),
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 2b. Budi Santoso: conditional_standing with SK Dewan Guru SK-DG/2026/06-01 and math conditions
        await connection.ExecuteAsync(new CommandDefinition(
            _insertAcademicStandingSql,
            new
            {
                Id = budiStandingS1Id,
                StudentId = budiStudentId,
                AcademicPeriodId = sem1Id,
                Standing = "conditional_standing",
                PolicyReference = "SK-AKAD/2026/POLICY-01",
                Conditions = "Must complete Mathematics remediation during holiday packet and pass reassessment before Grade 5 commencement.",
                IsException = true,
                ExceptionReason = "Academic council approval for conditional standing subject to mathematics remediation agreement",
                CommitteeReference = "SK-DG/2026/06-01",
                AuthorizedBy = "Academic Authority",
                DeterminedAt = sem1EndDate.AddDays(2),
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 2c. Timotius: not_in_good_standing
        await connection.ExecuteAsync(new CommandDefinition(
            _insertAcademicStandingSql,
            new
            {
                Id = timotiusStandingS1Id,
                StudentId = timotiusId,
                AcademicPeriodId = sem1Id,
                Standing = "not_in_good_standing",
                PolicyReference = "SK-AKAD/2026/POLICY-01",
                Conditions = "Multiple learning units below passing threshold and chronic non-attendance (<75%).",
                IsException = false,
                ExceptionReason = (string?)null,
                CommitteeReference = (string?)null,
                AuthorizedBy = "Academic Authority",
                DeterminedAt = sem1EndDate.AddDays(2),
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // S2 Determinations (Live / active period determinations)
        var rinaStandingS2Id = Guid.NewGuid();
        var budiStandingS2Id = Guid.NewGuid();
        var timotiusStandingS2Id = Guid.NewGuid();

        await connection.ExecuteAsync(new CommandDefinition(
            _insertAcademicStandingSql,
            new
            {
                Id = rinaStandingS2Id,
                StudentId = rinaId,
                AcademicPeriodId = sem2Id,
                Standing = "good_standing",
                PolicyReference = "SK-AKAD/2026/POLICY-01",
                Conditions = (string?)null,
                IsException = false,
                ExceptionReason = (string?)null,
                CommitteeReference = (string?)null,
                AuthorizedBy = "Academic Authority",
                DeterminedAt = baseDate.AddDays(-2),
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            _insertAcademicStandingSql,
            new
            {
                Id = budiStandingS2Id,
                StudentId = budiStudentId,
                AcademicPeriodId = sem2Id,
                Standing = "conditional_standing",
                PolicyReference = "SK-AKAD/2026/POLICY-01",
                Conditions = "Must complete Mathematics remediation during holiday packet and pass reassessment before Grade 5 commencement.",
                IsException = true,
                ExceptionReason = "Academic council approval for conditional standing subject to mathematics remediation agreement",
                CommitteeReference = "SK-DG/2026/06-01",
                AuthorizedBy = "Academic Authority",
                DeterminedAt = baseDate.AddDays(-2),
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            _insertAcademicStandingSql,
            new
            {
                Id = timotiusStandingS2Id,
                StudentId = timotiusId,
                AcademicPeriodId = sem2Id,
                Standing = "not_in_good_standing",
                PolicyReference = "SK-AKAD/2026/POLICY-01",
                Conditions = "Multiple learning units below passing threshold and chronic non-attendance (<75%).",
                IsException = false,
                ExceptionReason = (string?)null,
                CommitteeReference = (string?)null,
                AuthorizedBy = "Academic Authority",
                DeterminedAt = baseDate.AddDays(-2),
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // ---------------------------------------------------------------------
        // 3. Promotion Recommendations
        // ---------------------------------------------------------------------
        // S1 Recommendations
        await connection.ExecuteAsync(new CommandDefinition(
            _insertPromotionRecommendationSql,
            new
            {
                Id = Guid.NewGuid(),
                StudentId = rinaId,
                AcademicPeriodId = sem1Id,
                AcademicStandingId = (Guid?)rinaStandingS1Id,
                Recommendation = "promotion_eligible",
                FromLevel = "Grade 4",
                RecommendedToLevel = "Grade 5",
                Conditions = (string?)null,
                SupportingEvidence = "Completed all Grade 4 PACE units with Good Standing and mastery GPA.",
                IsException = false,
                ExceptionReason = (string?)null,
                CommitteeReference = (string?)null,
                AuthorizedBy = "Academic Authority",
                IssuedAt = sem1EndDate.AddDays(3),
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            _insertPromotionRecommendationSql,
            new
            {
                Id = Guid.NewGuid(),
                StudentId = budiStudentId,
                AcademicPeriodId = sem1Id,
                AcademicStandingId = (Guid?)budiStandingS1Id,
                Recommendation = "conditional_promotion",
                FromLevel = "Grade 4",
                RecommendedToLevel = "Grade 5",
                Conditions = "Conditional advancement granted subject to completion of Math remediation tutoring packet prior to term start.",
                SupportingEvidence = "Conditional standing certified per SK Dewan Guru SK-DG/2026/06-01; acceptable progress in other 4 core subjects.",
                IsException = true,
                ExceptionReason = "Dewan Guru certified conditional promotion with mandatory mathematics tutoring contract",
                CommitteeReference = "SK-DG/2026/06-01",
                AuthorizedBy = "Academic Authority",
                IssuedAt = sem1EndDate.AddDays(3),
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            _insertPromotionRecommendationSql,
            new
            {
                Id = Guid.NewGuid(),
                StudentId = timotiusId,
                AcademicPeriodId = sem1Id,
                AcademicStandingId = (Guid?)timotiusStandingS1Id,
                Recommendation = "retention_recommended",
                FromLevel = "Grade 4",
                RecommendedToLevel = (string?)null,
                Conditions = (string?)null,
                SupportingEvidence = "Not in good standing. Retention recommended to consolidate foundational PACE units in Grade 4.",
                IsException = false,
                ExceptionReason = (string?)null,
                CommitteeReference = (string?)null,
                AuthorizedBy = "Academic Authority",
                IssuedAt = sem1EndDate.AddDays(3),
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // S2 Recommendations
        await connection.ExecuteAsync(new CommandDefinition(
            _insertPromotionRecommendationSql,
            new
            {
                Id = Guid.NewGuid(),
                StudentId = rinaId,
                AcademicPeriodId = sem2Id,
                AcademicStandingId = (Guid?)rinaStandingS2Id,
                Recommendation = "promotion_eligible",
                FromLevel = "Grade 4",
                RecommendedToLevel = "Grade 5",
                Conditions = (string?)null,
                SupportingEvidence = "Completed all Grade 4 PACE units with Good Standing and mastery GPA.",
                IsException = false,
                ExceptionReason = (string?)null,
                CommitteeReference = (string?)null,
                AuthorizedBy = "Academic Authority",
                IssuedAt = baseDate.AddDays(-1),
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            _insertPromotionRecommendationSql,
            new
            {
                Id = Guid.NewGuid(),
                StudentId = budiStudentId,
                AcademicPeriodId = sem2Id,
                AcademicStandingId = (Guid?)budiStandingS2Id,
                Recommendation = "conditional_promotion",
                FromLevel = "Grade 4",
                RecommendedToLevel = "Grade 5",
                Conditions = "Conditional advancement granted subject to completion of Math remediation tutoring packet prior to term start.",
                SupportingEvidence = "Conditional standing certified per SK Dewan Guru SK-DG/2026/06-01; acceptable progress in other 4 core subjects.",
                IsException = true,
                ExceptionReason = "Dewan Guru certified conditional promotion with mandatory mathematics tutoring contract",
                CommitteeReference = "SK-DG/2026/06-01",
                AuthorizedBy = "Academic Authority",
                IssuedAt = baseDate.AddDays(-1),
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            _insertPromotionRecommendationSql,
            new
            {
                Id = Guid.NewGuid(),
                StudentId = timotiusId,
                AcademicPeriodId = sem2Id,
                AcademicStandingId = (Guid?)timotiusStandingS2Id,
                Recommendation = "retention_recommended",
                FromLevel = "Grade 4",
                RecommendedToLevel = (string?)null,
                Conditions = (string?)null,
                SupportingEvidence = "Not in good standing. Retention recommended to consolidate foundational PACE units in Grade 4.",
                IsException = false,
                ExceptionReason = (string?)null,
                CommitteeReference = (string?)null,
                AuthorizedBy = "Academic Authority",
                IssuedAt = baseDate.AddDays(-1),
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // ---------------------------------------------------------------------
        // 4. Academic Interventions
        // ---------------------------------------------------------------------
        // 4a. Budi Santoso: active Math remediation plan
        await connection.ExecuteAsync(new CommandDefinition(
            _insertInterventionSql,
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
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 4b. Rina Kusuma: extension_work Advanced Math / Olympiad
        await connection.ExecuteAsync(new CommandDefinition(
            _insertInterventionSql,
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

    private static async Task CreateAttendanceRecordsAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        List<Guid> activityIds,
        List<Guid> studentIds,
        Guid jonathanId,
        DateTime periodStart,
        DateTimeOffset now,
        string createdBy,
        Random random,
        int targetCount,
        CancellationToken cancellationToken)
    {
        int created = 0;
        foreach (var activityId in activityIds)
        {
            if (created >= targetCount) break;

            var activityDate = periodStart.AddDays(random.Next(0, 100));
            var selectedStudents = studentIds
                .OrderBy(_ => random.Next())
                .Take(Math.Min(random.Next(10, 30), studentIds.Count - created))
                .ToList();

            foreach (var studentId in selectedStudents)
            {
                if (created >= targetCount) break;

                var status = studentId == jonathanId
                    ? "present"
                    : (random.Next(100) switch
                    {
                        < 95 => "present",
                        < 98 => "excused_absence",
                        < 99 => "late",
                        _ => "absent"
                    });

                await connection.ExecuteAsync(new CommandDefinition(
                    _insertAttendanceSql,
                    new
                    {
                        Id = Guid.NewGuid(),
                        LearningActivityId = activityId,
                        StudentId = studentId,
                        Status = status,
                        RecordedBy = "teacher-01",
                        RecordedDate = activityDate,
                        CreatedBy = createdBy
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
                created++;
            }
        }
    }
}
