namespace Elsasa.Sis.Academics.Features.LearningActivities.GetTeacherDailyActivities;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying scheduled or conducted activities for the authenticated teacher by date (ADR-004, ADR-005, P3-S07, SCR-ACD-01).
/// </summary>
public sealed class GetTeacherDailyActivitiesHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static GetTeacherDailyActivitiesHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectTeacherActivitiesSql = """
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
            a.delivery_notes AS DeliveryNotes
        FROM academics.learning_activities a
        INNER JOIN academics.learning_units u ON u.id = a.learning_unit_id
        WHERE a.activity_date = @ActivityDate
          AND a.status IN ('planned', 'conducted')
          AND (@SectionLabel IS NULL OR a.section_label = @SectionLabel)
          AND (
              (@AllowAll = 1)
              OR (@TeacherAssignmentId IS NOT NULL AND a.teacher_assignment_id = @TeacherAssignmentId)
              OR (@TeacherName IS NOT NULL AND a.teacher_name = @TeacherName)
          )
        ORDER BY a.start_time ASC, a.title ASC;
        """;

    public async Task<Result<IReadOnlyList<TeacherDailyActivityItemDto>>> HandleAsync(
        DateOnly? date,
        string? sectionLabel,
        string? teacherNameOverride = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var effectiveTeacherName = !string.IsNullOrWhiteSpace(teacherNameOverride)
            ? teacherNameOverride.Trim()
            : currentUser.Username;

        var teacherAssignmentId = currentUser.UserId;

        var isAdminOrCoordinator = currentUser.Roles.Any(r =>
            string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(r, "AcademicCoordinator", StringComparison.OrdinalIgnoreCase)
            || string.Equals(r, "Academic Coordinator", StringComparison.OrdinalIgnoreCase)
            || string.Equals(r, "AcademicAuthority", StringComparison.OrdinalIgnoreCase)
            || string.Equals(r, "Academic Authority", StringComparison.OrdinalIgnoreCase));

        var allowAll = isAdminOrCoordinator && string.IsNullOrWhiteSpace(teacherNameOverride);

        var activities = await connection.QueryAsync<TeacherDailyActivityItemDto>(new CommandDefinition(
            _selectTeacherActivitiesSql,
            new
            {
                ActivityDate = targetDate,
                SectionLabel = string.IsNullOrWhiteSpace(sectionLabel) ? null : sectionLabel.Trim(),
                TeacherAssignmentId = teacherAssignmentId,
                TeacherName = effectiveTeacherName,
                AllowAll = allowAll ? 1 : 0
            },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<TeacherDailyActivityItemDto>>(activities.AsList());
    }
}
