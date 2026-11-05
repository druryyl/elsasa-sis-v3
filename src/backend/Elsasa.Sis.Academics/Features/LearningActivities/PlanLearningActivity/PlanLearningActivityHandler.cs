namespace Elsasa.Sis.Academics.Features.LearningActivities.PlanLearningActivity;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for scheduling a learning activity or homeroom session (ADR-004, ADR-005, TD-03, P3-S07).
/// </summary>
public sealed class PlanLearningActivityHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static PlanLearningActivityHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectUnitStatusSql = """
        SELECT id AS Id, status AS Status
        FROM academics.learning_units
        WHERE id = @LearningUnitId;
        """;

    private const string _insertActivitySql = """
        INSERT INTO academics.learning_activities (
            id,
            learning_unit_id,
            activity_type,
            title,
            description,
            section_label,
            activity_date,
            start_time,
            end_time,
            teacher_assignment_id,
            teacher_name,
            status,
            is_attendance_required,
            delivery_notes,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @LearningUnitId,
            @ActivityType,
            @Title,
            @Description,
            @SectionLabel,
            @ActivityDate,
            @StartTime,
            @EndTime,
            @TeacherAssignmentId,
            @TeacherName,
            @Status,
            @IsAttendanceRequired,
            NULL,
            NULL,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private sealed record UnitStatusRow(Guid Id, string Status);

    public async Task<Result<PlanLearningActivityResponse>> HandleAsync(
        PlanLearningActivityRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify learning unit exists and is active
        var unit = await connection.QuerySingleOrDefaultAsync<UnitStatusRow>(new CommandDefinition(
            _selectUnitStatusSql,
            new { request.LearningUnitId },
            cancellationToken: cancellationToken));

        if (unit is null)
        {
            return Result.Failure<PlanLearningActivityResponse>(
                DomainError.NotFound(
                    "Academic.LearningUnitNotFound",
                    $"Learning unit with ID '{request.LearningUnitId}' was not found."));
        }

        if (!string.Equals(unit.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<PlanLearningActivityResponse>(
                DomainError.Conflict(
                    "Academic.LearningUnitNotActive",
                    $"Cannot schedule activities for learning unit in '{unit.Status}' status. Unit must be active."));
        }

        // 2. Prepare activity entity
        var activityId = Guid.NewGuid();
        var teacherName = !string.IsNullOrWhiteSpace(request.TeacherName)
            ? request.TeacherName.Trim()
            : (currentUser.Username ?? "Unknown Teacher");
        var createdBy = currentUser.Username ?? "system";
        var createdAt = DateTimeOffset.UtcNow;
        const string status = "planned";

        await connection.ExecuteAsync(new CommandDefinition(
            _insertActivitySql,
            new
            {
                Id = activityId,
                request.LearningUnitId,
                ActivityType = request.ActivityType.ToLowerInvariant(),
                request.Title,
                request.Description,
                request.SectionLabel,
                request.ActivityDate,
                request.StartTime,
                request.EndTime,
                request.TeacherAssignmentId,
                TeacherName = teacherName,
                Status = status,
                request.IsAttendanceRequired,
                CreatedAt = createdAt,
                CreatedBy = createdBy
            },
            cancellationToken: cancellationToken));

        var response = new PlanLearningActivityResponse(
            activityId,
            request.LearningUnitId,
            request.ActivityType.ToLowerInvariant(),
            request.Title,
            request.Description,
            request.SectionLabel,
            request.ActivityDate,
            request.StartTime,
            request.EndTime,
            request.TeacherAssignmentId,
            teacherName,
            status,
            request.IsAttendanceRequired,
            null,
            null,
            createdAt,
            createdBy);

        return Result.Success(response);
    }
}
