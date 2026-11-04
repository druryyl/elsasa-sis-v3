namespace Elsasa.Sis.Students.Features.Records.History.GetStudentAdministrativeHistory;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying the chronological administrative history audit log by student ID (GAP-006, TD-06).
/// </summary>
public sealed class GetStudentAdministrativeHistoryHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _checkStudentSql = """
        SELECT COUNT(1)
        FROM students.students
        WHERE id = @StudentId;
        """;

    private const string _selectHistorySql = """
        SELECT
            h.id AS Id,
            h.event_type AS EventType,
            h.event_date AS EventDate,
            h.detail AS Detail,
            h.authorized_by AS AuthorizedBy,
            h.decision_reference AS DecisionReference,
            h.is_correction AS IsCorrection,
            h.corrects_entry_id AS CorrectsEntryId,
            h.created_at AS CreatedAt,
            h.created_by AS CreatedBy
        FROM students.administrative_history_entries h
        WHERE h.student_id = @StudentId
        ORDER BY h.event_date DESC, h.created_at DESC;
        """;

    /// <summary>
    /// Retrieves the chronological administrative history entries for a given student ID.
    /// </summary>
    public async Task<Result<GetStudentAdministrativeHistoryResponse>> HandleAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var exists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _checkStudentSql,
            new { StudentId = studentId },
            cancellationToken: cancellationToken));

        if (exists == 0)
        {
            return Result.Failure<GetStudentAdministrativeHistoryResponse>(
                DomainError.NotFound(
                    "Students.Records.StudentNotFound",
                    $"Student with ID '{studentId}' was not found."));
        }

        var entries = await connection.QueryAsync<AdministrativeHistoryEntryDto>(new CommandDefinition(
            _selectHistorySql,
            new { StudentId = studentId },
            cancellationToken: cancellationToken));

        var response = new GetStudentAdministrativeHistoryResponse(
            studentId,
            entries.AsList());

        return Result.Success(response);
    }
}
