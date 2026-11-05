namespace Elsasa.Sis.Academics.Features.LearningActivities.GetActivityById;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying detailed activity info including teacher context, attendance summary, and issued assignments (ADR-004, ADR-005, P3-S07).
/// </summary>
public sealed class GetActivityByIdHandler(ISqlConnectionFactory connectionFactory)
{
    static GetActivityByIdHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectActivitySql = """
        SELECT
            a.id AS Id,
            a.learning_unit_id AS LearningUnitId,
            u.code AS LearningUnitCode,
            u.name AS LearningUnitName,
            a.activity_type AS ActivityType,
            a.title AS Title,
            a.description AS Description,
            a.section_label AS SectionLabel,
            a.activity_date AS ActivityDate,
            a.start_time AS StartTime,
            a.end_time AS EndTime,
            a.teacher_assignment_id AS TeacherAssignmentId,
            a.teacher_name AS TeacherName,
            a.status AS Status,
            a.is_attendance_required AS IsAttendanceRequired,
            a.delivery_notes AS DeliveryNotes,
            a.notes AS Notes,
            a.created_at AS CreatedAt,
            a.created_by AS CreatedBy,
            a.updated_at AS UpdatedAt,
            a.updated_by AS UpdatedBy
        FROM academics.learning_activities a
        INNER JOIN academics.learning_units u ON u.id = a.learning_unit_id
        WHERE a.id = @Id;
        """;

    private const string _selectAttendanceSummarySql = """
        SELECT
            COUNT(1) AS TotalCount,
            ISNULL(SUM(CASE WHEN status = 'present' THEN 1 ELSE 0 END), 0) AS PresentCount,
            ISNULL(SUM(CASE WHEN status = 'absent' THEN 1 ELSE 0 END), 0) AS AbsentCount,
            ISNULL(SUM(CASE WHEN status = 'late' THEN 1 ELSE 0 END), 0) AS LateCount,
            ISNULL(SUM(CASE WHEN status = 'excused_absence' THEN 1 ELSE 0 END), 0) AS ExcusedCount,
            ISNULL(SUM(CASE WHEN status = 'partial' THEN 1 ELSE 0 END), 0) AS PartialCount
        FROM academics.attendance_records
        WHERE learning_activity_id = @Id;
        """;

    private const string _selectAssignmentsSql = """
        SELECT
            id AS Id,
            title AS Title,
            description AS Description,
            release_date AS ReleaseDate,
            due_date AS DueDate,
            max_score AS MaxScore,
            weight_percentage AS WeightPercentage,
            status AS Status,
            created_at AS CreatedAt
        FROM academics.assignments
        WHERE learning_activity_id = @Id
        ORDER BY release_date ASC, title ASC;
        """;

    private sealed record ActivityDetailRow(
        Guid Id,
        Guid LearningUnitId,
        string LearningUnitCode,
        string LearningUnitName,
        string ActivityType,
        string Title,
        string? Description,
        string? SectionLabel,
        DateOnly ActivityDate,
        TimeOnly? StartTime,
        TimeOnly? EndTime,
        Guid? TeacherAssignmentId,
        string? TeacherName,
        string Status,
        bool IsAttendanceRequired,
        string? DeliveryNotes,
        string? Notes,
        DateTimeOffset CreatedAt,
        string CreatedBy,
        DateTimeOffset? UpdatedAt,
        string? UpdatedBy);

    public async Task<Result<GetActivityByIdResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var activity = await connection.QuerySingleOrDefaultAsync<ActivityDetailRow>(new CommandDefinition(
            _selectActivitySql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (activity is null)
        {
            return Result.Failure<GetActivityByIdResponse>(
                DomainError.NotFound(
                    "Academic.LearningActivityNotFound",
                    $"Learning activity with ID '{id}' was not found."));
        }

        var attendanceSummary = await connection.QuerySingleOrDefaultAsync<ActivityAttendanceSummary>(new CommandDefinition(
            _selectAttendanceSummarySql,
            new { Id = id },
            cancellationToken: cancellationToken)) ?? new ActivityAttendanceSummary(0, 0, 0, 0, 0, 0);

        var assignments = await connection.QueryAsync<ActivityAssignmentItem>(new CommandDefinition(
            _selectAssignmentsSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        var response = new GetActivityByIdResponse(
            activity.Id,
            activity.LearningUnitId,
            activity.LearningUnitCode,
            activity.LearningUnitName,
            activity.ActivityType,
            activity.Title,
            activity.Description,
            activity.SectionLabel,
            activity.ActivityDate,
            activity.StartTime,
            activity.EndTime,
            activity.TeacherAssignmentId,
            activity.TeacherName,
            activity.Status,
            activity.IsAttendanceRequired,
            activity.DeliveryNotes,
            activity.Notes,
            activity.CreatedAt,
            activity.CreatedBy,
            activity.UpdatedAt,
            activity.UpdatedBy,
            attendanceSummary,
            assignments.AsList());

        return Result.Success(response);
    }
}
