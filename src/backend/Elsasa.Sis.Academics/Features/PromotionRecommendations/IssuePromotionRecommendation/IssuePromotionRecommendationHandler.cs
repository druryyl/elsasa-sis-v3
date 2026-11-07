namespace Elsasa.Sis.Academics.Features.PromotionRecommendations.IssuePromotionRecommendation;

using Dapper;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using FluentValidation;
using MediatR;

/// <summary>
/// Feature handler executing single progression recommendation certification (P3-S08, UC-ACD-17).
/// Persists record to <c>academics.promotion_recommendations</c> and publishes <see cref="PromotionRecommendationIssuedEvent"/> post-commit.
/// </summary>
public sealed class IssuePromotionRecommendationHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher,
    IValidator<IssuePromotionRecommendationCommand>? validator = null)
{
    static IssuePromotionRecommendationHandler()
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

    private const string _findStandingSql = """
        SELECT TOP (1) id
        FROM academics.academic_standings
        WHERE student_id = @StudentId AND academic_period_id = @AcademicPeriodId;
        """;

    /// <summary>
    /// Executes certification and persistence of a student's progression recommendation.
    /// </summary>
    /// <param name="command">Command payload containing recommendation attributes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing certified recommendation record or domain error.</returns>
    public async Task<Result<PromotionRecommendationDto>> HandleAsync(
        IssuePromotionRecommendationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (validator is not null)
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<PromotionRecommendationDto>(
                    DomainError.Validation(
                        "Academic.ValidationFailed",
                        string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))));
            }
        }

        var normalizedRecommendation = command.Recommendation.Trim().ToLowerInvariant();
        var authorizedBy = !string.IsNullOrWhiteSpace(currentUser.Username)
            ? currentUser.Username
            : (!string.IsNullOrWhiteSpace(currentUser.Email) ? currentUser.Email : "Academic Authority");
        var now = DateTimeOffset.UtcNow;
        var newId = Guid.NewGuid();

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // Resolve linked academic_standing_id if not supplied or empty
        Guid? standingId = command.AcademicStandingId;
        if (!standingId.HasValue || standingId.Value == Guid.Empty)
        {
            standingId = await connection.QueryFirstOrDefaultAsync<Guid?>(
                new CommandDefinition(
                    _findStandingSql,
                    new { command.StudentId, command.AcademicPeriodId },
                    cancellationToken: cancellationToken));
        }

        var row = await connection.QuerySingleAsync<PromotionRecommendationDto>(
            new CommandDefinition(
                _upsertRecommendationSql,
                new
                {
                    NewId = newId,
                    command.StudentId,
                    command.AcademicPeriodId,
                    Recommendation = normalizedRecommendation,
                    command.FromLevel,
                    command.RecommendedToLevel,
                    command.Conditions,
                    command.SupportingEvidence,
                    AcademicStandingId = standingId,
                    command.IsException,
                    command.ExceptionReason,
                    command.CommitteeReference,
                    AuthorizedBy = authorizedBy,
                    IssuedAt = now
                },
                cancellationToken: cancellationToken));

        // Post-commit: Publish PromotionRecommendationIssuedEvent
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

        return Result.Success(row);
    }
}
