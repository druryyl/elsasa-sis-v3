namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.CreateStudentLearningPlan;

using Dapper;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Contracts.Operations;
using Elsasa.Sis.Contracts.Students;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler formulating an initial draft student learning plan (v1) (UC-ACD-09, P2-S04).
/// Validates student enrollment, active operational period, and ensures no active plan currently exists.
/// </summary>
public sealed class CreateStudentLearningPlanHandler(
    ISqlConnectionFactory connectionFactory,
    IStudentsModule studentsModule,
    IOperationsCalendarQueryService calendarQueryService,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    private const string _checkActivePlanSql = """
        SELECT id
        FROM academics.student_learning_plans
        WHERE student_id = @StudentId
          AND academic_period_id = @AcademicPeriodId
          AND status = 'active';
        """;

    private const string _insertPlanSql = """
        INSERT INTO academics.student_learning_plans (
            id,
            student_id,
            academic_period_id,
            academic_period_label,
            status,
            version,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @StudentId,
            @AcademicPeriodId,
            @AcademicPeriodLabel,
            'draft',
            1,
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    /// <summary>
    /// Executes validation and formulation of an initial draft student learning plan.
    /// </summary>
    /// <param name="request">The formulation request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created student learning plan response or domain error.</returns>
    public async Task<Result<CreateStudentLearningPlanResponse>> HandleAsync(
        CreateStudentLearningPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.StudentId == Guid.Empty)
        {
            return Result.Failure<CreateStudentLearningPlanResponse>(
                DomainError.Validation("Academics.InvalidStudentId", "Student ID cannot be empty."));
        }

        if (request.AcademicPeriodId == Guid.Empty)
        {
            return Result.Failure<CreateStudentLearningPlanResponse>(
                DomainError.Validation("Academics.InvalidAcademicPeriodId", "Academic period ID cannot be empty."));
        }

        // 1. Synchronously validate student enrollment via IStudentsModule
        var enrollmentResult = await studentsModule.IsStudentEnrolledAsync(request.StudentId, cancellationToken);
        if (enrollmentResult.IsFailure || !enrollmentResult.Value)
        {
            return Result.Failure<CreateStudentLearningPlanResponse>(
                DomainError.Validation(
                    "Academic.StudentNotEnrolled",
                    "Student is not actively enrolled."));
        }

        // 2. Synchronously validate operational academic period via IOperationsCalendarQueryService
        var periodResult = await calendarQueryService.GetOperationalPeriodByIdAsync(request.AcademicPeriodId, cancellationToken);
        if (!periodResult.IsSuccess || periodResult.Value is null)
        {
            return Result.Failure<CreateStudentLearningPlanResponse>(
                DomainError.NotFound(
                    "Academics.AcademicPeriodNotFound",
                    $"Operational academic period with ID '{request.AcademicPeriodId}' was not found."));
        }

        var period = periodResult.Value;
        var isPeriodActive = string.Equals(period.Status, "active", StringComparison.OrdinalIgnoreCase)
            || string.Equals(period.Status, "approved", StringComparison.OrdinalIgnoreCase);

        if (!isPeriodActive)
        {
            return Result.Failure<CreateStudentLearningPlanResponse>(
                DomainError.Validation(
                    "Academics.AcademicPeriodNotActive",
                    $"Academic period '{period.Name}' is not active or approved (current status: '{period.Status}')."));
        }

        // 3. Verify no active plan currently exists for this student and period
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var existingActivePlanId = await connection.QueryFirstOrDefaultAsync<Guid?>(
            new CommandDefinition(
                _checkActivePlanSql,
                new { request.StudentId, request.AcademicPeriodId },
                cancellationToken: cancellationToken));

        if (existingActivePlanId.HasValue && existingActivePlanId.Value != Guid.Empty)
        {
            return Result.Failure<CreateStudentLearningPlanResponse>(
                DomainError.Conflict(
                    "Academics.ActiveLearningPlanAlreadyExists",
                    $"An active student learning plan already exists for student '{request.StudentId}' in academic period '{request.AcademicPeriodId}'."));
        }

        // 4. Insert new draft plan record (v1)
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var createdBy = currentUser.Username ?? currentUser.Email ?? "system";
        var periodLabel = !string.IsNullOrWhiteSpace(request.AcademicPeriodLabel)
            ? request.AcademicPeriodLabel.Trim()
            : (!string.IsNullOrWhiteSpace(period.Name) ? period.Name : period.PeriodCode);

        await connection.ExecuteAsync(new CommandDefinition(
            _insertPlanSql,
            new
            {
                Id = id,
                request.StudentId,
                request.AcademicPeriodId,
                AcademicPeriodLabel = periodLabel,
                request.Notes,
                CreatedAt = now,
                CreatedBy = createdBy
            },
            cancellationToken: cancellationToken));

        // 5. Publish post-commit domain event (ADR-003, UC-ACD-09)
        await publisher.Publish(
            new StudentLearningPlanCreatedEvent(
                id,
                request.StudentId,
                request.AcademicPeriodId,
                1,
                createdBy,
                now),
            cancellationToken);

        return Result.Success(new CreateStudentLearningPlanResponse(
            id,
            request.StudentId,
            request.AcademicPeriodId,
            periodLabel,
            "draft",
            1,
            request.Notes,
            now,
            createdBy));
    }
}
