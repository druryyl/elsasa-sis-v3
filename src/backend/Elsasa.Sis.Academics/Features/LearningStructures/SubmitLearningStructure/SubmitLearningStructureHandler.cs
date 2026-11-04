namespace Elsasa.Sis.Academics.Features.LearningStructures.SubmitLearningStructure;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler submitting a draft learning structure framework for review (P3-S07, ACD-WF-01, TD-06).
/// Enforces Stage 2 Criterion 1: verifies structure contains at least 1 learning unit and at least 1 academic requirement.
/// </summary>
public sealed class SubmitLearningStructureHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static SubmitLearningStructureHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectStructureStatusSql = """
        SELECT
            id AS Id,
            status AS Status
        FROM academics.learning_structures
        WHERE id = @Id;
        """;

    private const string _countChildrenSql = """
        SELECT
            (SELECT COUNT(1) FROM academics.learning_units WHERE learning_structure_id = @Id) AS UnitCount,
            (SELECT COUNT(1) FROM academics.academic_requirements WHERE learning_structure_id = @Id) AS RequirementCount;
        """;

    private const string _updateStatusSql = """
        UPDATE academics.learning_structures
        SET
            status = 'submitted',
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private sealed record StructureStatusRow(Guid Id, string Status);
    private sealed record CriterionCountRow(int UnitCount, int RequirementCount);

    /// <summary>
    /// Executes the submission lifecycle transition.
    /// </summary>
    public async Task<Result<SubmitLearningStructureResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Fetch current status
        var structure = await connection.QuerySingleOrDefaultAsync<StructureStatusRow>(new CommandDefinition(
            _selectStructureStatusSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (structure is null)
        {
            return Result.Failure<SubmitLearningStructureResponse>(
                DomainError.NotFound(
                    "Academics.LearningStructureNotFound",
                    $"Learning structure with ID '{id}' was not found."));
        }

        // 2. Verifies structure status is 'draft'
        if (!string.Equals(structure.Status, "draft", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<SubmitLearningStructureResponse>(
                DomainError.Conflict(
                    "Academics.InvalidSubmissionStatus",
                    $"Only draft learning structures may be submitted. Current status is '{structure.Status}'."));
        }

        // 3. Enforces Stage 2 Criterion 1: verifies structure contains at least 1 learning unit and at least 1 academic requirement
        var counts = await connection.QuerySingleAsync<CriterionCountRow>(new CommandDefinition(
            _countChildrenSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (counts.UnitCount == 0 || counts.RequirementCount == 0)
        {
            return Result.Failure<SubmitLearningStructureResponse>(
                DomainError.Validation(
                    "Academics.EmptyLearningStructure",
                    "Learning structure must contain at least one learning unit and at least one academic requirement before submission."));
        }

        // 4. Transitions status = 'submitted', updates audit fields
        var now = DateTimeOffset.UtcNow;
        var auditUser = currentUser.Username ?? currentUser.Email ?? "system";

        await connection.ExecuteAsync(new CommandDefinition(
            _updateStatusSql,
            new
            {
                Id = id,
                UpdatedAt = now,
                UpdatedBy = auditUser
            },
            cancellationToken: cancellationToken));

        return Result.Success(new SubmitLearningStructureResponse(
            id,
            "submitted",
            now,
            auditUser));
    }
}
