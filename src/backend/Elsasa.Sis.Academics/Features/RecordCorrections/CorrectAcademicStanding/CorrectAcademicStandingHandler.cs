namespace Elsasa.Sis.Academics.Features.RecordCorrections.CorrectAcademicStanding;

using System.Text.Json;
using Dapper;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using FluentValidation;
using MediatR;

/// <summary>
/// Feature handler executing auditable in-place corrections of approved academic standings (P5-S12, UC-ACD-15, TD-05).
/// Enforces reason justification (minimum 10 characters), serializes pre- and post-correction states to
/// <c>academics.academic_record_corrections</c> within a transaction, and publishes <see cref="AcademicStandingCorrectedEvent"/> post-commit.
/// </summary>
public sealed class CorrectAcademicStandingHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher,
    IValidator<CorrectAcademicStandingCommand>? validator = null)
{
    static CorrectAcademicStandingHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectStandingSql = """
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
        FROM academics.academic_standings WITH (UPDLOCK, ROWLOCK)
        WHERE id = @Id;
        """;

    private const string _updateStandingSql = """
        UPDATE academics.academic_standings
        SET standing = @Standing,
            policy_reference = @PolicyReference,
            conditions = @Conditions,
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
            'academic_standing',
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
    /// Executes transaction for correcting an approved academic standing determination with immutable audit recording.
    /// </summary>
    /// <param name="command">Correction command payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the updated standing determination DTO or domain error.</returns>
    public async Task<Result<StudentAcademicStandingDto>> HandleAsync(
        CorrectAcademicStandingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (validator is not null)
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<StudentAcademicStandingDto>(
                    DomainError.Validation(
                        "Academic.ValidationFailed",
                        string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))));
            }
        }

        var normalizedStanding = command.Standing.Trim().ToLowerInvariant();
        var authorizerIdentity = !string.IsNullOrWhiteSpace(currentUser.Username)
            ? currentUser.Username
            : (!string.IsNullOrWhiteSpace(currentUser.Email) ? currentUser.Email : "Academic Authority");
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        StudentAcademicStandingDto? existing;
        try
        {
            existing = await connection.QuerySingleOrDefaultAsync<StudentAcademicStandingDto>(
                new CommandDefinition(
                    _selectStandingSql,
                    new { command.Id },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            if (existing is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<StudentAcademicStandingDto>(
                    DomainError.NotFound(
                        "Academics.AcademicStandings.NotFound",
                        $"Academic standing '{command.Id}' was not found."));
            }

            // Serialize existing state into JSON
            var originalSnapshot = new
            {
                id = existing.Id,
                student_id = existing.StudentId,
                academic_period_id = existing.AcademicPeriodId,
                standing = existing.Standing,
                policy_reference = existing.PolicyReference,
                conditions = existing.Conditions,
                is_exception = existing.IsException,
                exception_reason = existing.ExceptionReason,
                committee_reference = existing.CommitteeReference,
                authorized_by = existing.AuthorizedBy,
                determined_at = existing.DeterminedAt
            };
            var originalValueJson = JsonSerializer.Serialize(originalSnapshot);

            // Serialize corrected state into JSON
            var correctedSnapshot = new
            {
                id = existing.Id,
                student_id = existing.StudentId,
                academic_period_id = existing.AcademicPeriodId,
                standing = normalizedStanding,
                policy_reference = command.PolicyReference.Trim(),
                conditions = command.Conditions?.Trim(),
                is_exception = command.IsException,
                exception_reason = command.IsException ? command.ExceptionReason?.Trim() : null,
                committee_reference = command.IsException ? command.CommitteeReference?.Trim() : null,
                authorized_by = authorizerIdentity,
                determined_at = existing.DeterminedAt
            };
            var correctedValueJson = JsonSerializer.Serialize(correctedSnapshot);

            // Update academics.academic_standings
            await connection.ExecuteAsync(
                new CommandDefinition(
                    _updateStandingSql,
                    new
                    {
                        command.Id,
                        Standing = normalizedStanding,
                        PolicyReference = command.PolicyReference.Trim(),
                        Conditions = command.Conditions?.Trim(),
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

        // Post-commit: Publish AcademicStandingCorrectedEvent
        await publisher.Publish(
            new AcademicStandingCorrectedEvent(
                existing.Id,
                existing.StudentId,
                existing.AcademicPeriodId,
                existing.Standing,
                normalizedStanding,
                command.Reason.Trim(),
                now),
            cancellationToken);

        var updatedDto = new StudentAcademicStandingDto(
            existing.Id,
            existing.StudentId,
            existing.AcademicPeriodId,
            normalizedStanding,
            command.PolicyReference.Trim(),
            command.Conditions?.Trim(),
            command.IsException,
            command.IsException ? command.ExceptionReason?.Trim() : null,
            command.IsException ? command.CommitteeReference?.Trim() : null,
            authorizerIdentity,
            existing.DeterminedAt);

        return Result.Success(updatedDto);
    }
}
