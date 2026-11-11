namespace Elsasa.Sis.Teaching.Features.Workload.GetTeacherUnavailabilityWindows;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying a teacher's leave and blackout windows with optional filters (TD-04, P3-S08).
/// </summary>
public sealed class GetTeacherUnavailabilityWindowsHandler(ISqlConnectionFactory connectionFactory)
{
    static GetTeacherUnavailabilityWindowsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkTeacherSql = """
        SELECT TOP 1 1
        FROM teaching.teacher_profiles
        WHERE id = @TeacherId;
        """;

    public async Task<Result<IReadOnlyList<TeacherUnavailabilityWindowDto>>> HandleAsync(
        Guid teacherId,
        string? status = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        if (teacherId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<TeacherUnavailabilityWindowDto>>(DomainError.Validation(
                "Teaching.InvalidTeacherId",
                "Teacher ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var teacherExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            _checkTeacherSql,
            new { TeacherId = teacherId },
            cancellationToken: cancellationToken));

        if (!teacherExists)
        {
            return Result.Failure<IReadOnlyList<TeacherUnavailabilityWindowDto>>(DomainError.NotFound(
                "Teaching.TeacherNotFound",
                $"Teacher profile with ID '{teacherId}' does not exist."));
        }

        var conditions = new List<string> { "tuw.teacher_profile_id = @TeacherId" };
        var parameters = new DynamicParameters();
        parameters.Add("TeacherId", teacherId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            conditions.Add("tuw.status = @Status");
            parameters.Add("Status", status.Trim().ToLowerInvariant());
        }

        if (fromDate.HasValue)
        {
            conditions.Add("tuw.end_date >= @FromDate");
            parameters.Add("FromDate", fromDate.Value);
        }

        if (toDate.HasValue)
        {
            conditions.Add("tuw.start_date <= @ToDate");
            parameters.Add("ToDate", toDate.Value);
        }

        var whereClause = string.Join(" AND ", conditions);

        var querySql = $"""
            SELECT
                tuw.id AS Id,
                tuw.teacher_profile_id AS TeacherProfileId,
                tuw.start_date AS StartDate,
                tuw.end_date AS EndDate,
                tuw.start_time AS StartTime,
                tuw.end_time AS EndTime,
                tuw.unavailability_type AS UnavailabilityType,
                tuw.external_source_reference AS ExternalSourceReference,
                tuw.reason AS Reason,
                tuw.status AS Status,
                tuw.notes AS Notes,
                tuw.created_at AS CreatedAt
            FROM teaching.teacher_unavailability_windows tuw
            WHERE {whereClause}
            ORDER BY tuw.start_date DESC;
            """;

        var items = (await connection.QueryAsync<TeacherUnavailabilityWindowDto>(new CommandDefinition(
            querySql,
            parameters,
            cancellationToken: cancellationToken))).AsList();

        return Result.Success<IReadOnlyList<TeacherUnavailabilityWindowDto>>(items);
    }
}
