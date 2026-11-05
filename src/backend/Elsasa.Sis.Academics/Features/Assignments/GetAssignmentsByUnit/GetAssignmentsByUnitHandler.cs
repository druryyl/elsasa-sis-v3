namespace Elsasa.Sis.Academics.Features.Assignments.GetAssignmentsByUnit;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for querying coursework assignments across all learning activities in a learning unit (P5-S10).
/// </summary>
public sealed class GetAssignmentsByUnitHandler(ISqlConnectionFactory connectionFactory)
{
    static GetAssignmentsByUnitHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkUnitSql = """
        SELECT COUNT(1)
        FROM academics.learning_units
        WHERE id = @UnitId;
        """;

    private const string _selectAssignmentsSql = """
        SELECT
            a.id AS Id,
            a.learning_activity_id AS LearningActivityId,
            act.title AS LearningActivityTitle,
            act.activity_type AS LearningActivityType,
            act.activity_date AS LearningActivityDate,
            a.title AS Title,
            a.description AS Description,
            a.release_date AS ReleaseDate,
            a.due_date AS DueDate,
            a.status AS Status,
            a.notes AS Notes,
            a.created_at AS CreatedAt,
            a.created_by AS CreatedBy,
            a.updated_at AS UpdatedAt,
            a.updated_by AS UpdatedBy
        FROM academics.assignments a
        INNER JOIN academics.learning_activities act ON act.id = a.learning_activity_id
        WHERE act.learning_unit_id = @UnitId
          AND (@Status IS NULL OR a.status = @Status)
        ORDER BY a.release_date ASC, a.created_at ASC;
        """;

    public async Task<Result<IReadOnlyList<UnitAssignmentItemDto>>> HandleAsync(
        Guid unitId,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var unitExists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _checkUnitSql,
            new { UnitId = unitId },
            cancellationToken: cancellationToken));

        if (unitExists == 0)
        {
            return Result.Failure<IReadOnlyList<UnitAssignmentItemDto>>(
                DomainError.NotFound(
                    "Academic.LearningUnitNotFound",
                    $"Learning unit with ID '{unitId}' was not found."));
        }

        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToLowerInvariant();

        var assignments = await connection.QueryAsync<UnitAssignmentItemDto>(new CommandDefinition(
            _selectAssignmentsSql,
            new
            {
                UnitId = unitId,
                Status = normalizedStatus
            },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<UnitAssignmentItemDto>>(assignments.AsList());
    }
}
