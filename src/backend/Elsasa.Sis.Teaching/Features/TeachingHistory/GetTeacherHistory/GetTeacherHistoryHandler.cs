namespace Elsasa.Sis.Teaching.Features.TeachingHistory.GetTeacherHistory;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature query handler for retrieving a teacher's immutable audit history log (TD-07, P4-S14).
/// </summary>
public sealed class GetTeacherHistoryHandler(ISqlConnectionFactory connectionFactory)
{
    static GetTeacherHistoryHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkTeacherSql = """
        SELECT TOP 1 1
        FROM teaching.teacher_profiles
        WHERE id = @TeacherId;
        """;

    public async Task<Result<IReadOnlyList<TeachingHistoryEntryDto>>> HandleAsync(
        Guid teacherId,
        string? eventType = null,
        CancellationToken cancellationToken = default)
    {
        if (teacherId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<TeachingHistoryEntryDto>>(DomainError.Validation(
                "Teaching.InvalidTeacherId",
                "Teacher profile ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var teacherExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            _checkTeacherSql,
            new { TeacherId = teacherId },
            cancellationToken: cancellationToken));

        if (!teacherExists)
        {
            return Result.Failure<IReadOnlyList<TeachingHistoryEntryDto>>(DomainError.NotFound(
                "Teaching.TeacherNotFound",
                $"Teacher profile with ID '{teacherId}' does not exist."));
        }

        var conditions = new List<string> { "teacher_profile_id = @TeacherId" };
        var parameters = new DynamicParameters();
        parameters.Add("TeacherId", teacherId);

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            conditions.Add("event_type = @EventType");
            parameters.Add("EventType", eventType.Trim());
        }

        var whereClause = string.Join(" AND ", conditions);

        var querySql = $"""
            SELECT
                id AS Id,
                teacher_profile_id AS TeacherProfileId,
                event_type AS EventType,
                entity_type AS EntityType,
                entity_id AS EntityId,
                effective_date AS EffectiveDate,
                event_timestamp AS EventTimestamp,
                actor_role AS ActorRole,
                actor_id AS ActorId,
                event_summary AS EventSummary,
                change_payload AS ChangePayload,
                notes AS Notes,
                created_at AS CreatedAt
            FROM teaching.teaching_history_entries
            WHERE {whereClause}
            ORDER BY event_timestamp DESC, created_at DESC;
            """;

        var rows = (await connection.QueryAsync<TeachingHistoryEntryDto>(new CommandDefinition(
            querySql,
            parameters,
            cancellationToken: cancellationToken))).AsList();

        return Result.Success<IReadOnlyList<TeachingHistoryEntryDto>>(rows);
    }
}
