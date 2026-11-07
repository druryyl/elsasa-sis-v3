namespace Elsasa.Sis.Academics.Features.AcademicInterventions.GetAtRiskLearners;

using System.Data;
using Dapper;
using Elsasa.Sis.Academics.Features.AcademicInterventions;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing the On-Demand At-Risk Query Engine (UC-ACD-11, P4-S10, TD-04).
/// Dynamically detects struggling students based on operative assessment scores below threshold,
/// overdue missing coursework, attendance presence rates below 75%, or non-good standing determinations.
/// </summary>
public sealed class GetAtRiskLearnersHandler(ISqlConnectionFactory connectionFactory)
{
    static GetAtRiskLearnersHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAtRiskLearnersSql = """
        WITH DistinctParticipations AS (
            SELECT
                ap.student_id,
                ap.learning_unit_id,
                ap.academic_period_id,
                ap.section_label,
                ROW_NUMBER() OVER (
                    PARTITION BY ap.student_id, ap.learning_unit_id, ap.academic_period_id
                    ORDER BY ap.attempt_number DESC, ap.created_at DESC
                ) AS rn
            FROM academics.academic_participations ap
            WHERE ap.academic_period_id = @AcademicPeriodId
        ),
        AssessmentRisk AS (
            SELECT
                ap.student_id,
                ap.learning_unit_id,
                MIN(ar.score_numeric) AS MinScore,
                MAX(asm.passing_score) AS PassingScore,
                MAX(asm.title) AS AssessmentTitle,
                COUNT(1) AS FailingAssessmentCount
            FROM academics.academic_participations ap
            INNER JOIN academics.assessments asm ON asm.learning_unit_id = ap.learning_unit_id
            INNER JOIN academics.academic_results ar ON ar.assessment_id = asm.id AND ar.student_id = ap.student_id
            WHERE ap.academic_period_id = @AcademicPeriodId
              AND ar.is_final = 1
              AND asm.passing_score IS NOT NULL
              AND ar.score_numeric IS NOT NULL
              AND ar.score_numeric < asm.passing_score
            GROUP BY ap.student_id, ap.learning_unit_id
        ),
        RemediationProgressRisk AS (
            SELECT
                lp.student_id,
                lp.learning_unit_id,
                1 AS NeedsRemediationFlag
            FROM academics.learning_progress lp
            WHERE lp.academic_period_id = @AcademicPeriodId
              AND lp.status = 'needs_remediation'
        ),
        MissingWorkRisk AS (
            SELECT
                ap.student_id,
                ap.learning_unit_id,
                COUNT(1) AS MissingAssignmentsCount
            FROM academics.academic_participations ap
            INNER JOIN academics.learning_activities act ON act.learning_unit_id = ap.learning_unit_id
            INNER JOIN academics.assignments a ON a.learning_activity_id = act.id
            WHERE ap.academic_period_id = @AcademicPeriodId
              AND a.status = 'released'
              AND a.due_date IS NOT NULL
              AND a.due_date < CAST(SYSUTCDATETIME() AS DATE)
              AND NOT EXISTS (
                  SELECT 1
                  FROM academics.submissions sub
                  WHERE sub.assignment_id = a.id
                    AND sub.student_id = ap.student_id
              )
            GROUP BY ap.student_id, ap.learning_unit_id
        ),
        AttendanceRisk AS (
            SELECT
                ap.student_id,
                ap.learning_unit_id,
                COUNT(att.id) AS TotalSessions,
                SUM(CASE WHEN att.status IN ('present', 'late') THEN 1 ELSE 0 END) AS PresentSessions,
                CASE
                    WHEN COUNT(att.id) > 0
                    THEN ROUND((CAST(SUM(CASE WHEN att.status IN ('present', 'late') THEN 1 ELSE 0 END) AS DECIMAL(5,2)) * 100.0) / COUNT(att.id), 2)
                    ELSE 100.00
                END AS AttendanceRate
            FROM academics.academic_participations ap
            INNER JOIN academics.learning_activities act ON act.learning_unit_id = ap.learning_unit_id
            INNER JOIN academics.attendance_records att ON att.learning_activity_id = act.id AND att.student_id = ap.student_id
            WHERE ap.academic_period_id = @AcademicPeriodId
            GROUP BY ap.student_id, ap.learning_unit_id
        ),
        StandingRisk AS (
            SELECT
                ast.student_id,
                ast.standing AS Standing,
                ast.conditions AS StandingConditions
            FROM academics.academic_standings ast
            WHERE ast.academic_period_id = @AcademicPeriodId
              AND ast.standing IN ('conditional_standing', 'not_in_good_standing')
        ),
        ActiveInterventions AS (
            SELECT
                ai.student_id,
                ai.learning_unit_id,
                COUNT(1) AS ActiveInterventionsCount
            FROM academics.academic_interventions ai
            WHERE ai.academic_period_id = @AcademicPeriodId
              AND ai.status IN ('planned', 'in_progress')
            GROUP BY ai.student_id, ai.learning_unit_id
        )
        SELECT
            dp.student_id AS StudentId,
            s.student_number AS StudentNumber,
            s.full_name AS StudentName,
            COALESCE(lu.level, dp.section_label) AS GradeLevel,
            dp.learning_unit_id AS LearningUnitId,
            lu.code AS LearningUnitCode,
            lu.name AS LearningUnitName,
            dp.academic_period_id AS AcademicPeriodId,
            dp.section_label AS SectionLabel,
            CASE WHEN (ar.FailingAssessmentCount > 0 OR rp.NeedsRemediationFlag = 1) THEN 1 ELSE 0 END AS HasAssessmentBelowThreshold,
            CASE WHEN (mw.MissingAssignmentsCount > 0) THEN 1 ELSE 0 END AS HasMissingWork,
            CASE WHEN (att.AttendanceRate < 75.0 AND att.TotalSessions > 0) THEN 1 ELSE 0 END AS HasAttendanceConcern,
            CASE WHEN (st.Standing IS NOT NULL) THEN 1 ELSE 0 END AS HasStandingConcern,
            ar.MinScore AS OperativeScore,
            ar.PassingScore AS PassingScore,
            ar.AssessmentTitle AS FailingAssessmentTitle,
            COALESCE(mw.MissingAssignmentsCount, 0) AS MissingAssignmentsCount,
            att.AttendanceRate AS AttendanceRate,
            st.Standing AS CurrentStanding,
            COALESCE(ai.ActiveInterventionsCount, 0) AS ActiveInterventionsCount
        FROM DistinctParticipations dp
        INNER JOIN academics.learning_units lu ON lu.id = dp.learning_unit_id
        LEFT JOIN students.students s ON s.id = dp.student_id
        LEFT JOIN AssessmentRisk ar ON ar.student_id = dp.student_id AND ar.learning_unit_id = dp.learning_unit_id
        LEFT JOIN RemediationProgressRisk rp ON rp.student_id = dp.student_id AND rp.learning_unit_id = dp.learning_unit_id
        LEFT JOIN MissingWorkRisk mw ON mw.student_id = dp.student_id AND mw.learning_unit_id = dp.learning_unit_id
        LEFT JOIN AttendanceRisk att ON att.student_id = dp.student_id AND att.learning_unit_id = dp.learning_unit_id
        LEFT JOIN StandingRisk st ON st.student_id = dp.student_id
        LEFT JOIN ActiveInterventions ai ON ai.student_id = dp.student_id AND ai.learning_unit_id = dp.learning_unit_id
        WHERE dp.rn = 1
          AND (@GradeLevel IS NULL OR lu.level = @GradeLevel OR dp.section_label LIKE @GradeLevel + '%')
          AND (
              (ar.FailingAssessmentCount > 0 OR rp.NeedsRemediationFlag = 1)
              OR (mw.MissingAssignmentsCount > 0)
              OR (att.AttendanceRate < 75.0 AND att.TotalSessions > 0)
              OR (st.Standing IS NOT NULL)
          )
        ORDER BY s.full_name ASC, lu.code ASC;
        """;

    private sealed record AtRiskQueryRow(
        Guid StudentId,
        string? StudentNumber,
        string? StudentName,
        string? GradeLevel,
        Guid LearningUnitId,
        string? LearningUnitCode,
        string? LearningUnitName,
        Guid AcademicPeriodId,
        string? SectionLabel,
        int HasAssessmentBelowThreshold,
        int HasMissingWork,
        int HasAttendanceConcern,
        int HasStandingConcern,
        decimal? OperativeScore,
        decimal? PassingScore,
        string? FailingAssessmentTitle,
        int MissingAssignmentsCount,
        decimal? AttendanceRate,
        string? CurrentStanding,
        int ActiveInterventionsCount);

    /// <summary>
    /// Executes the live on-demand query detecting at-risk learners.
    /// </summary>
    /// <param name="query">The query parameters including academicPeriodId and optional gradeLevel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of at-risk learner records with trigger indicators and pre-populated action context.</returns>
    public async Task<Result<IReadOnlyList<AtRiskLearnerDto>>> HandleAsync(
        GetAtRiskLearnersQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.AcademicPeriodId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<AtRiskLearnerDto>>(
                DomainError.Validation("Academic.PeriodIdRequired", "academicPeriodId query parameter is required."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<AtRiskQueryRow>(new CommandDefinition(
            _selectAtRiskLearnersSql,
            new
            {
                query.AcademicPeriodId,
                GradeLevel = string.IsNullOrWhiteSpace(query.GradeLevel) ? null : query.GradeLevel.Trim()
            },
            cancellationToken: cancellationToken));

        var results = new List<AtRiskLearnerDto>();

        foreach (var row in rows)
        {
            var detectedTriggers = new List<string>(4);
            string? primaryTrigger = null;
            string? primaryDescription = null;

            var unitDisplay = !string.IsNullOrWhiteSpace(row.LearningUnitName)
                ? row.LearningUnitName
                : row.LearningUnitCode ?? "Learning Unit";

            if (row.HasAssessmentBelowThreshold == 1)
            {
                detectedTriggers.Add("assessment_below_threshold");
                primaryTrigger ??= "assessment_below_threshold";
                if (row.OperativeScore.HasValue && row.PassingScore.HasValue)
                {
                    var testTitle = !string.IsNullOrWhiteSpace(row.FailingAssessmentTitle)
                        ? $" on '{row.FailingAssessmentTitle}'"
                        : string.Empty;
                    primaryDescription ??= $"Score {row.OperativeScore.Value:0.##}{testTitle} below passing score {row.PassingScore.Value:0.##} in {unitDisplay}";
                }
                else
                {
                    primaryDescription ??= $"Assessment score below passing threshold in {unitDisplay}";
                }
            }

            if (row.HasMissingWork == 1)
            {
                detectedTriggers.Add("missing_work");
                primaryTrigger ??= "missing_work";
                primaryDescription ??= $"{row.MissingAssignmentsCount} overdue assignment(s) missing submission in {unitDisplay}";
            }

            if (row.HasAttendanceConcern == 1)
            {
                detectedTriggers.Add("attendance_concern");
                primaryTrigger ??= "attendance_concern";
                primaryDescription ??= row.AttendanceRate.HasValue
                    ? $"Attendance presence rate {row.AttendanceRate.Value:0.##}% is below 75% threshold in {unitDisplay}"
                    : $"Attendance presence rate is below 75% threshold in {unitDisplay}";
            }

            if (row.HasStandingConcern == 1)
            {
                detectedTriggers.Add("standing_concern");
                primaryTrigger ??= "standing_concern";
                primaryDescription ??= $"Academic standing is '{row.CurrentStanding}' in active period";
            }

            primaryTrigger ??= "teacher_referral";
            primaryDescription ??= $"Academic support recommended for {unitDisplay}";

            results.Add(new AtRiskLearnerDto(
                row.StudentId,
                row.StudentNumber,
                row.StudentName,
                row.GradeLevel,
                row.LearningUnitId,
                row.LearningUnitCode,
                row.LearningUnitName,
                row.AcademicPeriodId,
                row.SectionLabel,
                primaryTrigger,
                primaryDescription,
                detectedTriggers,
                row.HasAssessmentBelowThreshold == 1,
                row.HasMissingWork == 1,
                row.HasAttendanceConcern == 1,
                row.HasStandingConcern == 1,
                row.OperativeScore,
                row.PassingScore,
                row.MissingAssignmentsCount,
                row.AttendanceRate,
                row.CurrentStanding,
                row.ActiveInterventionsCount));
        }

        return Result.Success<IReadOnlyList<AtRiskLearnerDto>>(results);
    }
}
