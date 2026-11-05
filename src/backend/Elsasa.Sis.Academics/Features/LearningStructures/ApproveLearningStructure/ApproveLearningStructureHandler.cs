namespace Elsasa.Sis.Academics.Features.LearningStructures.ApproveLearningStructure;

using Dapper;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler executing formal approval and activation of a learning structure framework (P3-S07, ACD-WF-01, TD-06, TD-09).
/// Enforces transaction with UPDLOCK, HOLDLOCK, validates Criterion 1, activates structure and children,
/// retires any predecessor structure and its children, and publishes LearningStructureActivatedEvent post-commit.
/// </summary>
public sealed class ApproveLearningStructureHandler(
    ISqlConnectionFactory connectionFactory,
    IPublisher publisher,
    ICurrentUser currentUser)
{
    static ApproveLearningStructureHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectStructureForLockSql = """
        SELECT
            id AS Id,
            academic_period_id AS AcademicPeriodId,
            version AS Version,
            effective_date AS EffectiveDate,
            status AS Status,
            supersedes_id AS SupersedesId
        FROM academics.learning_structures WITH (UPDLOCK, HOLDLOCK)
        WHERE id = @Id;
        """;

    private const string _countChildrenSql = """
        SELECT
            (SELECT COUNT(1) FROM academics.learning_units WHERE learning_structure_id = @Id) AS UnitCount,
            (SELECT COUNT(1) FROM academics.academic_requirements WHERE learning_structure_id = @Id) AS RequirementCount;
        """;

    private const string _updateStructureActiveSql = """
        UPDATE academics.learning_structures
        SET
            status = 'active',
            approved_by = @ApprovedBy,
            approved_date = @ApprovedDate,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private const string _updateUnitsActiveSql = """
        UPDATE academics.learning_units
        SET
            status = 'active',
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE learning_structure_id = @Id;
        """;

    private const string _updateRequirementsActiveSql = """
        UPDATE academics.academic_requirements
        SET
            status = 'active',
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE learning_structure_id = @Id;
        """;

    private const string _updatePredecessorStructureSql = """
        UPDATE academics.learning_structures
        SET
            status = 'superseded',
            end_date = @EffectiveDate,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @PredecessorId;
        """;

    private const string _updatePredecessorUnitsSql = """
        UPDATE academics.learning_units
        SET
            status = 'archived',
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE learning_structure_id = @PredecessorId;
        """;

    private const string _updatePredecessorRequirementsSql = """
        UPDATE academics.academic_requirements
        SET
            status = 'superseded',
            end_date = @EffectiveDate,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE learning_structure_id = @PredecessorId;
        """;

    private sealed record StructureLockRow(
        Guid Id,
        Guid AcademicPeriodId,
        string Version,
        DateOnly EffectiveDate,
        string Status,
        Guid? SupersedesId);

    private sealed record CriterionCountRow(int UnitCount, int RequirementCount);

    /// <summary>
    /// Executes transactional approval and activation workflow.
    /// </summary>
    public async Task<Result<ApproveLearningStructureResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // 1. Lock and retrieve structure with UPDLOCK, HOLDLOCK
        var structure = await connection.QuerySingleOrDefaultAsync<StructureLockRow>(new CommandDefinition(
            _selectStructureForLockSql,
            new { Id = id },
            transaction: transaction,
            cancellationToken: cancellationToken));

        if (structure is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<ApproveLearningStructureResponse>(
                DomainError.NotFound(
                    "Academics.LearningStructureNotFound",
                    $"Learning structure with ID '{id}' was not found."));
        }

        // 2. Verifies structure status is 'draft' or 'submitted'
        if (!string.Equals(structure.Status, "draft", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(structure.Status, "submitted", StringComparison.OrdinalIgnoreCase))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<ApproveLearningStructureResponse>(
                DomainError.Conflict(
                    "Academics.InvalidApprovalStatus",
                    $"Only draft or submitted learning structures may be approved. Current status is '{structure.Status}'."));
        }

        // 3. Verifies Stage 2 Criterion 1: structure contains at least 1 unit and at least 1 requirement
        var counts = await connection.QuerySingleAsync<CriterionCountRow>(new CommandDefinition(
            _countChildrenSql,
            new { Id = id },
            transaction: transaction,
            cancellationToken: cancellationToken));

        if (counts.UnitCount == 0 || counts.RequirementCount == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<ApproveLearningStructureResponse>(
                DomainError.Validation(
                    "Academics.EmptyLearningStructure",
                    "Learning structure must contain at least one learning unit and at least one academic requirement before approval."));
        }

        var now = DateTimeOffset.UtcNow;
        var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var auditUser = currentUser.Username ?? currentUser.Email ?? "system";

        // 4. Update structure to active
        await connection.ExecuteAsync(new CommandDefinition(
            _updateStructureActiveSql,
            new
            {
                Id = id,
                ApprovedBy = auditUser,
                ApprovedDate = currentDate,
                UpdatedAt = now,
                UpdatedBy = auditUser
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 5. Update child units and requirements to active
        await connection.ExecuteAsync(new CommandDefinition(
            _updateUnitsActiveSql,
            new
            {
                Id = id,
                UpdatedAt = now,
                UpdatedBy = auditUser
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            _updateRequirementsActiveSql,
            new
            {
                Id = id,
                UpdatedAt = now,
                UpdatedBy = auditUser
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 6. If supersedes_id is populated, retire predecessor structure and its children
        if (structure.SupersedesId.HasValue)
        {
            var predecessorId = structure.SupersedesId.Value;

            await connection.ExecuteAsync(new CommandDefinition(
                _updatePredecessorStructureSql,
                new
                {
                    PredecessorId = predecessorId,
                    EffectiveDate = structure.EffectiveDate,
                    UpdatedAt = now,
                    UpdatedBy = auditUser
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                _updatePredecessorUnitsSql,
                new
                {
                    PredecessorId = predecessorId,
                    UpdatedAt = now,
                    UpdatedBy = auditUser
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                _updatePredecessorRequirementsSql,
                new
                {
                    PredecessorId = predecessorId,
                    EffectiveDate = structure.EffectiveDate,
                    UpdatedAt = now,
                    UpdatedBy = auditUser
                },
                transaction: transaction,
                cancellationToken: cancellationToken));
        }

        // 7. Commit transaction
        await transaction.CommitAsync(cancellationToken);

        // 8. Publish LearningStructureActivatedEvent post-commit via IPublisher (ADR-003)
        await publisher.Publish(
            new LearningStructureActivatedEvent(
                structure.Id,
                structure.AcademicPeriodId,
                structure.Version,
                structure.EffectiveDate,
                now),
            cancellationToken);

        return Result.Success(new ApproveLearningStructureResponse(
            structure.Id,
            "active",
            auditUser,
            currentDate,
            now,
            auditUser));
    }
}
