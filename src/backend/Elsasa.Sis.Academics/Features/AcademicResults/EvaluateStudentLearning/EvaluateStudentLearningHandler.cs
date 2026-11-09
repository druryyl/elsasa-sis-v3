namespace Elsasa.Sis.Academics.Features.AcademicResults.EvaluateStudentLearning;

using Dapper;
using Elsasa.Sis.Academics.Services.Reassessment;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Contracts.Students;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler executing student/applicant academic evaluation recording (UC-ACD-06, TD-03, TD-04, P4-S08).
/// Enforces assessment lifecycle guards, student enrollment verification via <see cref="IStudentsModule"/>,
/// multi-attempt progression, atomic <c>is_final</c> adjudication via <see cref="IReassessmentPolicyEvaluator"/>,
/// autonomous formative release, and post-commit domain event publication.
/// </summary>
public sealed class EvaluateStudentLearningHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IStudentsModule studentsModule,
    IReassessmentPolicyEvaluator reassessmentPolicyEvaluator,
    IPublisher publisher)
{
    static EvaluateStudentLearningHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAssessmentSql = """
        SELECT
            id AS Id,
            status AS Status,
            assessment_type AS AssessmentType,
            max_score AS MaxScore,
            passing_score AS PassingScore,
            reassessment_policy AS ReassessmentPolicy
        FROM academics.assessments
        WHERE id = @AssessmentId;
        """;

    private const string _selectPriorAttemptsSql = """
        SELECT
            id AS Id,
            attempt_number AS AttemptNumber,
            is_final AS IsFinal,
            score_numeric AS ScoreNumeric,
            grade_letter AS GradeLetter,
            grade_label AS GradeLabel,
            evaluated_date AS EvaluatedDate
        FROM academics.academic_results
        WHERE assessment_id = @AssessmentId
          AND (
              (@StudentId IS NOT NULL AND student_id = @StudentId)
              OR (@ApplicantId IS NOT NULL AND applicant_id = @ApplicantId)
          )
        ORDER BY attempt_number ASC;
        """;

    private const string _insertResultSql = """
        INSERT INTO academics.academic_results (
            id,
            assessment_id,
            student_id,
            applicant_id,
            attempt_number,
            replaces_result_id,
            is_final,
            score_numeric,
            score_max,
            grade_letter,
            grade_label,
            narrative_feedback,
            evaluation_model,
            evaluated_by,
            evaluated_date,
            result_status,
            approved_by,
            approved_date,
            published_date,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @AssessmentId,
            @StudentId,
            @ApplicantId,
            @AttemptNumber,
            @ReplacesResultId,
            @IsFinal,
            @ScoreNumeric,
            @ScoreMax,
            @GradeLetter,
            @GradeLabel,
            @NarrativeFeedback,
            @EvaluationModel,
            @EvaluatedBy,
            @EvaluatedDate,
            @ResultStatus,
            @ApprovedBy,
            @ApprovedDate,
            @PublishedDate,
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private const string _updateOperativeAttemptSql = """
        UPDATE academics.academic_results
        SET is_final = CASE WHEN id = @OperativeId THEN 1 ELSE 0 END
        WHERE assessment_id = @AssessmentId
          AND (
              (@StudentId IS NOT NULL AND student_id = @StudentId)
              OR (@ApplicantId IS NOT NULL AND applicant_id = @ApplicantId)
          );
        """;

    private sealed record AssessmentHeaderRow(
        Guid Id,
        string Status,
        string AssessmentType,
        decimal? MaxScore,
        decimal? PassingScore,
        string ReassessmentPolicy);

    private sealed record PriorAttemptRow(
        Guid Id,
        int AttemptNumber,
        bool IsFinal,
        decimal? ScoreNumeric,
        string? GradeLetter,
        string? GradeLabel,
        DateOnly EvaluatedDate);

    public async Task<Result<AcademicResultDto>> HandleAsync(
        Guid assessmentId,
        EvaluateStudentLearningRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Synchronously validate student enrollment via IStudentsModule when student_id is provided
        if (request.StudentId.HasValue)
        {
            var enrolledResult = await studentsModule.IsStudentEnrolledAsync(request.StudentId.Value, cancellationToken);
            if (enrolledResult.IsFailure || !enrolledResult.Value)
            {
                return Result.Failure<AcademicResultDto>(
                    DomainError.Validation(
                        "Academic.StudentNotEnrolled",
                        $"Student with ID '{request.StudentId.Value}' is not actively enrolled."));
            }
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 2. Retrieve parent assessment details
        var assessment = await connection.QuerySingleOrDefaultAsync<AssessmentHeaderRow>(new CommandDefinition(
            _selectAssessmentSql,
            new { AssessmentId = assessmentId },
            cancellationToken: cancellationToken));

        if (assessment is null)
        {
            return Result.Failure<AcademicResultDto>(
                DomainError.NotFound(
                    "Academic.AssessmentNotFound",
                    $"Assessment with ID '{assessmentId}' was not found."));
        }

        if (string.Equals(assessment.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<AcademicResultDto>(
                DomainError.Conflict(
                    "Academics.Assessments.AssessmentCancelled",
                    $"Cannot evaluate results for assessment '{assessmentId}' because it has been cancelled."));
        }

        // 3. Score boundary validation against assessment max_score
        var effectiveScoreMax = request.ScoreMax ?? assessment.MaxScore;
        if (request.ScoreNumeric.HasValue && effectiveScoreMax.HasValue && request.ScoreNumeric.Value > effectiveScoreMax.Value)
        {
            return Result.Failure<AcademicResultDto>(
                DomainError.Validation(
                    "Academic.ScoreExceedsMaxScore",
                    $"Numeric score '{request.ScoreNumeric.Value}' exceeds maximum score '{effectiveScoreMax.Value}'."));
        }

        // 4. Begin explicit transaction for multi-attempt history and is_final adjudication
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Query existing attempt history for candidate and assessment
        var priorAttempts = (await connection.QueryAsync<PriorAttemptRow>(new CommandDefinition(
            _selectPriorAttemptsSql,
            new
            {
                AssessmentId = assessmentId,
                request.StudentId,
                request.ApplicantId
            },
            transaction: transaction,
            cancellationToken: cancellationToken))).ToList();

        int priorMaxAttempt = priorAttempts.Count > 0 ? priorAttempts.Max(a => a.AttemptNumber) : 0;
        Guid? priorOperativeId = priorAttempts.FirstOrDefault(a => a.IsFinal)?.Id
                                ?? priorAttempts.LastOrDefault()?.Id;

        int newAttemptNumber = priorMaxAttempt + 1;
        Guid? newReplacesResultId = priorOperativeId;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var evaluatedDate = today;
        var evaluatedDateTime = evaluatedDate.ToDateTime(TimeOnly.MinValue);
        var newResultId = Guid.NewGuid();

        // 5. Evaluate Reassessment Policy via pure domain service
        var existingRecords = priorAttempts
            .Select(a => new AttemptScoreRecord(
                a.Id,
                a.AttemptNumber,
                a.ScoreNumeric,
                a.GradeLetter,
                a.GradeLabel,
                a.EvaluatedDate.ToDateTime(TimeOnly.MinValue)))
            .ToList();

        var candidateRecord = new AttemptScoreRecord(
            newResultId,
            newAttemptNumber,
            request.ScoreNumeric,
            request.GradeLetter,
            request.GradeLabel,
            evaluatedDateTime);

        var evalResult = reassessmentPolicyEvaluator.Evaluate(
            assessment.ReassessmentPolicy,
            assessment.PassingScore,
            existingRecords,
            candidateRecord);

        // For 'average' policy, latest attempt carries calculated arithmetic mean
        decimal? scoreToSave = string.Equals(assessment.ReassessmentPolicy, ReassessmentPolicies.Average, StringComparison.OrdinalIgnoreCase)
                               && evalResult.OperativeScoreNumeric.HasValue
            ? evalResult.OperativeScoreNumeric
            : request.ScoreNumeric;

        bool isNewAttemptOperative = evalResult.OperativeResultId == newResultId;

        // 6. Three-Tier Result Governance Lifecycle & Formative Autonomous Release (TD-04, UC-ACD-06)
        bool isFormative = string.Equals(assessment.AssessmentType, "formative", StringComparison.OrdinalIgnoreCase);
        bool publishImmediately = request.PublishImmediately == true;

        string resultStatus;
        string? approvedBy;
        DateOnly? approvedDate;
        DateOnly? publishedDate;

        if (isFormative && publishImmediately)
        {
            resultStatus = "published";
            approvedBy = currentUser.Username ?? "teacher";
            approvedDate = evaluatedDate;
            publishedDate = evaluatedDate;
        }
        else
        {
            resultStatus = "provisional";
            approvedBy = null;
            approvedDate = null;
            publishedDate = null;
        }

        var now = DateTimeOffset.UtcNow;
        var evaluatedBy = currentUser.Username ?? "system";
        var evaluationModel = request.EvaluationModel.Trim().ToLowerInvariant();
        var gradeLetter = string.IsNullOrWhiteSpace(request.GradeLetter) ? null : request.GradeLetter.Trim();
        var gradeLabel = string.IsNullOrWhiteSpace(request.GradeLabel) ? null : request.GradeLabel.Trim();
        var narrativeFeedback = string.IsNullOrWhiteSpace(request.NarrativeFeedback) ? null : request.NarrativeFeedback.Trim();
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        // 7. Insert new result attempt record
        await connection.ExecuteAsync(new CommandDefinition(
            _insertResultSql,
            new
            {
                Id = newResultId,
                AssessmentId = assessmentId,
                request.StudentId,
                request.ApplicantId,
                AttemptNumber = newAttemptNumber,
                ReplacesResultId = newReplacesResultId,
                IsFinal = isNewAttemptOperative,
                ScoreNumeric = scoreToSave,
                ScoreMax = effectiveScoreMax,
                GradeLetter = gradeLetter,
                GradeLabel = gradeLabel,
                NarrativeFeedback = narrativeFeedback,
                EvaluationModel = evaluationModel,
                EvaluatedBy = evaluatedBy,
                EvaluatedDate = evaluatedDate,
                ResultStatus = resultStatus,
                ApprovedBy = approvedBy,
                ApprovedDate = approvedDate,
                PublishedDate = publishedDate,
                Notes = notes,
                CreatedAt = now,
                CreatedBy = evaluatedBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 8. Atomically update operative is_final flag across all attempts
        await connection.ExecuteAsync(new CommandDefinition(
            _updateOperativeAttemptSql,
            new
            {
                OperativeId = evalResult.OperativeResultId,
                AssessmentId = assessmentId,
                request.StudentId,
                request.ApplicantId
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 9. Commit transaction
        await transaction.CommitAsync(cancellationToken);

        // 10. Emit post-commit domain event if formative result was autonomously published (ADR-003, TD-04)
        if (string.Equals(resultStatus, "published", StringComparison.OrdinalIgnoreCase))
        {
            await publisher.Publish(
                new AcademicResultPublishedEvent(
                    newResultId,
                    assessmentId,
                    request.StudentId,
                    scoreToSave,
                    gradeLetter),
                cancellationToken);
        }

        var response = new AcademicResultDto(
            newResultId,
            assessmentId,
            request.StudentId,
            request.ApplicantId,
            newAttemptNumber,
            newReplacesResultId,
            isNewAttemptOperative,
            scoreToSave,
            effectiveScoreMax,
            gradeLetter,
            gradeLabel,
            narrativeFeedback,
            evaluationModel,
            evaluatedBy,
            evaluatedDate,
            resultStatus,
            approvedBy,
            approvedDate,
            publishedDate,
            notes,
            now,
            evaluatedBy,
            null,
            null);

        return Result.Success(response);
    }
}
