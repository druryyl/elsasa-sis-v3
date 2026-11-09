namespace Elsasa.Sis.Academics.Features.RecordCorrections.CorrectApprovedResult;

using System.Text.Json;
using Dapper;
using Elsasa.Sis.Academics.Features.AcademicResults;
using Elsasa.Sis.Academics.Services.Reassessment;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using FluentValidation;
using MediatR;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler executing auditable in-place grade corrections for approved or published academic results (UC-ACD-19, TD-06, P6-S14).
/// Strictly enforces reason justification (minimum 10 characters), serializes pre/post mutation states to
/// <c>academics.academic_record_corrections</c>, re-adjudicates operative <c>is_final</c> flags across retake attempts,
/// and publishes post-commit <see cref="AcademicResultCorrectedEvent"/>.
/// </summary>
public sealed class CorrectApprovedResultHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IReassessmentPolicyEvaluator reassessmentPolicyEvaluator,
    IPublisher publisher,
    IValidator<CorrectApprovedResultRequest>? validator = null)
{
    static CorrectApprovedResultHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectResultForCorrectionSql = """
        SELECT
            r.id AS Id,
            r.assessment_id AS AssessmentId,
            r.student_id AS StudentId,
            r.applicant_id AS ApplicantId,
            r.attempt_number AS AttemptNumber,
            r.replaces_result_id AS ReplacesResultId,
            r.is_final AS IsFinal,
            r.score_numeric AS ScoreNumeric,
            r.score_max AS ScoreMax,
            r.grade_letter AS GradeLetter,
            r.grade_label AS GradeLabel,
            r.narrative_feedback AS NarrativeFeedback,
            r.evaluation_model AS EvaluationModel,
            r.evaluated_by AS EvaluatedBy,
            r.evaluated_date AS EvaluatedDate,
            r.result_status AS ResultStatus,
            r.approved_by AS ApprovedBy,
            r.approved_date AS ApprovedDate,
            r.published_date AS PublishedDate,
            r.notes AS Notes,
            r.created_at AS CreatedAt,
            r.created_by AS CreatedBy,
            r.updated_at AS UpdatedAt,
            r.updated_by AS UpdatedBy,
            a.reassessment_policy AS AssessmentReassessmentPolicy,
            a.passing_score AS AssessmentPassingScore,
            a.max_score AS AssessmentMaxScore
        FROM academics.academic_results r WITH (UPDLOCK, ROWLOCK)
        INNER JOIN academics.assessments a ON a.id = r.assessment_id
        WHERE r.id = @Id;
        """;

    private const string _insertCorrectionAuditSql = """
        INSERT INTO academics.academic_record_corrections (
            id,
            record_type,
            record_id,
            original_value,
            corrected_value,
            reason,
            evidence,
            corrected_by,
            corrected_date,
            is_material,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            'academic_result',
            @RecordId,
            @OriginalValue,
            @CorrectedValue,
            @Reason,
            @Evidence,
            @CorrectedBy,
            @CorrectedDate,
            @IsMaterial,
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private const string _selectCandidateAttemptsSql = """
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

    private const string _updateTargetResultSql = """
        UPDATE academics.academic_results
        SET score_numeric = @ScoreNumeric,
            score_max = @ScoreMax,
            grade_letter = @GradeLetter,
            grade_label = @GradeLabel,
            narrative_feedback = @NarrativeFeedback,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
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

    public async Task<Result<AcademicResultDto>> HandleAsync(
        Guid id,
        CorrectApprovedResultRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. FluentValidation check
        if (validator is not null)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var firstError = validationResult.Errors[0];
                return Result.Failure<AcademicResultDto>(
                    DomainError.Validation(firstError.PropertyName, firstError.ErrorMessage));
            }
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // 2. Query target academic result and parent assessment under lock
        var target = await connection.QuerySingleOrDefaultAsync<ResultWithAssessmentRow>(
            new CommandDefinition(
                _selectResultForCorrectionSql,
                new { Id = id },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (target is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<AcademicResultDto>(
                DomainError.NotFound(
                    "Academics.AcademicResults.NotFound",
                    $"Academic result '{id}' was not found."));
        }

        // 3. Verify target result is in 'approved' or 'published' status
        if (!string.Equals(target.ResultStatus, "approved", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(target.ResultStatus, "published", StringComparison.OrdinalIgnoreCase))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<AcademicResultDto>(
                DomainError.Conflict(
                    "Academics.AcademicResults.InvalidStatusForCorrection",
                    $"Only approved or published academic results can be corrected. Current status is '{target.ResultStatus}'."));
        }

        // 4. Score upper-bound sanity check
        var targetScoreMax = request.EffectiveScoreMax ?? target.ScoreMax ?? target.AssessmentMaxScore;
        if (request.EffectiveScoreNumeric.HasValue && targetScoreMax.HasValue && request.EffectiveScoreNumeric.Value > targetScoreMax.Value)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<AcademicResultDto>(
                DomainError.Validation(
                    "Academics.AcademicResults.ScoreExceedsMax",
                    $"Corrected score {request.EffectiveScoreNumeric.Value} exceeds maximum possible score {targetScoreMax.Value}."));
        }

        // 5. Serialize pre-mutation snapshot into JSON for immutable audit log
        var originalSnapshot = new
        {
            score_numeric = target.ScoreNumeric,
            score_max = target.ScoreMax,
            grade_letter = target.GradeLetter,
            grade_label = target.GradeLabel,
            narrative_feedback = target.NarrativeFeedback
        };
        var originalValueJson = JsonSerializer.Serialize(originalSnapshot);

        // 6. Compute proposed updated values
        decimal? updatedScoreNumeric = request.EffectiveScoreNumeric ?? target.ScoreNumeric;
        decimal? updatedScoreMax = request.EffectiveScoreMax ?? target.ScoreMax;
        string? updatedGradeLetter = request.EffectiveGradeLetter ?? target.GradeLetter;
        string? updatedGradeLabel = request.EffectiveGradeLabel ?? target.GradeLabel;
        string? updatedNarrativeFeedback = request.EffectiveNarrativeFeedback ?? target.NarrativeFeedback;

        var correctedSnapshot = new
        {
            score_numeric = updatedScoreNumeric,
            score_max = updatedScoreMax,
            grade_letter = updatedGradeLetter,
            grade_label = updatedGradeLabel,
            narrative_feedback = updatedNarrativeFeedback
        };
        var correctedValueJson = JsonSerializer.Serialize(correctedSnapshot);

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var correctedBy = currentUser.Username ?? "authority";
        var correctionId = Guid.NewGuid();

        // 7. Insert audit record into academics.academic_record_corrections
        await connection.ExecuteAsync(new CommandDefinition(
            _insertCorrectionAuditSql,
            new
            {
                Id = correctionId,
                RecordId = target.Id,
                OriginalValue = originalValueJson,
                CorrectedValue = correctedValueJson,
                Reason = request.Reason.Trim(),
                Evidence = string.IsNullOrWhiteSpace(request.Evidence) ? null : request.Evidence.Trim(),
                CorrectedBy = correctedBy,
                CorrectedDate = today,
                request.IsMaterial,
                Notes = (string?)null,
                CreatedAt = now,
                CreatedBy = correctedBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 8. Re-evaluate operative attempt outcome via IReassessmentPolicyEvaluator
        var candidateAttempts = (await connection.QueryAsync<AttemptRow>(new CommandDefinition(
            _selectCandidateAttemptsSql,
            new
            {
                target.AssessmentId,
                target.StudentId,
                target.ApplicantId
            },
            transaction: transaction,
            cancellationToken: cancellationToken))).ToList();

        var otherAttempts = candidateAttempts
            .Where(a => a.Id != target.Id)
            .Select(a => new AttemptScoreRecord(
                a.Id,
                a.AttemptNumber,
                a.ScoreNumeric,
                a.GradeLetter,
                a.GradeLabel,
                a.EvaluatedDate.ToDateTime(TimeOnly.MinValue)))
            .ToList();

        var evaluatedDateTime = target.EvaluatedDate.ToDateTime(TimeOnly.MinValue);
        var targetAttemptRecord = new AttemptScoreRecord(
            target.Id,
            target.AttemptNumber,
            updatedScoreNumeric,
            updatedGradeLetter,
            updatedGradeLabel,
            evaluatedDateTime);

        var evalResult = reassessmentPolicyEvaluator.Evaluate(
            target.AssessmentReassessmentPolicy,
            target.AssessmentPassingScore,
            otherAttempts,
            targetAttemptRecord);

        // For 'average' policy, latest attempt carries calculated arithmetic mean
        decimal? finalScoreNumeric = string.Equals(target.AssessmentReassessmentPolicy, ReassessmentPolicies.Average, StringComparison.OrdinalIgnoreCase)
                                     && evalResult.OperativeScoreNumeric.HasValue
            ? evalResult.OperativeScoreNumeric
            : updatedScoreNumeric;

        bool isTargetOperative = evalResult.OperativeResultId == target.Id;

        // 9. Update target academic result values
        await connection.ExecuteAsync(new CommandDefinition(
            _updateTargetResultSql,
            new
            {
                target.Id,
                ScoreNumeric = finalScoreNumeric,
                ScoreMax = updatedScoreMax,
                GradeLetter = updatedGradeLetter,
                GradeLabel = updatedGradeLabel,
                NarrativeFeedback = updatedNarrativeFeedback,
                UpdatedAt = now,
                UpdatedBy = correctedBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 10. Update operative is_final flags across all candidate attempts
        await connection.ExecuteAsync(new CommandDefinition(
            _updateOperativeAttemptSql,
            new
            {
                OperativeId = evalResult.OperativeResultId,
                target.AssessmentId,
                target.StudentId,
                target.ApplicantId
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 11. Commit transaction
        await transaction.CommitAsync(cancellationToken);

        // 12. Publish post-commit domain event (ADR-003, UC-ACD-19, TD-06)
        await publisher.Publish(
            new AcademicResultCorrectedEvent(
                target.Id,
                target.AssessmentId,
                target.StudentId,
                request.Reason.Trim(),
                request.IsMaterial,
                correctedBy),
            cancellationToken);

        var response = new AcademicResultDto(
            target.Id,
            target.AssessmentId,
            target.StudentId,
            target.ApplicantId,
            target.AttemptNumber,
            target.ReplacesResultId,
            isTargetOperative,
            finalScoreNumeric,
            updatedScoreMax,
            updatedGradeLetter,
            updatedGradeLabel,
            updatedNarrativeFeedback,
            target.EvaluationModel,
            target.EvaluatedBy,
            target.EvaluatedDate,
            target.ResultStatus,
            target.ApprovedBy,
            target.ApprovedDate,
            target.PublishedDate,
            target.Notes,
            target.CreatedAt,
            target.CreatedBy,
            now,
            correctedBy);

        return Result.Success(response);
    }

    private sealed record ResultWithAssessmentRow(
        Guid Id,
        Guid AssessmentId,
        Guid? StudentId,
        Guid? ApplicantId,
        int AttemptNumber,
        Guid? ReplacesResultId,
        bool IsFinal,
        decimal? ScoreNumeric,
        decimal? ScoreMax,
        string? GradeLetter,
        string? GradeLabel,
        string? NarrativeFeedback,
        string EvaluationModel,
        string EvaluatedBy,
        DateOnly EvaluatedDate,
        string ResultStatus,
        string? ApprovedBy,
        DateOnly? ApprovedDate,
        DateOnly? PublishedDate,
        string? Notes,
        DateTimeOffset CreatedAt,
        string CreatedBy,
        DateTimeOffset? UpdatedAt,
        string? UpdatedBy,
        string AssessmentReassessmentPolicy,
        decimal? AssessmentPassingScore,
        decimal? AssessmentMaxScore);

    private sealed record AttemptRow(
        Guid Id,
        int AttemptNumber,
        bool IsFinal,
        decimal? ScoreNumeric,
        string? GradeLetter,
        string? GradeLabel,
        DateOnly EvaluatedDate);
}
