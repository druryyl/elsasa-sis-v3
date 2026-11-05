namespace Elsasa.Sis.Academics.Features.LearningUnits.GetLearningUnits;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying all learning units for a learning structure ordered by code (ADR-004, ADR-005, P3-S08).
/// Flat query enables in-memory 2-level tree projection in $O(N)$ time.
/// </summary>
public sealed class GetLearningUnitsHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _checkStructureSql = """
        SELECT COUNT(1)
        FROM academics.learning_structures
        WHERE id = @StructureId;
        """;

    private const string _selectUnitsSql = """
        SELECT
            id AS Id,
            learning_structure_id AS LearningStructureId,
            parent_unit_id AS ParentUnitId,
            code AS Code,
            name AS Name,
            unit_type AS UnitType,
            level AS Level,
            description AS Description,
            credit_weight AS CreditWeight,
            status AS Status,
            notes AS Notes,
            created_at AS CreatedAt,
            created_by AS CreatedBy,
            updated_at AS UpdatedAt,
            updated_by AS UpdatedBy
        FROM academics.learning_units
        WHERE learning_structure_id = @StructureId
        ORDER BY code ASC;
        """;

    public async Task<Result<IReadOnlyList<LearningUnitItemDto>>> HandleAsync(
        Guid structureId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var structureExists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _checkStructureSql,
            new { StructureId = structureId },
            cancellationToken: cancellationToken));

        if (structureExists == 0)
        {
            return Result.Failure<IReadOnlyList<LearningUnitItemDto>>(
                DomainError.NotFound(
                    "Academic.LearningStructureNotFound",
                    $"Learning structure with ID '{structureId}' was not found."));
        }

        var units = await connection.QueryAsync<LearningUnitItemDto>(new CommandDefinition(
            _selectUnitsSql,
            new { StructureId = structureId },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<LearningUnitItemDto>>(units.AsList());
    }
}
