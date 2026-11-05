namespace Elsasa.Sis.Academics.Features.AcademicParticipations.RegisterParticipation;

using Dapper;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Contracts.Students;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler executing single student academic participation registration (P2-S04, TD-02, UC-ACD-10).
/// Enforces synchronous student enrollment check, unit existence and active status, repeat attempt calculation,
/// filtered active participation unique constraint trapping, and post-commit domain event publication.
/// </summary>
public sealed class RegisterParticipationHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IStudentsModule studentsModule,
    IPublisher publisher)
{
    private const string _selectUnitSql = """
        SELECT id AS Id, status AS Status
        FROM academics.learning_units
        WHERE id = @LearningUnitId;
        """;

    private const string _selectPriorParticipationsSql = """
        SELECT id AS Id, attempt_number AS AttemptNumber, status AS Status
        FROM academics.academic_participations
        WHERE student_id = @StudentId AND learning_unit_id = @LearningUnitId;
        """;

    private const string _checkActiveParticipationSql = """
        SELECT COUNT(1)
        FROM academics.academic_participations
        WHERE student_id = @StudentId
          AND learning_unit_id = @LearningUnitId
          AND academic_period_id = @AcademicPeriodId
          AND status = 'in_progress';
        """;

    private const string _insertParticipationSql = """
        INSERT INTO academics.academic_participations (
            id,
            student_id,
            learning_unit_id,
            academic_period_id,
            academic_period_label,
            participation_type,
            attempt_number,
            source,
            section_label,
            status,
            start_date,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @StudentId,
            @LearningUnitId,
            @AcademicPeriodId,
            @AcademicPeriodLabel,
            @ParticipationType,
            @AttemptNumber,
            @Source,
            @SectionLabel,
            'in_progress',
            @StartDate,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private sealed record UnitStatusRow(Guid Id, string Status);
    private sealed record PriorParticipationRow(Guid Id, int AttemptNumber, string Status);

    /// <summary>
    /// Executes validation and single student participation registration.
    /// </summary>
    /// <param name="request">Registration request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created participation details, or a domain error.</returns>
    public async Task<Result<RegisterParticipationResponse>> HandleAsync(
        RegisterParticipationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Synchronously validate student enrollment via IStudentsModule
        var enrolledResult = await studentsModule.IsStudentEnrolledAsync(request.StudentId, cancellationToken);
        if (enrolledResult.IsFailure || !enrolledResult.Value)
        {
            return Result.Failure<RegisterParticipationResponse>(
                DomainError.Validation(
                    "Academic.StudentNotEnrolled",
                    "Student is not actively enrolled."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 2. Validate learning unit exists and is active
        var unit = await connection.QuerySingleOrDefaultAsync<UnitStatusRow>(new CommandDefinition(
            _selectUnitSql,
            new { request.LearningUnitId },
            cancellationToken: cancellationToken));

        if (unit is null)
        {
            return Result.Failure<RegisterParticipationResponse>(
                DomainError.NotFound(
                    "Academic.LearningUnitNotFound",
                    $"Learning unit with ID '{request.LearningUnitId}' was not found."));
        }

        if (!string.Equals(unit.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<RegisterParticipationResponse>(
                DomainError.Validation(
                    "Academic.LearningUnitNotActive",
                    "Learning unit is not active."));
        }

        var normalizedParticipationType = request.ParticipationType.Trim().ToLowerInvariant();
        var normalizedSource = request.Source.Trim().ToLowerInvariant();

        // 3. Repeat attempt calculation & prior participation terminal state invariant
        var attemptNumber = 1;
        if (string.Equals(normalizedParticipationType, "repeat", StringComparison.OrdinalIgnoreCase))
        {
            var priorParticipations = (await connection.QueryAsync<PriorParticipationRow>(new CommandDefinition(
                _selectPriorParticipationsSql,
                new { request.StudentId, request.LearningUnitId },
                cancellationToken: cancellationToken))).ToList();

            var hasNonTerminal = priorParticipations.Any(p =>
                !string.Equals(p.Status, "completed", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(p.Status, "withdrawn", StringComparison.OrdinalIgnoreCase));

            if (hasNonTerminal)
            {
                return Result.Failure<RegisterParticipationResponse>(
                    DomainError.Conflict(
                        "Academic.NonTerminalPriorParticipation",
                        "All prior participations in this learning unit must be terminal (completed or withdrawn) before registering a repeat attempt."));
            }

            var maxPriorAttempt = priorParticipations.Count > 0
                ? priorParticipations.Max(p => p.AttemptNumber)
                : 0;

            attemptNumber = maxPriorAttempt + 1;
        }

        // 4. Pre-check filtered unique index: at most one active participation per student/unit/period
        var activeConflict = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _checkActiveParticipationSql,
            new { request.StudentId, request.LearningUnitId, request.AcademicPeriodId },
            cancellationToken: cancellationToken));

        if (activeConflict > 0)
        {
            return Result.Failure<RegisterParticipationResponse>(
                DomainError.Conflict(
                    "Academic.ActiveParticipationConflict",
                    "Student already has an active participation in this learning unit for this period."));
        }

        // 5. Persist participation with filtered unique index violation trapping
        var participationId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var createdBy = currentUser.Username ?? currentUser.Email ?? "system";

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                _insertParticipationSql,
                new
                {
                    Id = participationId,
                    request.StudentId,
                    request.LearningUnitId,
                    request.AcademicPeriodId,
                    request.AcademicPeriodLabel,
                    ParticipationType = normalizedParticipationType,
                    AttemptNumber = attemptNumber,
                    Source = normalizedSource,
                    request.SectionLabel,
                    request.StartDate,
                    CreatedAt = createdAt,
                    CreatedBy = createdBy
                },
                cancellationToken: cancellationToken));
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627 || ex.Message.Contains("UQ_academics_participations_active", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<RegisterParticipationResponse>(
                DomainError.Conflict(
                    "Academic.ActiveParticipationConflict",
                    "Student already has an active participation in this learning unit for this period."));
        }

        // 6. Post-commit domain event publication
        await publisher.Publish(
            new AcademicParticipationRegisteredEvent(
                participationId,
                request.StudentId,
                request.LearningUnitId,
                request.AcademicPeriodId,
                request.SectionLabel,
                normalizedParticipationType),
            cancellationToken);

        return Result.Success(new RegisterParticipationResponse(
            participationId,
            request.StudentId,
            request.LearningUnitId,
            request.AcademicPeriodId,
            request.AcademicPeriodLabel,
            normalizedParticipationType,
            attemptNumber,
            normalizedSource,
            request.SectionLabel,
            "in_progress",
            request.StartDate,
            createdAt,
            createdBy));
    }
}
