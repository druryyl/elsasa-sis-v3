namespace Elsasa.Sis.Academics.Services;

using System.Text.Json;
using Dapper;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Authoritative implementation of the synchronous Academics cross-domain query contract (ADR-001, ADR-003, TD-01, TD-09).
/// Provides parameterized read operations for active curriculum structures, learning units, and academic requirements.
/// </summary>
public sealed class AcademicsModule(ISqlConnectionFactory connectionFactory) : IAcademicsModule
{
    static AcademicsModule()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectActiveLearningStructureSql = """
        SELECT
            id AS Id,
            name AS Name,
            academic_period_id AS AcademicPeriodId,
            academic_period_label AS AcademicPeriodLabel,
            version AS Version,
            effective_date AS EffectiveDate,
            status AS Status,
            approved_by AS ApprovedBy,
            approved_date AS ApprovedDate
        FROM academics.learning_structures
        WHERE academic_period_id = @AcademicPeriodId
          AND status = 'active';
        """;

    private const string _selectLearningUnitsByStructureSql = """
        SELECT
            id AS Id,
            learning_structure_id AS LearningStructureId,
            parent_unit_id AS ParentUnitId,
            code AS Code,
            name AS Name,
            unit_type AS UnitType,
            level AS Level,
            credit_weight AS CreditWeight,
            status AS Status
        FROM academics.learning_units
        WHERE learning_structure_id = @LearningStructureId
        ORDER BY code ASC;
        """;

    private const string _selectLearningUnitByIdSql = """
        SELECT
            id AS Id,
            learning_structure_id AS LearningStructureId,
            parent_unit_id AS ParentUnitId,
            code AS Code,
            name AS Name,
            unit_type AS UnitType,
            level AS Level,
            credit_weight AS CreditWeight,
            status AS Status
        FROM academics.learning_units
        WHERE id = @LearningUnitId;
        """;

    private const string _selectAcademicRequirementsByStructureSql = """
        SELECT
            id AS Id,
            name AS Name,
            requirement_scope AS RequirementScope,
            learning_structure_id AS LearningStructureId,
            learning_unit_id AS LearningUnitId,
            level AS Level,
            requirement_type AS RequirementType,
            description AS Description,
            version AS Version,
            effective_date AS EffectiveDate,
            end_date AS EndDate,
            status AS Status
        FROM academics.academic_requirements
        WHERE learning_structure_id = @LearningStructureId
        ORDER BY requirement_scope ASC, name ASC;
        """;

    /// <inheritdoc />
    public async Task<Result<ActiveLearningStructureDto?>> GetActiveLearningStructureForPeriodAsync(
        Guid academicPeriodId,
        CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var activeStructure = await connection.QuerySingleOrDefaultAsync<ActiveLearningStructureDto>(new CommandDefinition(
                _selectActiveLearningStructureSql,
                new { AcademicPeriodId = academicPeriodId },
                cancellationToken: ct));

            return Result.Success(activeStructure);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<ActiveLearningStructureDto?>(
                DomainError.Failure(
                    "Academics.ActiveLearningStructureQueryFailed",
                    $"Failed to query active learning structure for academic period '{academicPeriodId}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LearningUnitDto>>> GetLearningUnitsByStructureAsync(
        Guid learningStructureId,
        CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var units = (await connection.QueryAsync<LearningUnitDto>(new CommandDefinition(
                _selectLearningUnitsByStructureSql,
                new { LearningStructureId = learningStructureId },
                cancellationToken: ct))).AsList();

            return Result.Success<IReadOnlyList<LearningUnitDto>>(units);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<LearningUnitDto>>(
                DomainError.Failure(
                    "Academics.LearningUnitsQueryFailed",
                    $"Failed to query learning units for structure '{learningStructureId}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<LearningUnitDto?>> GetLearningUnitByIdAsync(
        Guid learningUnitId,
        CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var unit = await connection.QuerySingleOrDefaultAsync<LearningUnitDto>(new CommandDefinition(
                _selectLearningUnitByIdSql,
                new { LearningUnitId = learningUnitId },
                cancellationToken: ct));

            return Result.Success(unit);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<LearningUnitDto?>(
                DomainError.Failure(
                    "Academics.LearningUnitQueryFailed",
                    $"Failed to query learning unit '{learningUnitId}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AcademicRequirementDto>>> GetAcademicRequirementsByStructureAsync(
        Guid learningStructureId,
        CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var requirements = (await connection.QueryAsync<AcademicRequirementDto>(new CommandDefinition(
                _selectAcademicRequirementsByStructureSql,
                new { LearningStructureId = learningStructureId },
                cancellationToken: ct))).AsList();

            return Result.Success<IReadOnlyList<AcademicRequirementDto>>(requirements);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<AcademicRequirementDto>>(
                DomainError.Failure(
                    "Academics.AcademicRequirementsQueryFailed",
                    $"Failed to query academic requirements for structure '{learningStructureId}': {ex.Message}"));
        }
    }

    private const string _checkStudentParticipatingSql = """
        SELECT COUNT(1)
        FROM academics.academic_participations
        WHERE student_id = @StudentId
          AND learning_unit_id = @LearningUnitId
          AND academic_period_id = @AcademicPeriodId
          AND status = 'in_progress';
        """;

    private const string _selectStudentCompletedUnitsSql = """
        SELECT
            p.id AS ParticipationId,
            p.student_id AS StudentId,
            p.learning_unit_id AS LearningUnitId,
            u.code AS LearningUnitCode,
            u.name AS LearningUnitName,
            u.credit_weight AS CreditWeight,
            COALESCE(p.completion_date, p.end_date, p.start_date) AS CompletionDate,
            p.participation_type AS ParticipationType
        FROM academics.academic_participations p
        INNER JOIN academics.learning_units u ON u.id = p.learning_unit_id
        WHERE p.student_id = @StudentId
          AND p.status = 'completed'
        ORDER BY p.completion_date DESC, u.code ASC;
        """;

    /// <inheritdoc />
    public async Task<Result<bool>> IsStudentParticipatingAsync(
        Guid studentId,
        Guid learningUnitId,
        Guid academicPeriodId,
        CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                _checkStudentParticipatingSql,
                new
                {
                    StudentId = studentId,
                    LearningUnitId = learningUnitId,
                    AcademicPeriodId = academicPeriodId
                },
                cancellationToken: ct));

            return Result.Success(count > 0);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<bool>(
                DomainError.Failure(
                    "Academics.ParticipationQueryFailed",
                    $"Failed to check participation for student '{studentId}' in unit '{learningUnitId}': {ex.Message}"));
        }
    }

    private const string _selectStudentAttendanceSummaryForPeriodSql = """
        SELECT
            COUNT(1) AS TotalSessions,
            ISNULL(SUM(CASE WHEN r.status = 'present' THEN 1 ELSE 0 END), 0) AS TotalPresent,
            ISNULL(SUM(CASE WHEN r.status = 'absent' THEN 1 ELSE 0 END), 0) AS TotalAbsent,
            ISNULL(SUM(CASE WHEN r.status = 'late' THEN 1 ELSE 0 END), 0) AS TotalLate,
            ISNULL(SUM(CASE WHEN r.status = 'excused_absence' THEN 1 ELSE 0 END), 0) AS TotalExcused
        FROM academics.attendance_records r
        INNER JOIN academics.learning_activities a ON a.id = r.learning_activity_id
        INNER JOIN academics.learning_units u ON u.id = a.learning_unit_id
        INNER JOIN academics.learning_structures ls ON ls.id = u.learning_structure_id
        WHERE r.student_id = @StudentId
          AND (ls.academic_period_id = @AcademicPeriodId OR EXISTS (
              SELECT 1 FROM academics.academic_participations p
              WHERE p.student_id = r.student_id
                AND p.learning_unit_id = a.learning_unit_id
                AND p.academic_period_id = @AcademicPeriodId
          ));
        """;

    private sealed record AttendanceSummaryRow(
        int TotalSessions,
        int TotalPresent,
        int TotalAbsent,
        int TotalLate,
        int TotalExcused);

    /// <inheritdoc />
    public async Task<Result<StudentAttendanceSummaryDto>> GetStudentAttendanceSummaryAsync(
        Guid studentId,
        Guid academicPeriodId,
        CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var row = await connection.QuerySingleOrDefaultAsync<AttendanceSummaryRow>(new CommandDefinition(
                _selectStudentAttendanceSummaryForPeriodSql,
                new
                {
                    StudentId = studentId,
                    AcademicPeriodId = academicPeriodId
                },
                cancellationToken: ct));

            var totalSessions = row?.TotalSessions ?? 0;
            var totalPresent = row?.TotalPresent ?? 0;
            var totalAbsent = row?.TotalAbsent ?? 0;
            var totalLate = row?.TotalLate ?? 0;
            var totalExcused = row?.TotalExcused ?? 0;

            var percentage = totalSessions > 0
                ? Math.Round((decimal)(totalPresent + totalLate) * 100m / totalSessions, 2)
                : 0.00m;

            var summary = new StudentAttendanceSummaryDto(
                studentId,
                academicPeriodId,
                totalSessions,
                totalPresent,
                totalAbsent,
                totalLate,
                totalExcused,
                percentage);

            return Result.Success(summary);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<StudentAttendanceSummaryDto>(
                DomainError.Failure(
                    "Academics.AttendanceSummaryQueryFailed",
                    $"Failed to query attendance summary for student '{studentId}' in period '{academicPeriodId}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CompletedParticipationDto>>> GetStudentCompletedUnitsAsync(
        Guid studentId,
        CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var completedUnits = (await connection.QueryAsync<CompletedParticipationDto>(new CommandDefinition(
                _selectStudentCompletedUnitsSql,
                new { StudentId = studentId },
                cancellationToken: ct))).AsList();

            return Result.Success<IReadOnlyList<CompletedParticipationDto>>(completedUnits);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<CompletedParticipationDto>>(
                DomainError.Failure(
                    "Academics.CompletedUnitsQueryFailed",
                    $"Failed to query completed units for student '{studentId}': {ex.Message}"));
        }
    }

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private const string _selectStudentApprovedResultsSql = """
        SELECT
            r.id AS ResultId,
            r.assessment_id AS AssessmentId,
            a.title AS AssessmentTitle,
            u.id AS LearningUnitId,
            u.name AS LearningUnitTitle,
            r.score_numeric AS ScoreNumeric,
            COALESCE(r.score_max, a.max_score) AS ScoreMax,
            r.grade_letter AS GradeLetter,
            r.grade_label AS GradeLabel,
            r.narrative_feedback AS NarrativeFeedback,
            CAST(r.evaluated_date AS DATETIME2) AS EvaluatedDate
        FROM academics.academic_results r
        INNER JOIN academics.assessments a ON a.id = r.assessment_id
        INNER JOIN academics.learning_units u ON u.id = a.learning_unit_id
        INNER JOIN academics.learning_structures ls ON ls.id = u.learning_structure_id
        WHERE r.student_id = @StudentId
          AND r.is_final = 1
          AND r.result_status IN ('approved', 'published')
          AND (ls.academic_period_id = @AcademicPeriodId OR EXISTS (
              SELECT 1 FROM academics.academic_participations p
              WHERE p.student_id = r.student_id
                AND p.learning_unit_id = a.learning_unit_id
                AND p.academic_period_id = @AcademicPeriodId
          ))
        ORDER BY u.name ASC, a.title ASC, r.evaluated_date DESC;
        """;

    private const string _selectApplicantAssessmentScoreSql = """
        SELECT
            r.id AS ResultId,
            r.assessment_id AS AssessmentId,
            a.title AS AssessmentTitle,
            r.applicant_id AS ApplicantId,
            r.score_numeric AS ScoreNumeric,
            COALESCE(r.score_max, a.max_score) AS ScoreMax,
            r.grade_letter AS GradeLetter,
            r.grade_label AS GradeLabel,
            r.evaluation_model AS EvaluationModel,
            r.result_status AS ResultStatus,
            r.evaluated_by AS EvaluatedBy,
            CAST(r.evaluated_date AS DATETIME2) AS EvaluatedDate
        FROM academics.academic_results r
        INNER JOIN academics.assessments a ON a.id = r.assessment_id
        WHERE r.applicant_id = @ApplicantId
          AND r.assessment_id = @AssessmentId
          AND r.is_final = 1;
        """;

    private const string _selectReportCardSnapshotSql = """
        SELECT content_snapshot
        FROM academics.report_cards
        WHERE id = @ReportCardId;
        """;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<StudentApprovedResultDto>>> GetStudentApprovedResultsAsync(
        Guid studentId,
        Guid academicPeriodId,
        CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var results = (await connection.QueryAsync<StudentApprovedResultDto>(new CommandDefinition(
                _selectStudentApprovedResultsSql,
                new
                {
                    StudentId = studentId,
                    AcademicPeriodId = academicPeriodId
                },
                cancellationToken: ct))).AsList();

            return Result.Success<IReadOnlyList<StudentApprovedResultDto>>(results);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<StudentApprovedResultDto>>(
                DomainError.Failure(
                    "Academics.StudentApprovedResultsQueryFailed",
                    $"Failed to query approved results for student '{studentId}' in period '{academicPeriodId}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<ApplicantAssessmentScoreDto?>> GetApplicantAssessmentScoreAsync(
        Guid applicantId,
        Guid assessmentId,
        CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var score = await connection.QuerySingleOrDefaultAsync<ApplicantAssessmentScoreDto>(new CommandDefinition(
                _selectApplicantAssessmentScoreSql,
                new
                {
                    ApplicantId = applicantId,
                    AssessmentId = assessmentId
                },
                cancellationToken: ct));

            return Result.Success(score);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<ApplicantAssessmentScoreDto?>(
                DomainError.Failure(
                    "Academics.ApplicantAssessmentScoreQueryFailed",
                    $"Failed to query assessment score for applicant '{applicantId}' in assessment '{assessmentId}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<ReportCardSnapshotDto?>> GetStudentReportCardSnapshotAsync(
        Guid reportCardId,
        CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var jsonSnapshot = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
                _selectReportCardSnapshotSql,
                new { ReportCardId = reportCardId },
                cancellationToken: ct));

            if (string.IsNullOrWhiteSpace(jsonSnapshot))
            {
                return Result.Success<ReportCardSnapshotDto?>(null);
            }

            var snapshot = JsonSerializer.Deserialize<ReportCardSnapshotDto>(jsonSnapshot, _jsonSerializerOptions);
            return Result.Success(snapshot);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<ReportCardSnapshotDto?>(
                DomainError.Failure(
                    "Academics.ReportCardSnapshotQueryFailed",
                    $"Failed to query report card snapshot for '{reportCardId}': {ex.Message}"));
        }
    }

    private const string _selectStudentAcademicStandingSql = """
        SELECT
            id AS Id,
            student_id AS StudentId,
            academic_period_id AS AcademicPeriodId,
            standing AS Standing,
            policy_reference AS PolicyReference,
            conditions AS Conditions,
            is_exception AS IsException,
            exception_reason AS ExceptionReason,
            committee_reference AS CommitteeReference,
            authorized_by AS AuthorizedBy,
            determined_at AS DeterminedAt
        FROM academics.academic_standings
        WHERE student_id = @StudentId
          AND academic_period_id = @AcademicPeriodId;
        """;

    private const string _selectPromotionRecommendationSql = """
        SELECT
            id AS Id,
            student_id AS StudentId,
            academic_period_id AS AcademicPeriodId,
            recommendation AS Recommendation,
            from_level AS FromLevel,
            recommended_to_level AS RecommendedToLevel,
            conditions AS Conditions,
            supporting_evidence AS SupportingEvidence,
            academic_standing_id AS AcademicStandingId,
            is_exception AS IsException,
            exception_reason AS ExceptionReason,
            committee_reference AS CommitteeReference,
            authorized_by AS AuthorizedBy,
            issued_at AS IssuedAt
        FROM academics.promotion_recommendations
        WHERE student_id = @StudentId
          AND academic_period_id = @AcademicPeriodId;
        """;

    private const string _selectStudentAcademicAchievementsSql = """
        SELECT
            id AS Id,
            student_id AS StudentId,
            learning_unit_id AS LearningUnitId,
            academic_period_id AS AcademicPeriodId,
            achievement_type AS AchievementType,
            title AS Title,
            description AS Description,
            criteria_met AS CriteriaMet,
            supporting_evidence AS SupportingEvidence,
            recognized_by AS RecognizedBy,
            recognized_at AS RecognizedAt
        FROM academics.academic_achievements
        WHERE student_id = @StudentId
          AND (@AcademicPeriodId IS NULL OR academic_period_id = @AcademicPeriodId)
        ORDER BY recognized_at DESC, created_at DESC;
        """;

    private const string _selectStudentLearningProgressSummarySql = """
        SELECT
            status AS Status,
            COUNT(1) AS UnitCount
        FROM academics.learning_progress
        WHERE student_id = @StudentId
          AND academic_period_id = @AcademicPeriodId
        GROUP BY status
        ORDER BY status ASC;
        """;

    /// <inheritdoc />
    public async Task<Result<StudentAcademicStandingDto?>> GetStudentAcademicStandingAsync(
        Guid studentId,
        Guid academicPeriodId,
        CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var standing = await connection.QuerySingleOrDefaultAsync<StudentAcademicStandingDto>(new CommandDefinition(
                _selectStudentAcademicStandingSql,
                new
                {
                    StudentId = studentId,
                    AcademicPeriodId = academicPeriodId
                },
                cancellationToken: ct));

            return Result.Success(standing);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<StudentAcademicStandingDto?>(
                DomainError.Failure(
                    "Academics.StudentAcademicStandingQueryFailed",
                    $"Failed to query academic standing for student '{studentId}' in period '{academicPeriodId}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<PromotionRecommendationDto?>> GetPromotionRecommendationAsync(
        Guid studentId,
        Guid academicPeriodId,
        CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var recommendation = await connection.QuerySingleOrDefaultAsync<PromotionRecommendationDto>(new CommandDefinition(
                _selectPromotionRecommendationSql,
                new
                {
                    StudentId = studentId,
                    AcademicPeriodId = academicPeriodId
                },
                cancellationToken: ct));

            return Result.Success(recommendation);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<PromotionRecommendationDto?>(
                DomainError.Failure(
                    "Academics.PromotionRecommendationQueryFailed",
                    $"Failed to query promotion recommendation for student '{studentId}' in period '{academicPeriodId}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AcademicAchievementDto>>> GetStudentAcademicAchievementsAsync(
        Guid studentId,
        Guid? academicPeriodId = null,
        CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var achievements = (await connection.QueryAsync<AcademicAchievementDto>(new CommandDefinition(
                _selectStudentAcademicAchievementsSql,
                new
                {
                    StudentId = studentId,
                    AcademicPeriodId = academicPeriodId
                },
                cancellationToken: ct))).AsList();

            return Result.Success<IReadOnlyList<AcademicAchievementDto>>(achievements);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<AcademicAchievementDto>>(
                DomainError.Failure(
                    "Academics.StudentAcademicAchievementsQueryFailed",
                    $"Failed to query academic achievements for student '{studentId}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LearningProgressSummaryDto>>> GetStudentLearningProgressSummaryAsync(
        Guid studentId,
        Guid academicPeriodId,
        CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var summary = (await connection.QueryAsync<LearningProgressSummaryDto>(new CommandDefinition(
                _selectStudentLearningProgressSummarySql,
                new
                {
                    StudentId = studentId,
                    AcademicPeriodId = academicPeriodId
                },
                cancellationToken: ct))).AsList();

            return Result.Success<IReadOnlyList<LearningProgressSummaryDto>>(summary);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<LearningProgressSummaryDto>>(
                DomainError.Failure(
                    "Academics.StudentLearningProgressSummaryQueryFailed",
                    $"Failed to query learning progress summary for student '{studentId}' in period '{academicPeriodId}': {ex.Message}"));
        }
    }
}

