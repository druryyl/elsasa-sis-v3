namespace Elsasa.Sis.Academics.Features.LearningUnits.DeleteLearningUnit;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for deleting a learning unit from a draft learning structure (ADR-004, ADR-005, P3-S08).
/// Prohibits deletion on non-draft structures, or if child units or academic requirements reference this unit.
/// </summary>
public sealed class DeleteLearningUnitHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _selectUnitWithStructureSql = """
        SELECT
            u.id AS Id,
            u.learning_structure_id AS LearningStructureId,
            s.status AS StructureStatus
        FROM academics.learning_units u
        INNER JOIN academics.learning_structures s ON u.learning_structure_id = s.id
        WHERE u.id = @Id;
        """;

    private const string _countChildUnitsSql = """
        SELECT COUNT(1)
        FROM academics.learning_units
        WHERE parent_unit_id = @Id;
        """;

    private const string _countLinkedRequirementsSql = """
        SELECT COUNT(1)
        FROM academics.academic_requirements
        WHERE learning_unit_id = @Id;
        """;

    private const string _deleteUnitSql = """
        DELETE FROM academics.learning_units
        WHERE id = @Id;
        """;

    private sealed record UnitWithStructureRow(Guid Id, Guid LearningStructureId, string StructureStatus);

    public async Task<Result> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Fetch unit and owning structure status
        var existing = await connection.QuerySingleOrDefaultAsync<UnitWithStructureRow>(new CommandDefinition(
            _selectUnitWithStructureSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure(
                DomainError.NotFound(
                    "Academic.LearningUnitNotFound",
                    $"Learning unit with ID '{id}' was not found."));
        }

        // 2. Verify structure is in 'draft' status
        if (!string.Equals(existing.StructureStatus, "draft", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(existing.StructureStatus, "active", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure(
                    DomainError.Conflict(
                        "Academic.ImmutableStructuralData",
                        "Structural modifications are locked on active curriculum frameworks. Create a new version via supersede."));
            }

            return Result.Failure(
                DomainError.Conflict(
                    "Academic.ImmutableLearningStructure",
                    $"Learning units can only be deleted from draft learning structures. Current structure status is '{existing.StructureStatus}'."));
        }

        // 3. Verify no child units reference this unit
        var childCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _countChildUnitsSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (childCount > 0)
        {
            return Result.Failure(
                DomainError.Conflict(
                    "Academic.UnitHasChildren",
                    "Cannot delete a learning unit that has child units. Remove or reassign child units first."));
        }

        // 4. Verify no academic requirements reference this unit
        var requirementCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _countLinkedRequirementsSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (requirementCount > 0)
        {
            return Result.Failure(
                DomainError.Conflict(
                    "Academic.UnitHasLinkedRequirements",
                    "Cannot delete a learning unit that is referenced by academic requirements. Remove or reassign linked requirements first."));
        }

        // 5. Delete unit
        await connection.ExecuteAsync(new CommandDefinition(
            _deleteUnitSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        return Result.Success();
    }
}
