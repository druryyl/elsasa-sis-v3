namespace Elsasa.Sis.Academics.Features.RecordCorrections.CorrectPromotionRecommendation;

using System.Text.Json;
using Dapper;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using FluentValidation;
using MediatR;

/// <summary>
/// Feature handler executing auditable in-place corrections of approved promotion recommendations (P5-S12, UC-ACD-15, TD-05).
/// Enforces reason justification (minimum 10 characters), serializes pre- and post-correction states to
/// <c>academics.academic_record_corrections</c> within a transaction, and publishes <see cref="PromotionRecommendationCorrectedEvent"/> post-commit.
/// </summary>
public sealed class CorrectPromotionRecommendationHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher,
    IValidator<CorrectPromotionRecommendationCommand>? validator = null)
{
    static CorrectPromotionRecommendationHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectRecommendationSql = """
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
        FROM academics.promotion_recommendations WITH (UPDLOCK, ROWLOCK)
        WHERE id = @Id;
        """;

    private const string _updateRecommendationSql = """
        UPDATE academics.promotion_recommendations
        SET recommendation = @Recommendation,
            from_level = @FromLevel,
            recommended_to_level = @RecommendedToLevel,
            conditions = @Conditions,
            supporting_evidence = @SupportingEvidence,
            is_exception = @IsException,
            exception_reason = @ExceptionReason,
            committee_reference = @CommitteeReference,
            authorized_by = @AuthorizedBy,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
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
            'promotion_recommendation',
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

    /// <summary>
    /// Executes transaction for correcting an approved promotion recommendation with immutable audit recording.
    /// </summary>
    /// <param name="command">Correction command payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the updated promotion recommendation DTO or domain error.</returns>
    public async Task<Result<PromotionRecommendationDto>> HandleAsync(
        CorrectPromotionRecommendationCommand command,
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
        var authorizerIdentity = !string.IsNullOrWhiteSpace(currentUser.Username)
            ? currentUser.Username
            : (!string.IsNullOrWhiteSpace(currentUser.Email) ? currentUser.Email : "Academic Authority");
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        PromotionRecommendationDto? existing;
        try
        {
            existing = await connection.QuerySingleOrDefaultAsync<PromotionRecommendationDto>(
                new CommandDefinition(
                    _selectRecommendationSql,
                    new { command.Id },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            if (existing is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<PromotionRecommendationDto>(
                    DomainError.NotFound(
                        "Academics.PromotionRecommendations.NotFound",
                        $"Promotion recommendation '{command.Id}' was not found."));
            }

            // Serialize existing state into JSON
            var originalSnapshot = new
            {
                id = existing.Id,
                student_id = existing.StudentId,
                academic_period_id = existing.AcademicPeriodId,
                recommendation = existing.Recommendation,
                from_level = existing.FromLevel,
                recommended_to_level = existing.RecommendedToLevel,
                conditions = existing.Conditions,
                supporting_evidence = existing.SupportingEvidence,
                academic_standing_id = existing.AcademicStandingId,
                is_exception = existing.IsException,
                exception_reason = existing.ExceptionReason,
                committee_reference = existing.CommitteeReference,
                authorized_by = existing.AuthorizedBy,
                issued_at = existing.IssuedAt
            };
            var originalValueJson = JsonSerializer.Serialize(originalSnapshot);

            // Serialize corrected state into JSON
            var correctedSnapshot = new
            {
                id = existing.Id,
                student_id = existing.StudentId,
                academic_period_id = existing.AcademicPeriodId,
                recommendation = normalizedRecommendation,
                from_level = command.FromLevel.Trim(),
                recommended_to_level = command.RecommendedToLevel?.Trim(),
                conditions = command.Conditions?.Trim(),
                supporting_evidence = command.SupportingEvidence.Trim(),
                academic_standing_id = existing.AcademicStandingId,
                is_exception = command.IsException,
                exception_reason = command.IsException ? command.ExceptionReason?.Trim() : null,
                committee_reference = command.IsException ? command.CommitteeReference?.Trim() : null,
                authorized_by = authorizerIdentity,
                issued_at = existing.IssuedAt
            };
            var correctedValueJson = JsonSerializer.Serialize(correctedSnapshot);

            // Update academics.promotion_recommendations
            await connection.ExecuteAsync(
                new CommandDefinition(
                    _updateRecommendationSql,
                    new
                    {
                        command.Id,
                        Recommendation = normalizedRecommendation,
                        FromLevel = command.FromLevel.Trim(),
                        RecommendedToLevel = command.RecommendedToLevel?.Trim(),
                        Conditions = command.Conditions?.Trim(),
                        SupportingEvidence = command.SupportingEvidence.Trim(),
                        command.IsException,
                        ExceptionReason = command.IsException ? command.ExceptionReason?.Trim() : null,
                        CommitteeReference = command.IsException ? command.CommitteeReference?.Trim() : null,
                        AuthorizedBy = authorizerIdentity,
                        UpdatedAt = now,
                        UpdatedBy = authorizerIdentity
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            // Insert audit record into academics.academic_record_corrections
            var auditId = Guid.NewGuid();
            await connection.ExecuteAsync(
                new CommandDefinition(
                    _insertCorrectionAuditSql,
                    new
                    {
                        Id = auditId,
                        RecordId = command.Id,
                        OriginalValue = originalValueJson,
                        CorrectedValue = correctedValueJson,
                        Reason = command.Reason.Trim(),
                        Evidence = (string?)null,
                        CorrectedBy = authorizerIdentity,
                        CorrectedDate = today,
                        IsMaterial = true,
                        Notes = (string?)null,
                        CreatedAt = now,
                        CreatedBy = authorizerIdentity
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        // Post-commit: Publish PromotionRecommendationCorrectedEvent
        await publisher.Publish(
            new PromotionRecommendationCorrectedEvent(
                existing.Id,
                existing.StudentId,
                existing.AcademicPeriodId,
                existing.Recommendation,
                normalizedRecommendation,
                command.Reason.Trim(),
                now),
            cancellationToken);

        var updatedDto = new PromotionRecommendationDto(
            existing.Id,
            existing.StudentId,
            existing.AcademicPeriodId,
            normalizedRecommendation,
            command.FromLevel.Trim(),
            command.RecommendedToLevel?.Trim(),
            command.Conditions?.Trim(),
            command.SupportingEvidence.Trim(),
            existing.AcademicStandingId,
            command.IsException,
            command.IsException ? command.ExceptionReason?.Trim() : null,
            command.IsException ? command.CommitteeReference?.Trim() : null,
            authorizerIdentity,
            existing.IssuedAt);

        return Result.Success(updatedDto);
    }
}
