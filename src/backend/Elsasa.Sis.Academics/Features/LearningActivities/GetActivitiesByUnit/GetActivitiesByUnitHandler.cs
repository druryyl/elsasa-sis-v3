namespace Elsasa.Sis.Academics.Features.LearningActivities.GetActivitiesByUnit;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying learning activities for a learning unit (ADR-004, ADR-005, P3-S07).
/// </summary>
public sealed class GetActivitiesByUnitHandler(ISqlConnectionFactory connectionFactory)
{
    static GetActivitiesByUnitHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkUnitSql = """
        SELECT COUNT(1)
        FROM academics.learning_units
        WHERE id = @LearningUnitId;
        """;

    private const string _selectActivitiesSql = """
        SELECT
            id AS Id,
            learning_unit_id AS LearningUnitId,
            activity_type AS ActivityType,
            title AS Title,
            description AS Description,
            section_label AS SectionLabel,
            activity_date AS ActivityDate,
            start_time AS StartTime,
            end_time AS EndTime,
            teacher_assignment_id AS TeacherAssignmentId,
            teacher_name AS TeacherName,
            status AS Status,
            is_attendance_required AS IsAttendanceRequired,
            delivery_notes AS DeliveryNotes,
            notes AS Notes,
            created_at AS CreatedAt,
            created_by AS CreatedBy,
            updated_at AS UpdatedAt,
            updated_by AS UpdatedBy
        FROM academics.learning_activities
        WHERE learning_unit_id = @LearningUnitId
          AND (@StartDate IS NULL OR activity_date >= @StartDate)
          AND (@EndDate IS NULL OR activity_date <= @EndDate)
          AND (@SectionLabel IS NULL OR section_label = @SectionLabel)
          AND (@Status IS NULL OR status = @Status)
        ORDER BY activity_date ASC, start_time ASC;
        """;

    public async Task<Result<IReadOnlyList<LearningActivityItemDto>>> HandleAsync(
        Guid learningUnitId,
        DateOnly? startDate,
        DateOnly? endDate,
        string? sectionLabel,
        string? status,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var unitExists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _checkUnitSql,
            new { LearningUnitId = learningUnitId },
            cancellationToken: cancellationToken));

        if (unitExists == 0)
        {
            return Result.Failure<IReadOnlyList<LearningActivityItemDto>>(
                DomainError.NotFound(
                    "Academic.LearningUnitNotFound",
                    $"Learning unit with ID '{learningUnitId}' was not found."));
        }

        var activities = await connection.QueryAsync<LearningActivityItemDto>(new CommandDefinition(
            _selectActivitiesSql,
            new
            {
                LearningUnitId = learningUnitId,
                StartDate = startDate,
                EndDate = endDate,
                SectionLabel = string.IsNullOrWhiteSpace(sectionLabel) ? null : sectionLabel.Trim(),
                Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToLowerInvariant()
            },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<LearningActivityItemDto>>(activities.AsList());
    }
}
