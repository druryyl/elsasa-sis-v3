namespace Elsasa.Sis.Academics.Features.AcademicParticipations.BatchRegisterCohortParticipation;

using Dapper;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Contracts.Students;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler executing cohort batch academic participation registration (P2-S04, GAP-003, OQ-001, TD-02, UC-ACD-10).
/// Validates target learning units, performs synchronous student enrollment checks via <see cref="IStudentsModule"/>,
/// atomically inserts non-existing participations, and publishes post-commit domain events.
/// </summary>
public sealed class BatchRegisterCohortParticipationHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IStudentsModule studentsModule,
    IPublisher publisher)
{
    private const string _selectUnitsSql = """
        SELECT id AS Id, status AS Status
        FROM academics.learning_units
        WHERE id IN @LearningUnitIds;
        """;

    private const string _selectPlacedStudentsSql = """
        SELECT DISTINCT p.student_id
        FROM students.administrative_placements p
        WHERE p.is_current = 1
          AND (p.cohort = @SectionLabel OR p.homeroom = @SectionLabel);
        """;

    private const string _insertParticipationIfNotExistsSql = """
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
        SELECT
            @Id,
            @StudentId,
            @LearningUnitId,
            @AcademicPeriodId,
            @AcademicPeriodLabel,
            'standard',
            1,
            'assigned',
            @SectionLabel,
            'in_progress',
            @StartDate,
            @CreatedAt,
            @CreatedBy
        WHERE NOT EXISTS (
            SELECT 1
            FROM academics.academic_participations
            WHERE student_id = @StudentId
              AND learning_unit_id = @LearningUnitId
              AND academic_period_id = @AcademicPeriodId
              AND status = 'in_progress'
        );
        """;

    private sealed record UnitStatusRow(Guid Id, string Status);

    /// <summary>
    /// Executes validation and atomic cohort participation registration.
    /// </summary>
    /// <param name="request">Batch cohort registration request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count of registered and skipped records, or a domain error.</returns>
    public async Task<Result<BatchRegisterCohortParticipationResponse>> HandleAsync(
        BatchRegisterCohortParticipationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify learning units exist and are active
        var distinctUnitIds = request.LearningUnitIds.Distinct().ToArray();
        var units = (await connection.QueryAsync<UnitStatusRow>(new CommandDefinition(
            _selectUnitsSql,
            new { LearningUnitIds = distinctUnitIds },
            cancellationToken: cancellationToken))).ToList();

        if (units.Count != distinctUnitIds.Length)
        {
            return Result.Failure<BatchRegisterCohortParticipationResponse>(
                DomainError.NotFound(
                    "Academic.LearningUnitNotFound",
                    "One or more specified learning units were not found."));
        }

        if (units.Any(u => !string.Equals(u.Status, "active", StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure<BatchRegisterCohortParticipationResponse>(
                DomainError.Validation(
                    "Academic.LearningUnitNotActive",
                    "All learning units in cohort batch registration must be active."));
        }

        // 2. Resolve candidate students
        List<Guid> candidateStudentIds;
        if (request.StudentIds is not null && request.StudentIds.Count > 0)
        {
            candidateStudentIds = request.StudentIds.Distinct().ToList();
        }
        else
        {
            var placedStudents = await connection.QueryAsync<Guid>(new CommandDefinition(
                _selectPlacedStudentsSql,
                new { request.SectionLabel },
                cancellationToken: cancellationToken));

            candidateStudentIds = placedStudents.ToList();
        }

        // 3. Synchronously validate administrative enrollment via IStudentsModule
        var eligibleStudentIds = new List<Guid>();
        foreach (var studentId in candidateStudentIds)
        {
            var enrolledResult = await studentsModule.IsStudentEnrolledAsync(studentId, cancellationToken);
            if (enrolledResult.IsFailure || !enrolledResult.Value)
            {
                if (request.StudentIds is not null && request.StudentIds.Count > 0)
                {
                    return Result.Failure<BatchRegisterCohortParticipationResponse>(
                        DomainError.Validation(
                            "Academic.StudentNotEnrolled",
                            $"Student '{studentId}' is not actively enrolled."));
                }

                // If queried by section placement, skip unenrolled students
                continue;
            }

            eligibleStudentIds.Add(studentId);
        }

        if (eligibleStudentIds.Count == 0)
        {
            return Result.Success(new BatchRegisterCohortParticipationResponse(0, 0));
        }

        // 4. Atomically insert participations using INSERT ... WHERE NOT EXISTS
        var registeredCount = 0;
        var skippedCount = 0;
        var createdParticipations = new List<(Guid ParticipationId, Guid StudentId, Guid UnitId)>();
        var createdBy = currentUser.Username ?? currentUser.Email ?? "system";
        var createdAt = DateTimeOffset.UtcNow;

        await using (var transaction = await connection.BeginTransactionAsync(cancellationToken))
        {
            foreach (var studentId in eligibleStudentIds)
            {
                foreach (var unitId in distinctUnitIds)
                {
                    var participationId = Guid.NewGuid();
                    var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
                        _insertParticipationIfNotExistsSql,
                        new
                        {
                            Id = participationId,
                            StudentId = studentId,
                            LearningUnitId = unitId,
                            request.AcademicPeriodId,
                            request.AcademicPeriodLabel,
                            request.SectionLabel,
                            request.StartDate,
                            CreatedAt = createdAt,
                            CreatedBy = createdBy
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                    if (rowsAffected > 0)
                    {
                        registeredCount++;
                        createdParticipations.Add((participationId, studentId, unitId));
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
            }

            await transaction.CommitAsync(cancellationToken);
        }

        // 5. Publish post-commit domain events
        foreach (var (participationId, studentId, unitId) in createdParticipations)
        {
            await publisher.Publish(
                new AcademicParticipationRegisteredEvent(
                    participationId,
                    studentId,
                    unitId,
                    request.AcademicPeriodId,
                    request.SectionLabel,
                    "standard"),
                cancellationToken);
        }

        return Result.Success(new BatchRegisterCohortParticipationResponse(registeredCount, skippedCount));
    }
}
