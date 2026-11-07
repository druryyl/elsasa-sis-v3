namespace Elsasa.Sis.Academics.Features.PromotionRecommendations.BatchIssuePromotionRecommendations;

using Dapper;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using FluentValidation;
using MediatR;

/// <summary>
/// Feature handler executing cohort batch academic progression recommendation issuance (P3-S08, UC-ACD-17, TD-03).
/// Atomically upserts recommendation records in a single database transaction and publishes <see cref="PromotionRecommendationIssuedEvent"/> for each student post-commit.
/// </summary>
public sealed class BatchIssuePromotionRecommendationsHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher,
    IValidator<BatchIssuePromotionRecommendationsCommand>? validator = null)
{
    static BatchIssuePromotionRecommendationsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _upsertRecommendationSql = """
        MERGE INTO academics.promotion_recommendations WITH (HOLDLOCK) AS target
        USING (
            SELECT
                @StudentId AS student_id,
                @AcademicPeriodId AS academic_period_id
        ) AS source
        ON target.student_id = source.student_id
           AND target.academic_period_id = source.academic_period_id
        WHEN MATCHED THEN
            UPDATE SET
                recommendation = @Recommendation,
                from_level = @FromLevel,
                recommended_to_level = @RecommendedToLevel,
                conditions = @Conditions,
                supporting_evidence = @SupportingEvidence,
                academic_standing_id = @AcademicStandingId,
                is_exception = @IsException,
                exception_reason = @ExceptionReason,
                committee_reference = @CommitteeReference,
                authorized_by = @AuthorizedBy,
                issued_at = @IssuedAt,
                updated_at = @IssuedAt,
                updated_by = @AuthorizedBy
        WHEN NOT MATCHED THEN
            INSERT (
                id,
                student_id,
                academic_period_id,
                recommendation,
                from_level,
                recommended_to_level,
                conditions,
                supporting_evidence,
                academic_standing_id,
                is_exception,
                exception_reason,
                committee_reference,
                authorized_by,
                issued_at,
                created_at,
                created_by
            )
            VALUES (
                @NewId,
                @StudentId,
                @AcademicPeriodId,
                @Recommendation,
                @FromLevel,
                @RecommendedToLevel,
                @Conditions,
                @SupportingEvidence,
                @AcademicStandingId,
                @IsException,
                @ExceptionReason,
                @CommitteeReference,
                @AuthorizedBy,
                @IssuedAt,
                @IssuedAt,
                @AuthorizedBy
            )
        OUTPUT
            inserted.id AS Id,
            inserted.student_id AS StudentId,
            inserted.academic_period_id AS AcademicPeriodId,
            inserted.recommendation AS Recommendation,
            inserted.from_level AS FromLevel,
            inserted.recommended_to_level AS RecommendedToLevel,
            inserted.conditions AS Conditions,
            inserted.supporting_evidence AS SupportingEvidence,
            inserted.academic_standing_id AS AcademicStandingId,
            inserted.is_exception AS IsException,
            inserted.exception_reason AS ExceptionReason,
            inserted.committee_reference AS CommitteeReference,
            inserted.authorized_by AS AuthorizedBy,
            inserted.issued_at AS IssuedAt;
        """;

    private const string _queryStandingsSql = """
        SELECT student_id AS StudentId, id AS StandingId
        FROM academics.academic_standings
        WHERE academic_period_id = @AcademicPeriodId
          AND student_id IN @StudentIds;
        """;

    /// <summary>
    /// Executes atomic transaction to validate and issue progression recommendations for all students in the batch.
    /// </summary>
    /// <param name="command">Batch command containing target period and item list.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary response of processed recommendations or domain error.</returns>
    public async Task<Result<BatchIssuePromotionRecommendationsResponse>> HandleAsync(
        BatchIssuePromotionRecommendationsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (validator is not null)
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<BatchIssuePromotionRecommendationsResponse>(
                    DomainError.Validation(
                        "Academic.ValidationFailed",
                        string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))));
            }
        }

        if (command.Items is null || command.Items.Count == 0)
        {
            return Result.Failure<BatchIssuePromotionRecommendationsResponse>(
                DomainError.Validation(
                    "Academic.EmptyBatch",
                    "At least one progression recommendation item is required."));
        }

        var authorizedBy = !string.IsNullOrWhiteSpace(currentUser.Username)
            ? currentUser.Username
            : (!string.IsNullOrWhiteSpace(currentUser.Email) ? currentUser.Email : "Academic Authority");
        var now = DateTimeOffset.UtcNow;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var issuedRows = new List<PromotionRecommendationDto>(command.Items.Count);

        try
        {
            // Resolve linked academic_standing_id for students from academics.academic_standings
            var studentIds = command.Items.Select(x => x.StudentId).Distinct().ToArray();
            var standingLookup = (await connection.QueryAsync<(Guid StudentId, Guid StandingId)>(
                new CommandDefinition(
                    _queryStandingsSql,
                    new { command.AcademicPeriodId, StudentIds = studentIds },
                    transaction: transaction,
                    cancellationToken: cancellationToken)))
                .ToDictionary(x => x.StudentId, x => x.StandingId);

            foreach (var item in command.Items)
            {
                var normalizedRecommendation = item.Recommendation.Trim().ToLowerInvariant();

                // Explicit item-level exception constraints enforcement
                if (item.IsException)
                {
                    if (string.IsNullOrWhiteSpace(item.ExceptionReason) || item.ExceptionReason.Trim().Length < 5)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return Result.Failure<BatchIssuePromotionRecommendationsResponse>(
                            DomainError.Validation(
                                "Academic.ExceptionReasonInvalid",
                                $"Exception reason is mandatory (minimum 5 characters) for student '{item.StudentId}' when marked as an exception."));
                    }

                    if (string.IsNullOrWhiteSpace(item.CommitteeReference) || item.CommitteeReference.Trim().Length < 5)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return Result.Failure<BatchIssuePromotionRecommendationsResponse>(
                            DomainError.Validation(
                                "Academic.CommitteeReferenceInvalid",
                                $"Committee reference is mandatory (minimum 5 characters) for student '{item.StudentId}' when marked as an exception."));
                    }
                }

                if (string.Equals(normalizedRecommendation, "conditional_promotion", StringComparison.OrdinalIgnoreCase) &&
                    string.IsNullOrWhiteSpace(item.Conditions))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Failure<BatchIssuePromotionRecommendationsResponse>(
                        DomainError.Validation(
                            "Academic.ConditionsRequired",
                            $"Conditions are mandatory for student '{item.StudentId}' when recommendation is 'conditional_promotion'."));
                }

                // Resolve standing ID: prefer item provided, otherwise use resolved standing from database
                Guid? standingId = item.AcademicStandingId;
                if (!standingId.HasValue || standingId.Value == Guid.Empty)
                {
                    if (standingLookup.TryGetValue(item.StudentId, out var resolvedStandingId))
                    {
                        standingId = resolvedStandingId;
                    }
                    else
                    {
                        standingId = null;
                    }
                }

                var row = await connection.QuerySingleAsync<PromotionRecommendationDto>(
                    new CommandDefinition(
                        _upsertRecommendationSql,
                        new
                        {
                            NewId = Guid.NewGuid(),
                            item.StudentId,
                            command.AcademicPeriodId,
                            Recommendation = normalizedRecommendation,
                            item.FromLevel,
                            item.RecommendedToLevel,
                            item.Conditions,
                            item.SupportingEvidence,
                            AcademicStandingId = standingId,
                            item.IsException,
                            item.ExceptionReason,
                            item.CommitteeReference,
                            AuthorizedBy = authorizedBy,
                            IssuedAt = now
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                issuedRows.Add(row);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        // Post-commit: Publish PromotionRecommendationIssuedEvent for each student
        foreach (var row in issuedRows)
        {
            await publisher.Publish(
                new PromotionRecommendationIssuedEvent(
                    row.Id,
                    row.StudentId,
                    row.AcademicPeriodId,
                    row.Recommendation,
                    row.FromLevel,
                    row.RecommendedToLevel,
                    row.IsException,
                    row.IssuedAt),
                cancellationToken);
        }

        return Result.Success(new BatchIssuePromotionRecommendationsResponse(issuedRows.Count, issuedRows));
    }
}
