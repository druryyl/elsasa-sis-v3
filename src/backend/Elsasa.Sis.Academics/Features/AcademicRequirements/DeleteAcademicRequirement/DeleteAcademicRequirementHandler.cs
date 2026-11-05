namespace Elsasa.Sis.Academics.Features.AcademicRequirements.DeleteAcademicRequirement;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler deleting an academic requirement (ADR-004, ADR-005, P3-S09).
/// Deletions are permitted strictly when the owning learning structure is in draft status.
/// </summary>
public sealed class DeleteAcademicRequirementHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _selectRequirementWithStructureSql = """
        SELECT
            ar.id AS Id,
            ar.status AS Status,
            ls.status AS LearningStructureStatus
        FROM academics.academic_requirements ar
        INNER JOIN academics.learning_structures ls ON ar.learning_structure_id = ls.id
        WHERE ar.id = @Id;
        """;

    private const string _selectSuccessorExistsSql = """
        SELECT 1
        FROM academics.academic_requirements
        WHERE supersedes_id = @Id;
        """;

    private const string _deleteRequirementSql = """
        DELETE FROM academics.academic_requirements
        WHERE id = @Id;
        """;

    private sealed record RequirementStatusRow(Guid Id, string Status, string LearningStructureStatus);

    /// <summary>
    /// Executes academic requirement deletion.
    /// </summary>
    public async Task<Result> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Fetch requirement and owning structure status
        var existing = await connection.QuerySingleOrDefaultAsync<RequirementStatusRow>(new CommandDefinition(
            _selectRequirementWithStructureSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure(
                DomainError.NotFound(
                    "Academics.AcademicRequirementNotFound",
                    $"Academic requirement with ID '{id}' was not found."));
        }

        // 2. Permitted only when owning structure is in draft status
        if (!string.Equals(existing.LearningStructureStatus, "draft", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(
                DomainError.Conflict(
                    "Academics.ImmutableAcademicRequirement",
                    $"Academic requirements can only be deleted when the owning learning structure is in 'draft' status. Current status is '{existing.LearningStructureStatus}'."));
        }

        // 3. Prevent deletion if another requirement references this as its predecessor
        var hasSuccessor = await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
            _selectSuccessorExistsSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (hasSuccessor.HasValue)
        {
            return Result.Failure(
                DomainError.Conflict(
                    "Academics.RequirementHasSuccessor",
                    "Cannot delete an academic requirement that has been superseded by another requirement."));
        }

        // 4. Delete row
        await connection.ExecuteAsync(new CommandDefinition(
            _deleteRequirementSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        return Result.Success();
    }
}
