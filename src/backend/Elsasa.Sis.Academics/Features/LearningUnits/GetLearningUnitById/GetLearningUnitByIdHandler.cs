namespace Elsasa.Sis.Academics.Features.LearningUnits.GetLearningUnitById;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying a single learning unit by ID with parent details (ADR-004, ADR-005, P3-S08).
/// </summary>
public sealed class GetLearningUnitByIdHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _selectUnitByIdSql = """
        SELECT
            u.id AS Id,
            u.learning_structure_id AS LearningStructureId,
            u.parent_unit_id AS ParentUnitId,
            p.code AS ParentCode,
            p.name AS ParentName,
            p.unit_type AS ParentUnitType,
            u.code AS Code,
            u.name AS Name,
            u.unit_type AS UnitType,
            u.level AS Level,
            u.description AS Description,
            u.credit_weight AS CreditWeight,
            u.status AS Status,
            u.notes AS Notes,
            u.created_at AS CreatedAt,
            u.created_by AS CreatedBy,
            u.updated_at AS UpdatedAt,
            u.updated_by AS UpdatedBy
        FROM academics.learning_units u
        LEFT JOIN academics.learning_units p ON u.parent_unit_id = p.id
        WHERE u.id = @Id;
        """;

    public async Task<Result<LearningUnitDetailResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var unit = await connection.QuerySingleOrDefaultAsync<LearningUnitDetailResponse>(new CommandDefinition(
            _selectUnitByIdSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (unit is null)
        {
            return Result.Failure<LearningUnitDetailResponse>(
                DomainError.NotFound(
                    "Academic.LearningUnitNotFound",
                    $"Learning unit with ID '{id}' was not found."));
        }

        return Result.Success(unit);
    }
}
