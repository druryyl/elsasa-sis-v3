namespace Elsasa.Sis.Academics.Features.AcademicResults.PublishAcademicResults;

using Dapper;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using FluentValidation;
using MediatR;

/// <summary>
/// Feature handler releasing approved academic results to published status for student/parent portal visibility (UC-ACD-16, P4-S09, TD-04).
/// Ensures assessment is active, validates target results are approved, executes atomic publication in a SQL transaction,
/// and emits <see cref="AcademicResultPublishedEvent"/> post-commit for each released record.
/// </summary>
public sealed class PublishAcademicResultsHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher,
    IValidator<PublishAcademicResultsRequest>? validator = null)
{
    static PublishAcademicResultsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAssessmentSql = """
        SELECT
            id AS Id,
            status AS Status
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

    private const string _selectAllApprovedOperativeSql = """
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
          AND result_status = 'approved';
        """;

    private const string _updatePublishSql = """
        UPDATE academics.academic_results
        SET
            result_status = 'published',
            published_date = @PublishedDate,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id IN @Ids;
        """;

    private sealed record AssessmentHeaderRow(
        Guid Id,
        string Status);

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

    public async Task<Result<PublishAcademicResultsResponse>> HandleAsync(
        Guid assessmentId,
        PublishAcademicResultsRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate request if provided
        if (request is not null && validator is not null)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<PublishAcademicResultsResponse>(
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
            return Result.Failure<PublishAcademicResultsResponse>(
                DomainError.NotFound(
                    "Academics.Assessments.NotFound",
                    $"Assessment with ID '{assessmentId}' was not found."));
        }

        if (string.Equals(assessment.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<PublishAcademicResultsResponse>(
                DomainError.Conflict(
                    "Academics.Assessments.AssessmentCancelled",
                    $"Cannot publish results for assessment '{assessmentId}' because it has been cancelled."));
        }

        // 3. Determine target results to publish
        List<AcademicResultRow> resultsToPublish;

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
                return Result.Failure<PublishAcademicResultsResponse>(
                    DomainError.NotFound(
                        "Academics.Results.NotFound",
                        $"One or more specified academic results were not found for assessment '{assessmentId}': [{string.Join(", ", missingIds)}]."));
            }

            var nonApproved = existingResults
                .Where(r => !string.Equals(r.ResultStatus, "approved", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (nonApproved.Count > 0)
            {
                return Result.Failure<PublishAcademicResultsResponse>(
                    DomainError.Conflict(
                        "Academics.Results.InvalidStatus",
                        $"One or more academic results cannot be published because their status is not 'approved': [{string.Join(", ", nonApproved.Select(r => $"{r.Id} ({r.ResultStatus})"))}]. Only approved results can be published."));
            }

            resultsToPublish = existingResults;
        }
        else
        {
            resultsToPublish = (await connection.QueryAsync<AcademicResultRow>(new CommandDefinition(
                _selectAllApprovedOperativeSql,
                new { AssessmentId = assessmentId },
                cancellationToken: cancellationToken))).ToList();
        }

        if (resultsToPublish.Count == 0)
        {
            return Result.Success(new PublishAcademicResultsResponse(0));
        }

        // 4. Atomically release results to published in SQL transaction
        var publishedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var updatedAt = DateTimeOffset.UtcNow;
        var updatedBy = currentUser.Username ?? "system";
        var targetResultIds = resultsToPublish.Select(r => r.Id).ToList();

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            _updatePublishSql,
            new
            {
                PublishedDate = publishedDate,
                UpdatedAt = updatedAt,
                UpdatedBy = updatedBy,
                Ids = targetResultIds
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        // 5. Publish post-commit domain events (ADR-003, UC-ACD-16)
        foreach (var row in resultsToPublish)
        {
            await publisher.Publish(
                new AcademicResultPublishedEvent(
                    row.Id,
                    row.AssessmentId,
                    row.StudentId,
                    row.ScoreNumeric,
                    row.GradeLetter),
                cancellationToken);
        }

        return Result.Success(new PublishAcademicResultsResponse(resultsToPublish.Count));
    }
}
