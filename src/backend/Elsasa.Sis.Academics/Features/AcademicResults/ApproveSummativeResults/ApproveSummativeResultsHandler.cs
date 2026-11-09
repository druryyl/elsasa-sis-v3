namespace Elsasa.Sis.Academics.Features.AcademicResults.ApproveSummativeResults;

using Dapper;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using FluentValidation;
using MediatR;

/// <summary>
/// Feature handler certifying provisional summative academic results to approved status (UC-ACD-16, P4-S09, TD-04).
/// Ensures assessment is conducted, validates target provisional results, executes atomic certification in a SQL transaction,
/// and publishes <see cref="AcademicResultApprovedEvent"/> post-commit for each certified record.
/// </summary>
public sealed class ApproveSummativeResultsHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher,
    IValidator<ApproveSummativeResultsRequest>? validator = null)
{
    static ApproveSummativeResultsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAssessmentSql = """
        SELECT
            id AS Id,
            status AS Status,
            assessment_type AS AssessmentType
        FROM academics.assessments
        WHERE id = @AssessmentId;
        """;

    private const string _selectSpecificResultsSql = """
        SELECT
            id AS Id,
            assessment_id AS AssessmentId,
            student_id AS StudentId,
            applicant_id AS ApplicantId,
            attempt_number AS AttemptNumber,
            is_final AS IsFinal,
            score_numeric AS ScoreNumeric,
            grade_letter AS GradeLetter,
            result_status AS ResultStatus
        FROM academics.academic_results
        WHERE assessment_id = @AssessmentId
          AND id IN @ResultIds;
        """;

    private const string _selectAllProvisionalOperativeSql = """
        SELECT
            id AS Id,
            assessment_id AS AssessmentId,
            student_id AS StudentId,
            applicant_id AS ApplicantId,
            attempt_number AS AttemptNumber,
            is_final AS IsFinal,
            score_numeric AS ScoreNumeric,
            grade_letter AS GradeLetter,
            result_status AS ResultStatus
        FROM academics.academic_results
        WHERE assessment_id = @AssessmentId
          AND is_final = 1
          AND result_status = 'provisional';
        """;

    private const string _updateApproveSql = """
        UPDATE academics.academic_results
        SET
            result_status = 'approved',
            approved_by = @ApprovedBy,
            approved_date = @ApprovedDate,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id IN @Ids;
        """;

    private sealed record AssessmentHeaderRow(
        Guid Id,
        string Status,
        string AssessmentType);

    private sealed record AcademicResultRow(
        Guid Id,
        Guid AssessmentId,
        Guid? StudentId,
        Guid? ApplicantId,
        int AttemptNumber,
        bool IsFinal,
        decimal? ScoreNumeric,
        string? GradeLetter,
        string ResultStatus);

    public async Task<Result<ApproveSummativeResultsResponse>> HandleAsync(
        Guid assessmentId,
        ApproveSummativeResultsRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate request if provided
        if (request is not null && validator is not null)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<ApproveSummativeResultsResponse>(
                    DomainError.Validation(
                        "Academics.Results.ValidationFailed",
                        "One or more validation errors occurred.",
                        validationResult.ToDictionary()));
            }
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 2. Validate parent assessment exists and lifecycle status
        var assessment = await connection.QuerySingleOrDefaultAsync<AssessmentHeaderRow>(new CommandDefinition(
            _selectAssessmentSql,
            new { AssessmentId = assessmentId },
            cancellationToken: cancellationToken));

        if (assessment is null)
        {
            return Result.Failure<ApproveSummativeResultsResponse>(
                DomainError.NotFound(
                    "Academics.Assessments.NotFound",
                    $"Assessment with ID '{assessmentId}' was not found."));
        }

        if (string.Equals(assessment.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ApproveSummativeResultsResponse>(
                DomainError.Conflict(
                    "Academics.Assessments.AssessmentCancelled",
                    $"Cannot approve results for assessment '{assessmentId}' because it has been cancelled."));
        }

        if (!string.Equals(assessment.Status, "conducted", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ApproveSummativeResultsResponse>(
                DomainError.Conflict(
                    "Academics.Assessments.AssessmentNotConducted",
                    $"Cannot approve results for assessment '{assessmentId}' because it is in '{assessment.Status}' status. Assessment must be conducted before approving results."));
        }

        // 3. Determine target results to approve
        List<AcademicResultRow> resultsToApprove;

        if (request?.ResultIds is not null && request.ResultIds.Count > 0)
        {
            var targetIds = request.ResultIds.Distinct().ToList();

            var existingResults = (await connection.QueryAsync<AcademicResultRow>(new CommandDefinition(
                _selectSpecificResultsSql,
                new { AssessmentId = assessmentId, ResultIds = targetIds },
                cancellationToken: cancellationToken))).ToList();

            var foundIds = existingResults.Select(r => r.Id).ToHashSet();
            var missingIds = targetIds.Where(id => !foundIds.Contains(id)).ToList();
            if (missingIds.Count > 0)
            {
                return Result.Failure<ApproveSummativeResultsResponse>(
                    DomainError.NotFound(
                        "Academics.Results.NotFound",
                        $"One or more specified academic results were not found for assessment '{assessmentId}': [{string.Join(", ", missingIds)}]."));
            }

            var nonProvisional = existingResults
                .Where(r => !string.Equals(r.ResultStatus, "provisional", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (nonProvisional.Count > 0)
            {
                return Result.Failure<ApproveSummativeResultsResponse>(
                    DomainError.Conflict(
                        "Academics.Results.InvalidStatus",
                        $"One or more academic results cannot be approved because their status is not 'provisional': [{string.Join(", ", nonProvisional.Select(r => $"{r.Id} ({r.ResultStatus})"))}]."));
            }

            resultsToApprove = existingResults;
        }
        else
        {
            resultsToApprove = (await connection.QueryAsync<AcademicResultRow>(new CommandDefinition(
                _selectAllProvisionalOperativeSql,
                new { AssessmentId = assessmentId },
                cancellationToken: cancellationToken))).ToList();
        }

        if (resultsToApprove.Count == 0)
        {
            return Result.Success(new ApproveSummativeResultsResponse(0));
        }

        // 4. Atomically certify results in SQL transaction
        var approvedBy = currentUser.Username ?? "system";
        var approvedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var updatedAt = DateTimeOffset.UtcNow;
        var updatedBy = approvedBy;
        var targetResultIds = resultsToApprove.Select(r => r.Id).ToList();

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            _updateApproveSql,
            new
            {
                ApprovedBy = approvedBy,
                ApprovedDate = approvedDate,
                UpdatedAt = updatedAt,
                UpdatedBy = updatedBy,
                Ids = targetResultIds
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        // 5. Publish post-commit domain events (ADR-003, UC-ACD-16)
        foreach (var row in resultsToApprove)
        {
            await publisher.Publish(
                new AcademicResultApprovedEvent(
                    row.Id,
                    row.AssessmentId,
                    row.StudentId,
                    row.ApplicantId,
                    row.ScoreNumeric,
                    row.GradeLetter,
                    approvedBy),
                cancellationToken);
        }

        return Result.Success(new ApproveSummativeResultsResponse(resultsToApprove.Count));
    }
}
