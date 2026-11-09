namespace Elsasa.Sis.Academics.Features.AcademicStandings.BatchDetermineCohortStanding;

using Dapper;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using FluentValidation;
using MediatR;

/// <summary>
/// Feature handler executing cohort batch academic standing determination certification (P3-S07, UC-ACD-17, TD-03).
/// Atomically upserts standing records in a single database transaction and publishes <see cref="AcademicStandingDeterminedEvent"/> for each student post-commit.
/// </summary>
public sealed class BatchDetermineCohortStandingHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher,
    IValidator<BatchDetermineCohortStandingCommand>? validator = null)
{
    static BatchDetermineCohortStandingHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _upsertStandingSql = """
        MERGE INTO academics.academic_standings WITH (HOLDLOCK) AS target
        USING (
            SELECT
                @StudentId AS student_id,
                @AcademicPeriodId AS academic_period_id
        ) AS source
        ON target.student_id = source.student_id
           AND target.academic_period_id = source.academic_period_id
        WHEN MATCHED THEN
            UPDATE SET
                standing = @Standing,
                policy_reference = @PolicyReference,
                conditions = @Conditions,
                is_exception = @IsException,
                exception_reason = @ExceptionReason,
                committee_reference = @CommitteeReference,
                authorized_by = @AuthorizedBy,
                determined_at = @DeterminedAt,
                updated_at = @DeterminedAt,
                updated_by = @AuthorizedBy
        WHEN NOT MATCHED THEN
            INSERT (
                id,
                student_id,
                academic_period_id,
                standing,
                policy_reference,
                conditions,
                is_exception,
                exception_reason,
                committee_reference,
                authorized_by,
                determined_at,
                created_at,
                created_by
            )
            VALUES (
                @NewId,
                @StudentId,
                @AcademicPeriodId,
                @Standing,
                @PolicyReference,
                @Conditions,
                @IsException,
                @ExceptionReason,
                @CommitteeReference,
                @AuthorizedBy,
                @DeterminedAt,
                @DeterminedAt,
                @AuthorizedBy
            )
        OUTPUT
            inserted.id AS Id,
            inserted.student_id AS StudentId,
            inserted.academic_period_id AS AcademicPeriodId,
            inserted.standing AS Standing,
            inserted.policy_reference AS PolicyReference,
            inserted.conditions AS Conditions,
            inserted.is_exception AS IsException,
            inserted.exception_reason AS ExceptionReason,
            inserted.committee_reference AS CommitteeReference,
            inserted.authorized_by AS AuthorizedBy,
            inserted.determined_at AS DeterminedAt;
        """;

    /// <summary>
    /// Executes atomic transaction to validate and certify standing determinations for all students in the batch.
    /// </summary>
    /// <param name="command">Batch command containing target period and item list.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary response of processed determinations or domain error.</returns>
    public async Task<Result<BatchDetermineCohortStandingResponse>> HandleAsync(
        BatchDetermineCohortStandingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (validator is not null)
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<BatchDetermineCohortStandingResponse>(
                    DomainError.Validation(
                        "Academic.ValidationFailed",
                        string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))));
            }
        }

        if (command.Items is null || command.Items.Count == 0)
        {
            return Result.Failure<BatchDetermineCohortStandingResponse>(
                DomainError.Validation(
                    "Academic.EmptyBatch",
                    "At least one standing determination item is required."));
        }

        var authorizedBy = !string.IsNullOrWhiteSpace(currentUser.Username)
            ? currentUser.Username
            : (!string.IsNullOrWhiteSpace(currentUser.Email) ? currentUser.Email : "Academic Authority");
        var now = DateTimeOffset.UtcNow;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var determinedRows = new List<StudentAcademicStandingDto>(command.Items.Count);

        try
        {
            foreach (var item in command.Items)
            {
                var normalizedStanding = item.Standing.Trim().ToLowerInvariant();

                // Explicit item-level exception constraints enforcement
                if (item.IsException)
                {
                    if (string.IsNullOrWhiteSpace(item.ExceptionReason) || item.ExceptionReason.Trim().Length < 5)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return Result.Failure<BatchDetermineCohortStandingResponse>(
                            DomainError.Validation(
                                "Academic.ExceptionReasonInvalid",
                                $"Exception reason is mandatory (minimum 5 characters) for student '{item.StudentId}' when marked as an exception."));
                    }

                    if (string.IsNullOrWhiteSpace(item.CommitteeReference) || item.CommitteeReference.Trim().Length < 5)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return Result.Failure<BatchDetermineCohortStandingResponse>(
                            DomainError.Validation(
                                "Academic.CommitteeReferenceInvalid",
                                $"Committee reference is mandatory (minimum 5 characters) for student '{item.StudentId}' when marked as an exception."));
                    }
                }

                if (string.Equals(normalizedStanding, "conditional_standing", StringComparison.OrdinalIgnoreCase) &&
                    string.IsNullOrWhiteSpace(item.Conditions))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Failure<BatchDetermineCohortStandingResponse>(
                        DomainError.Validation(
                            "Academic.ConditionsRequired",
                            $"Conditions are mandatory for student '{item.StudentId}' when standing is 'conditional_standing'."));
                }

                var row = await connection.QuerySingleAsync<StudentAcademicStandingDto>(
                    new CommandDefinition(
                        _upsertStandingSql,
                        new
                        {
                            NewId = Guid.NewGuid(),
                            item.StudentId,
                            command.AcademicPeriodId,
                            Standing = normalizedStanding,
                            item.PolicyReference,
                            item.Conditions,
                            item.IsException,
                            item.ExceptionReason,
                            item.CommitteeReference,
                            AuthorizedBy = authorizedBy,
                            DeterminedAt = now
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                determinedRows.Add(row);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        // Post-commit: Publish AcademicStandingDeterminedEvent for each student
        foreach (var row in determinedRows)
        {
            await publisher.Publish(
                new AcademicStandingDeterminedEvent(
                    row.Id,
                    row.StudentId,
                    row.AcademicPeriodId,
                    row.Standing,
                    row.IsException,
                    row.DeterminedAt),
                cancellationToken);
        }

        return Result.Success(new BatchDetermineCohortStandingResponse(determinedRows.Count, determinedRows));
    }
}
