namespace Elsasa.Sis.Academics.Services;

using System.Data;
using Dapper;
using Elsasa.Sis.Shared.Common;

/// <summary>
/// Service executing the transactional deep clone-on-supersede curriculum versioning workflow (GAP-003, OQ-001, TD-03).
/// </summary>
public sealed class CurriculumCloningService : ICurriculumCloningService
{
    private const string _selectPredecessorSql = """
        SELECT
            id AS Id,
            name AS Name,
            description AS Description,
            academic_period_id AS AcademicPeriodId,
            academic_period_label AS AcademicPeriodLabel,
            version AS Version,
            effective_date AS EffectiveDate,
            end_date AS EndDate,
            status AS Status,
            notes AS Notes
        FROM academics.learning_structures WITH (UPDLOCK, HOLDLOCK)
        WHERE id = @PredecessorId;
        """;

    private const string _insertStructureSql = """
        INSERT INTO academics.learning_structures (
            id,
            name,
            description,
            academic_period_id,
            academic_period_label,
            version,
            effective_date,
            end_date,
            status,
            approved_by,
            approved_date,
            supersedes_id,
            notes,
            created_at,
            created_by,
            updated_at,
            updated_by
        ) VALUES (
            @Id,
            @Name,
            @Description,
            @AcademicPeriodId,
            @AcademicPeriodLabel,
            @Version,
            @EffectiveDate,
            NULL,
            'draft',
            NULL,
            NULL,
            @SupersedesId,
            @Notes,
            @CreatedAt,
            @CreatedBy,
            NULL,
            NULL
        );
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
            notes AS Notes
        FROM academics.learning_units
        WHERE learning_structure_id = @PredecessorId;
        """;

    private const string _insertUnitSql = """
        INSERT INTO academics.learning_units (
            id,
            learning_structure_id,
            parent_unit_id,
            code,
            name,
            unit_type,
            level,
            description,
            credit_weight,
            status,
            notes,
            created_at,
            created_by,
            updated_at,
            updated_by
        ) VALUES (
            @Id,
            @LearningStructureId,
            @ParentUnitId,
            @Code,
            @Name,
            @UnitType,
            @Level,
            @Description,
            @CreditWeight,
            @Status,
            @Notes,
            @CreatedAt,
            @CreatedBy,
            NULL,
            NULL
        );
        """;

    private const string _selectRequirementsSql = """
        SELECT
            id AS Id,
            name AS Name,
            requirement_scope AS RequirementScope,
            learning_structure_id AS LearningStructureId,
            learning_unit_id AS LearningUnitId,
            level AS Level,
            requirement_type AS RequirementType,
            description AS Description,
            version AS Version,
            effective_date AS EffectiveDate,
            end_date AS EndDate,
            notes AS Notes
        FROM academics.academic_requirements
        WHERE learning_structure_id = @PredecessorId;
        """;

    private const string _insertRequirementSql = """
        INSERT INTO academics.academic_requirements (
            id,
            name,
            requirement_scope,
            learning_structure_id,
            learning_unit_id,
            level,
            requirement_type,
            description,
            version,
            effective_date,
            end_date,
            status,
            approved_by,
            approved_date,
            supersedes_id,
            notes,
            created_at,
            created_by,
            updated_at,
            updated_by
        ) VALUES (
            @Id,
            @Name,
            @RequirementScope,
            @LearningStructureId,
            @LearningUnitId,
            @Level,
            @RequirementType,
            @Description,
            @Version,
            @EffectiveDate,
            NULL,
            @Status,
            NULL,
            NULL,
            @SupersedesId,
            @Notes,
            @CreatedAt,
            @CreatedBy,
            NULL,
            NULL
        );
        """;

    /// <inheritdoc />
    public async Task<Result<Guid>> CloneStructureToDraftAsync(
        Guid predecessorId,
        string newVersion,
        DateOnly effectiveDate,
        Guid? newPeriodId,
        string? newPeriodLabel,
        string currentUser,
        IDbConnection connection,
        IDbTransaction transaction,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);

        try
        {
            // 1. Lock predecessor structure using UPDLOCK, HOLDLOCK and verify status = 'active'.
            var predecessor = await connection.QuerySingleOrDefaultAsync<PredecessorStructureRow>(new CommandDefinition(
                _selectPredecessorSql,
                new { PredecessorId = predecessorId },
                transaction,
                cancellationToken: ct));

            if (predecessor is null)
            {
                return Result.Failure<Guid>(DomainError.NotFound(
                    "Academic.LearningStructureNotFound",
                    $"Learning structure with ID '{predecessorId}' was not found."));
            }

            if (!string.Equals(predecessor.Status, "active", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<Guid>(DomainError.Conflict(
                    "Academic.InvalidSupersedeStatus",
                    "Only active learning structures may be superseded."));
            }

            var newStructureId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            var effectivePeriodId = newPeriodId ?? predecessor.AcademicPeriodId;
            var effectivePeriodLabel = !string.IsNullOrWhiteSpace(newPeriodLabel)
                ? newPeriodLabel
                : predecessor.AcademicPeriodLabel;

            // 2. Insert cloned academics.learning_structures record with status = 'draft' and supersedes_id = predecessorId.
            var insertStructureParams = new
            {
                Id = newStructureId,
                predecessor.Name,
                predecessor.Description,
                AcademicPeriodId = effectivePeriodId,
                AcademicPeriodLabel = effectivePeriodLabel,
                Version = newVersion,
                EffectiveDate = effectiveDate,
                SupersedesId = predecessorId,
                predecessor.Notes,
                CreatedAt = now,
                CreatedBy = currentUser
            };

            await connection.ExecuteAsync(new CommandDefinition(
                _insertStructureSql,
                insertStructureParams,
                transaction,
                cancellationToken: ct));

            // 3. Query all academics.learning_units where learning_structure_id = predecessorId.
            var predecessorUnits = (await connection.QueryAsync<LearningUnitRow>(new CommandDefinition(
                _selectUnitsSql,
                new { PredecessorId = predecessorId },
                transaction,
                cancellationToken: ct))).ToList();

            // 4. Build in-memory dictionary oldToNewUnitIds, assign new Guids, and remap parent_unit_id.
            var oldToNewUnitIds = new Dictionary<Guid, Guid>(predecessorUnits.Count);
            foreach (var oldUnit in predecessorUnits)
            {
                oldToNewUnitIds[oldUnit.Id] = Guid.NewGuid();
            }

            var clonedUnits = predecessorUnits.Select(oldUnit =>
            {
                var newUnitId = oldToNewUnitIds[oldUnit.Id];
                var newParentId = oldUnit.ParentUnitId.HasValue && oldToNewUnitIds.TryGetValue(oldUnit.ParentUnitId.Value, out var mappedParent)
                    ? mappedParent
                    : (Guid?)null;

                return new
                {
                    Id = newUnitId,
                    LearningStructureId = newStructureId,
                    ParentUnitId = newParentId,
                    oldUnit.Code,
                    oldUnit.Name,
                    oldUnit.UnitType,
                    oldUnit.Level,
                    oldUnit.Description,
                    oldUnit.CreditWeight,
                    Status = "draft",
                    oldUnit.Notes,
                    CreatedAt = now,
                    CreatedBy = currentUser
                };
            }).ToList();

            // 5. Insert cloned units: insert root units (ParentUnitId == null) first, then child units,
            // to satisfy foreign key constraints (FK_academics_learning_units_parent).
            if (clonedUnits.Count > 0)
            {
                var rootUnits = clonedUnits.Where(u => u.ParentUnitId is null).ToList();
                var childUnits = clonedUnits.Where(u => u.ParentUnitId is not null).ToList();

                if (rootUnits.Count > 0)
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        _insertUnitSql,
                        rootUnits,
                        transaction,
                        cancellationToken: ct));
                }

                if (childUnits.Count > 0)
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        _insertUnitSql,
                        childUnits,
                        transaction,
                        cancellationToken: ct));
                }
            }

            // 6. Query all academics.academic_requirements where learning_structure_id = predecessorId.
            var predecessorRequirements = (await connection.QueryAsync<AcademicRequirementRow>(new CommandDefinition(
                _selectRequirementsSql,
                new { PredecessorId = predecessorId },
                transaction,
                cancellationToken: ct))).ToList();

            // 7. For requirements scoped to learning_unit, remap learning_unit_id using oldToNewUnitIds.
            // Set supersedes_id = oldReq.Id, status = 'draft', and insert cloned requirements.
            var clonedRequirements = predecessorRequirements.Select(oldReq =>
            {
                Guid? remappedUnitId = null;
                if (string.Equals(oldReq.RequirementScope, "learning_unit", StringComparison.OrdinalIgnoreCase)
                    && oldReq.LearningUnitId.HasValue
                    && oldToNewUnitIds.TryGetValue(oldReq.LearningUnitId.Value, out var mappedUnitId))
                {
                    remappedUnitId = mappedUnitId;
                }

                return new
                {
                    Id = Guid.NewGuid(),
                    oldReq.Name,
                    oldReq.RequirementScope,
                    LearningStructureId = newStructureId,
                    LearningUnitId = remappedUnitId,
                    oldReq.Level,
                    oldReq.RequirementType,
                    oldReq.Description,
                    Version = newVersion,
                    EffectiveDate = effectiveDate,
                    Status = "draft",
                    SupersedesId = (Guid?)oldReq.Id,
                    oldReq.Notes,
                    CreatedAt = now,
                    CreatedBy = currentUser
                };
            }).ToList();

            if (clonedRequirements.Count > 0)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    _insertRequirementSql,
                    clonedRequirements,
                    transaction,
                    cancellationToken: ct));
            }

            // 8. Return the new cloned structure ID on success.
            return Result.Success(newStructureId);
        }
        catch (Exception ex)
        {
            return Result.Failure<Guid>(DomainError.Failure(
                "Academic.CloningFailed",
                $"Failed to clone learning structure '{predecessorId}': {ex.Message}"));
        }
    }

    private sealed record PredecessorStructureRow(
        Guid Id,
        string Name,
        string? Description,
        Guid AcademicPeriodId,
        string AcademicPeriodLabel,
        string Version,
        DateOnly EffectiveDate,
        DateOnly? EndDate,
        string Status,
        string? Notes);

    private sealed record LearningUnitRow(
        Guid Id,
        Guid LearningStructureId,
        Guid? ParentUnitId,
        string Code,
        string Name,
        string UnitType,
        string? Level,
        string? Description,
        decimal? CreditWeight,
        string? Notes);

    private sealed record AcademicRequirementRow(
        Guid Id,
        string Name,
        string RequirementScope,
        Guid LearningStructureId,
        Guid? LearningUnitId,
        string? Level,
        string RequirementType,
        string Description,
        string Version,
        DateOnly EffectiveDate,
        DateOnly? EndDate,
        string? Notes);
}
