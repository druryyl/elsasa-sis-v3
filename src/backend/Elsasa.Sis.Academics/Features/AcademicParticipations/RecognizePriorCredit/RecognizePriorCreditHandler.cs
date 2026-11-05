namespace Elsasa.Sis.Academics.Features.AcademicParticipations.RecognizePriorCredit;

using Dapper;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Contracts.Students;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler executing transfer credit and prior learning recognition (P2-S05, GAP-08, UC-ACD-11).
/// Enforces synchronous student enrollment check, authority stamping, completion date invariants,
/// and publishes both AcademicParticipationRegisteredEvent and ParticipationConcludedEvent post-commit.
/// </summary>
public sealed class RecognizePriorCreditHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IStudentsModule studentsModule,
    IPublisher publisher)
{
    static RecognizePriorCreditHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectUnitSql = """
        SELECT id AS Id
        FROM academics.learning_units
        WHERE id = @LearningUnitId;
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
            end_date,
            completion_date,
            source_institution,
            external_grade_or_credit,
            recognition_basis,
            approved_by,
            student_learning_plan_id,
            notes,
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
            1,
            @Source,
            NULL,
            'completed',
            @StartDate,
            @CompletionDate,
            @CompletionDate,
            @SourceInstitution,
            @ExternalGradeOrCredit,
            @RecognitionBasis,
            @ApprovedBy,
            @StudentLearningPlanId,
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    /// <summary>
    /// Executes validation, insertion, and domain event publication for prior credit recognition.
    /// </summary>
    /// <param name="request">Prior credit recognition request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created participation details or a domain error.</returns>
    public async Task<Result<RecognizePriorCreditResponse>> HandleAsync(
        RecognizePriorCreditRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Synchronously validate student enrollment via IStudentsModule
        var enrolledResult = await studentsModule.IsStudentEnrolledAsync(request.StudentId, cancellationToken);
        if (enrolledResult.IsFailure || !enrolledResult.Value)
        {
            return Result.Failure<RecognizePriorCreditResponse>(
                DomainError.Validation(
                    "Academic.StudentNotEnrolled",
                    "Student is not actively enrolled."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 2. Validate learning unit exists
        var unitExists = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            _selectUnitSql,
            new { request.LearningUnitId },
            cancellationToken: cancellationToken));

        if (!unitExists.HasValue)
        {
            return Result.Failure<RecognizePriorCreditResponse>(
                DomainError.NotFound(
                    "Academic.LearningUnitNotFound",
                    $"Learning unit with ID '{request.LearningUnitId}' was not found."));
        }

        // 3. Authenticated authority identity stamp
        var approvedBy = currentUser.Username ?? currentUser.Email ?? "system";
        var participationId = Guid.NewGuid();
        var normalizedType = request.ParticipationType.Trim().ToLowerInvariant();
        var source = request.StudentLearningPlanId.HasValue ? "learning_plan" : "assigned";
        var now = DateTimeOffset.UtcNow;

        // 4. Insert completed participation row
        await connection.ExecuteAsync(new CommandDefinition(
            _insertParticipationSql,
            new
            {
                Id = participationId,
                request.StudentId,
                request.LearningUnitId,
                request.AcademicPeriodId,
                AcademicPeriodLabel = request.AcademicPeriodLabel.Trim(),
                ParticipationType = normalizedType,
                Source = source,
                request.StartDate,
                request.CompletionDate,
                SourceInstitution = string.IsNullOrWhiteSpace(request.SourceInstitution) ? null : request.SourceInstitution.Trim(),
                ExternalGradeOrCredit = string.IsNullOrWhiteSpace(request.ExternalGradeOrCredit) ? null : request.ExternalGradeOrCredit.Trim(),
                RecognitionBasis = string.IsNullOrWhiteSpace(request.RecognitionBasis) ? null : request.RecognitionBasis.Trim(),
                ApprovedBy = approvedBy,
                request.StudentLearningPlanId,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                CreatedAt = now,
                CreatedBy = approvedBy
            },
            cancellationToken: cancellationToken));

        // 5. Publish post-commit domain events (ADR-003, TD-08)
        await publisher.Publish(
            new AcademicParticipationRegisteredEvent(
                participationId,
                request.StudentId,
                request.LearningUnitId,
                request.AcademicPeriodId,
                null,
                normalizedType),
            cancellationToken);

        await publisher.Publish(
            new ParticipationConcludedEvent(
                participationId,
                request.StudentId,
                request.LearningUnitId,
                request.CompletionDate),
            cancellationToken);

        return Result.Success(new RecognizePriorCreditResponse(
            participationId,
            request.StudentId,
            request.LearningUnitId,
            request.AcademicPeriodId,
            request.AcademicPeriodLabel.Trim(),
            normalizedType,
            1,
            "completed",
            request.StartDate,
            request.CompletionDate,
            request.CompletionDate,
            request.SourceInstitution,
            request.ExternalGradeOrCredit,
            request.RecognitionBasis,
            approvedBy,
            request.StudentLearningPlanId,
            request.Notes,
            now,
            approvedBy));
    }
}
