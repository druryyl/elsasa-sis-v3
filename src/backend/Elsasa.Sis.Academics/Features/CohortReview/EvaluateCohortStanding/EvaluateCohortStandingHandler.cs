namespace Elsasa.Sis.Academics.Features.CohortReview.EvaluateCohortStanding;

using Dapper;
using Elsasa.Sis.Academics.Features.CohortReview;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using FluentValidation;

/// <summary>
/// Feature handler dynamically evaluating proposed cohort academic standings for an academic period and grade level (TD-03, UC-ACD-17, P3-S06).
/// Executes on-demand multi-set aggregation over enrolled students, approved operative results, learning unit progress, and attendance.
/// </summary>
public sealed class EvaluateCohortStandingHandler(
    ISqlConnectionFactory connectionFactory,
    ThresholdRuleEvaluator thresholdEvaluator,
    IValidator<EvaluateCohortStandingRequest>? validator = null)
{
    static EvaluateCohortStandingHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _evaluateCohortStandingSql = """
        DECLARE @TrimmedGrade NVARCHAR(50) = RTRIM(LTRIM(@GradeLevel));

        -- 1. Candidate Students in this Grade / Period
        WITH CandidateIds AS (
            SELECT DISTINCT p.student_id
            FROM students.administrative_placements p
            WHERE p.is_current = 1
              AND (p.grade = @TrimmedGrade OR p.cohort = @TrimmedGrade OR p.school_level = @TrimmedGrade)

            UNION

            SELECT DISTINCT ap.student_id
            FROM academics.academic_participations ap
            LEFT JOIN academics.learning_units lu ON lu.id = ap.learning_unit_id
            WHERE ap.academic_period_id = @AcademicPeriodId
              AND (ap.section_label = @TrimmedGrade OR lu.level = @TrimmedGrade)

            UNION

            SELECT DISTINCT ast.student_id
            FROM academics.academic_standings ast
            WHERE ast.academic_period_id = @AcademicPeriodId
        )
        SELECT
            c.student_id AS StudentId,
            COALESCE(s.student_number, '') AS StudentNumber,
            COALESCE(s.full_name, 'Student ' + CAST(c.student_id AS NVARCHAR(36))) AS StudentName,
            COALESCE(p.grade, @TrimmedGrade) AS GradeLevel
        FROM CandidateIds c
        LEFT JOIN students.students s ON s.id = c.student_id
        LEFT JOIN students.administrative_placements p ON p.student_id = c.student_id AND p.is_current = 1
        ORDER BY StudentName;

        -- 2. Units (Participations and Progress) for candidate students in this period
        WITH EnrolledUnits AS (
            SELECT
                ap.student_id AS StudentId,
                ap.learning_unit_id AS LearningUnitId,
                ap.status AS ParticipationStatus,
                NULL AS ProgressStatus
            FROM academics.academic_participations ap
            WHERE ap.academic_period_id = @AcademicPeriodId

            UNION

            SELECT
                lp.student_id AS StudentId,
                lp.learning_unit_id AS LearningUnitId,
                NULL AS ParticipationStatus,
                lp.status AS ProgressStatus
            FROM academics.learning_progress lp
            WHERE lp.academic_period_id = @AcademicPeriodId
        )
        SELECT
            eu.StudentId,
            eu.LearningUnitId,
            lu.code AS UnitCode,
            lu.name AS UnitName,
            MAX(eu.ParticipationStatus) AS ParticipationStatus,
            MAX(eu.ProgressStatus) AS ProgressStatus
        FROM EnrolledUnits eu
        INNER JOIN academics.learning_units lu ON lu.id = eu.LearningUnitId
        GROUP BY eu.StudentId, eu.LearningUnitId, lu.code, lu.name;

        -- 3. Operative approved results for candidate students in this period
        SELECT
            ar.student_id AS StudentId,
            a.learning_unit_id AS LearningUnitId,
            lu.code AS UnitCode,
            lu.name AS UnitName,
            ar.score_numeric AS ScoreNumeric,
            a.passing_score AS PassingScore,
            a.max_score AS MaxScore,
            ar.result_status AS ResultStatus
        FROM academics.academic_results ar
        INNER JOIN academics.assessments a ON a.id = ar.assessment_id
        INNER JOIN academics.learning_units lu ON lu.id = a.learning_unit_id
        WHERE ar.is_final = 1
          AND ar.result_status IN ('approved', 'published')
          AND (
              EXISTS (
                  SELECT 1 FROM academics.academic_participations ap
                  WHERE ap.student_id = ar.student_id
                    AND ap.learning_unit_id = a.learning_unit_id
                    AND ap.academic_period_id = @AcademicPeriodId
              )
              OR EXISTS (
                  SELECT 1 FROM academics.learning_structures ls
                  WHERE ls.id = lu.learning_structure_id
                    AND ls.academic_period_id = @AcademicPeriodId
              )
          );

        -- 4. Attendance summary for candidate students in this period
        SELECT
            r.student_id AS StudentId,
            COUNT(1) AS TotalSessions,
            ISNULL(SUM(CASE WHEN r.status IN ('present', 'late') THEN 1 ELSE 0 END), 0) AS TotalPresent
        FROM academics.attendance_records r
        INNER JOIN academics.learning_activities a ON a.id = r.learning_activity_id
        INNER JOIN academics.learning_units u ON u.id = a.learning_unit_id
        INNER JOIN academics.learning_structures ls ON ls.id = u.learning_structure_id
        WHERE (ls.academic_period_id = @AcademicPeriodId OR EXISTS (
            SELECT 1 FROM academics.academic_participations p
            WHERE p.student_id = r.student_id
              AND p.learning_unit_id = a.learning_unit_id
              AND p.academic_period_id = @AcademicPeriodId
        ))
        GROUP BY r.student_id;

        -- 5. Existing saved standings for this period
        SELECT
            student_id AS StudentId,
            standing AS Standing,
            determined_at AS DeterminedAt,
            is_exception AS IsException,
            exception_reason AS ExceptionReason,
            committee_reference AS CommitteeReference,
            authorized_by AS AuthorizedBy,
            conditions AS Conditions
        FROM academics.academic_standings
        WHERE academic_period_id = @AcademicPeriodId;
        """;

    private sealed record CandidateStudentRow(
        Guid StudentId,
        string StudentNumber,
        string StudentName,
        string GradeLevel);

    private sealed record StudentUnitRow(
        Guid StudentId,
        Guid LearningUnitId,
        string UnitCode,
        string UnitName,
        string? ParticipationStatus,
        string? ProgressStatus);

    private sealed record ResultAssessmentRow(
        Guid StudentId,
        Guid LearningUnitId,
        string UnitCode,
        string UnitName,
        decimal? ScoreNumeric,
        decimal? PassingScore,
        decimal? MaxScore,
        string ResultStatus);

    private sealed record AttendanceSummaryRow(
        Guid StudentId,
        int TotalSessions,
        int TotalPresent);

    private sealed record ExistingStandingRow(
        Guid StudentId,
        string Standing,
        DateTimeOffset DeterminedAt,
        bool IsException,
        string? ExceptionReason,
        string? CommitteeReference,
        string AuthorizedBy,
        string? Conditions);

    public async Task<Result<IReadOnlyList<CohortStandingProposalDto>>> HandleAsync(
        EvaluateCohortStandingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (validator != null)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<IReadOnlyList<CohortStandingProposalDto>>(
                    DomainError.Validation("CohortStanding.Validation", validationResult.ToString()));
            }
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            _evaluateCohortStandingSql,
            new
            {
                AcademicPeriodId = request.AcademicPeriodId,
                GradeLevel = request.GradeLevel
            },
            cancellationToken: cancellationToken));

        var candidates = (await multi.ReadAsync<CandidateStudentRow>()).ToList();
        var units = (await multi.ReadAsync<StudentUnitRow>()).ToList();
        var results = (await multi.ReadAsync<ResultAssessmentRow>()).ToList();
        var attendanceRows = (await multi.ReadAsync<AttendanceSummaryRow>()).ToList();
        var existingStandings = (await multi.ReadAsync<ExistingStandingRow>()).ToList();

        var unitsByStudent = units.GroupBy(u => u.StudentId).ToDictionary(g => g.Key, g => g.ToList());
        var resultsByStudent = results.GroupBy(r => r.StudentId).ToDictionary(g => g.Key, g => g.ToList());
        var attendanceByStudent = attendanceRows.ToDictionary(a => a.StudentId);
        var existingStandingByStudent = existingStandings.ToDictionary(s => s.StudentId);

        var proposals = new List<CohortStandingProposalDto>(candidates.Count);

        foreach (var student in candidates)
        {
            unitsByStudent.TryGetValue(student.StudentId, out var studentUnits);
            resultsByStudent.TryGetValue(student.StudentId, out var studentResults);
            attendanceByStudent.TryGetValue(student.StudentId, out var attendance);
            existingStandingByStudent.TryGetValue(student.StudentId, out var savedStanding);

            // Collect all unique units for the student
            var unitMap = new Dictionary<Guid, (string Code, string Name)>();
            if (studentUnits != null)
            {
                foreach (var u in studentUnits)
                {
                    unitMap[u.LearningUnitId] = (u.UnitCode, u.UnitName);
                }
            }

            if (studentResults != null)
            {
                foreach (var r in studentResults)
                {
                    unitMap[r.LearningUnitId] = (r.UnitCode, r.UnitName);
                }
            }

            var totalUnits = unitMap.Count;
            var completedCount = 0;
            var failedUnitNames = new List<string>();

            foreach (var (unitId, unitInfo) in unitMap)
            {
                var unitProgress = studentUnits?.FirstOrDefault(u => u.LearningUnitId == unitId);
                var unitResults = studentResults?.Where(r => r.LearningUnitId == unitId).ToList();

                var isNeedsRemediation = string.Equals(unitProgress?.ProgressStatus, "needs_remediation", StringComparison.OrdinalIgnoreCase);
                var isCompleted = string.Equals(unitProgress?.ProgressStatus, "completed", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(unitProgress?.ProgressStatus, "exempted", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(unitProgress?.ParticipationStatus, "completed", StringComparison.OrdinalIgnoreCase);

                var hasPassingResult = unitResults?.Any(r => r.ScoreNumeric.HasValue && r.PassingScore.HasValue && r.ScoreNumeric.Value >= r.PassingScore.Value) == true;
                var hasFailingResult = unitResults?.Any(r => r.ScoreNumeric.HasValue && r.PassingScore.HasValue && r.ScoreNumeric.Value < r.PassingScore.Value) == true;

                if (isNeedsRemediation || (hasFailingResult && !hasPassingResult && !isCompleted))
                {
                    failedUnitNames.Add(unitInfo.Name);
                }
                else if (isCompleted || hasPassingResult)
                {
                    completedCount++;
                }
            }

            var attendanceRate = 100.00m;
            if (attendance != null && attendance.TotalSessions > 0)
            {
                attendanceRate = Math.Round((decimal)attendance.TotalPresent * 100m / attendance.TotalSessions, 2);
            }

            var (proposedStanding, conditions) = thresholdEvaluator.EvaluateStanding(
                totalUnits,
                completedCount,
                failedUnitNames,
                attendanceRate);

            proposals.Add(new CohortStandingProposalDto(
                StudentId: student.StudentId,
                StudentNumber: string.IsNullOrWhiteSpace(student.StudentNumber) ? null : student.StudentNumber,
                StudentName: student.StudentName,
                GradeLevel: student.GradeLevel,
                TotalUnits: totalUnits,
                CompletedUnits: completedCount,
                FailedUnits: failedUnitNames.Count,
                AttendanceRate: attendanceRate,
                ProposedStanding: proposedStanding,
                PolicyReference: ThresholdRuleEvaluator.DefaultPolicyReference,
                Conditions: conditions,
                ExistingStanding: savedStanding?.Standing,
                ExistingDeterminedAt: savedStanding?.DeterminedAt,
                IsException: savedStanding?.IsException ?? false,
                ExceptionReason: savedStanding?.ExceptionReason,
                CommitteeReference: savedStanding?.CommitteeReference,
                FailingUnitNames: failedUnitNames.Count > 0 ? failedUnitNames : null));
        }

        return Result.Success<IReadOnlyList<CohortStandingProposalDto>>(proposals);
    }
}
