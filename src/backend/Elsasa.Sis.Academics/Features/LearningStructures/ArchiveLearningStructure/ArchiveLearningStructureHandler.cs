namespace Elsasa.Sis.Academics.Features.LearningStructures.ArchiveLearningStructure;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler transitioning superseded or inactive learning structures to archived status (P3-S07, TD-06).
/// </summary>
public sealed class ArchiveLearningStructureHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static ArchiveLearningStructureHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectStructureSql = """
        SELECT
            id AS Id,
            status AS Status,
            end_date AS EndDate
        FROM academics.learning_structures
        WHERE id = @Id;
        """;

    private const string _archiveStructureAndChildrenSql = """
        UPDATE academics.learning_structures
        SET
            status = 'archived',
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;

        UPDATE academics.learning_units
        SET
            status = 'archived',
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE learning_structure_id = @Id;

        UPDATE academics.academic_requirements
        SET
            status = 'archived',
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE learning_structure_id = @Id;
        """;

    private sealed record StructureRow(Guid Id, string Status, DateOnly? EndDate);

    /// <summary>
    /// Executes archiving lifecycle transition.
    /// </summary>
    public async Task<Result<ArchiveLearningStructureResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Fetch current status
        var structure = await connection.QuerySingleOrDefaultAsync<StructureRow>(new CommandDefinition(
            _selectStructureSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (structure is null)
        {
            return Result.Failure<ArchiveLearningStructureResponse>(
                DomainError.NotFound(
                    "Academics.LearningStructureNotFound",
                    $"Learning structure with ID '{id}' was not found."));
        }

        // 2. Reject if already archived
        if (string.Equals(structure.Status, "archived", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ArchiveLearningStructureResponse>(
                DomainError.Conflict(
                    "Academics.AlreadyArchived",
                    "Learning structure is already archived."));
        }

        // 3. Reject active operational structures (unless expired/inactive)
        var isInactiveActive = string.Equals(structure.Status, "active", StringComparison.OrdinalIgnoreCase) &&
            structure.EndDate.HasValue && structure.EndDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow);

        if (string.Equals(structure.Status, "active", StringComparison.OrdinalIgnoreCase) && !isInactiveActive)
        {
            return Result.Failure<ArchiveLearningStructureResponse>(
                DomainError.Conflict(
                    "Academics.ActiveStructureCannotBeArchived",
                    "Active learning structures cannot be archived while operational. Only superseded or inactive structures may be archived."));
        }

        // 4. Archive structure, units, and requirements
        var now = DateTimeOffset.UtcNow;
        var auditUser = currentUser.Username ?? currentUser.Email ?? "system";

        await connection.ExecuteAsync(new CommandDefinition(
            _archiveStructureAndChildrenSql,
            new
            {
                Id = id,
                UpdatedAt = now,
                UpdatedBy = auditUser
            },
            cancellationToken: cancellationToken));

        return Result.Success(new ArchiveLearningStructureResponse(
            id,
            "archived",
            now,
            auditUser));
    }
}
