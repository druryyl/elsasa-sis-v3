namespace Elsasa.Sis.Academics.Features.AcademicStandings.DetermineAcademicStanding;

using Dapper;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using FluentValidation;
using MediatR;

/// <summary>
/// Feature handler executing single academic standing determination certification (P3-S07, UC-ACD-17).
/// Persists record to <c>academics.academic_standings</c> and publishes <see cref="AcademicStandingDeterminedEvent"/> post-commit.
/// </summary>
public sealed class DetermineAcademicStandingHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher,
    IValidator<DetermineAcademicStandingCommand>? validator = null)
{
    static DetermineAcademicStandingHandler()
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
    /// Executes certification and persistence of a student's academic standing determination.
    /// </summary>
    /// <param name="command">Command payload containing standing attributes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing certified standing record or domain error.</returns>
    public async Task<Result<StudentAcademicStandingDto>> HandleAsync(
        DetermineAcademicStandingCommand command,
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
        var authorizedBy = !string.IsNullOrWhiteSpace(currentUser.Username)
            ? currentUser.Username
            : (!string.IsNullOrWhiteSpace(currentUser.Email) ? currentUser.Email : "Academic Authority");
        var now = DateTimeOffset.UtcNow;
        var newId = Guid.NewGuid();

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleAsync<StudentAcademicStandingDto>(
            new CommandDefinition(
                _upsertStandingSql,
                new
                {
                    NewId = newId,
                    command.StudentId,
                    command.AcademicPeriodId,
                    Standing = normalizedStanding,
                    command.PolicyReference,
                    command.Conditions,
                    command.IsException,
                    command.ExceptionReason,
                    command.CommitteeReference,
                    AuthorizedBy = authorizedBy,
                    DeterminedAt = now
                },
                cancellationToken: cancellationToken));

        // Post-commit: Publish AcademicStandingDeterminedEvent
        await publisher.Publish(
            new AcademicStandingDeterminedEvent(
                row.Id,
                row.StudentId,
                row.AcademicPeriodId,
                row.Standing,
                row.IsException,
                row.DeterminedAt),
            cancellationToken);

        return Result.Success(row);
    }
}
