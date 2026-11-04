namespace Elsasa.Sis.Academics.Features.LearningStructures.UpdateLearningStructure;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing metadata updates on learning structure frameworks (ADR-004, ADR-005, P3-S06).
/// Enforces immutability guard rules: active structures permit metadata updates only;
/// superseded and archived structures strictly reject modifications.
/// </summary>
public sealed class UpdateLearningStructureHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectStatusSql = """
        SELECT id AS Id, status AS Status
        FROM academics.learning_structures
        WHERE id = @Id;
        """;

    private const string _updateStructureSql = """
        UPDATE academics.learning_structures
        SET
            name = @Name,
            description = @Description,
            notes = @Notes,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private sealed record StructureStatusRow(Guid Id, string Status);

    /// <summary>
    /// Executes metadata update on an existing learning structure.
    /// </summary>
    public async Task<Result<UpdateLearningStructureResponse>> HandleAsync(
        Guid id,
        UpdateLearningStructureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Fetch current status to enforce immutability guards
        var existing = await connection.QuerySingleOrDefaultAsync<StructureStatusRow>(new CommandDefinition(
            _selectStatusSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<UpdateLearningStructureResponse>(
                DomainError.NotFound(
                    "Academics.LearningStructureNotFound",
                    $"Learning structure with ID '{id}' was not found."));
        }

        // 2. Reject modification if framework is superseded or archived
        if (string.Equals(existing.Status, "superseded", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(existing.Status, "archived", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<UpdateLearningStructureResponse>(
                DomainError.Conflict(
                    "Academics.ImmutableLearningStructure",
                    $"Learning structure in status '{existing.Status}' cannot be modified."));
        }

        // 3. Perform metadata update (applicable to 'draft', 'submitted', and 'active' status)
        var updatedAt = DateTimeOffset.UtcNow;
        var updatedBy = currentUser.Username ?? currentUser.Email ?? "system";

        var parameters = new
        {
            Id = id,
            request.Name,
            request.Description,
            request.Notes,
            UpdatedAt = updatedAt,
            UpdatedBy = updatedBy
        };

        await connection.ExecuteAsync(new CommandDefinition(
            _updateStructureSql,
            parameters,
            cancellationToken: cancellationToken));

        return Result.Success(new UpdateLearningStructureResponse(
            id,
            request.Name,
            request.Description,
            request.Notes,
            existing.Status,
            updatedAt,
            updatedBy));
    }
}
