namespace Elsasa.Sis.Academics.Features.LearningUnits.UpdateLearningUnit;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing updates on a learning unit (ADR-004, ADR-005, P3-S08).
/// Enforces immutability guard rules: active structures allow metadata updates only (locking code, unit_type, parent_unit_id);
/// draft structures allow full editing while enforcing 2-level hierarchy and code uniqueness.
/// </summary>
public sealed class UpdateLearningUnitHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectUnitWithStructureSql = """
        SELECT
            u.id AS Id,
            u.learning_structure_id AS LearningStructureId,
            u.parent_unit_id AS ParentUnitId,
            u.code AS Code,
            u.name AS Name,
            u.unit_type AS UnitType,
            u.level AS Level,
            u.description AS Description,
            u.credit_weight AS CreditWeight,
            u.status AS Status,
            u.notes AS Notes,
            s.status AS StructureStatus
        FROM academics.learning_units u
        INNER JOIN academics.learning_structures s ON u.learning_structure_id = s.id
        WHERE u.id = @Id;
        """;

    private const string _selectParentUnitSql = """
        SELECT id AS Id, learning_structure_id AS LearningStructureId, parent_unit_id AS ParentUnitId
        FROM academics.learning_units
        WHERE id = @ParentUnitId;
        """;

    private const string _countChildUnitsSql = """
        SELECT COUNT(1)
        FROM academics.learning_units
        WHERE parent_unit_id = @Id;
        """;

    private const string _checkDuplicateCodeSql = """
        SELECT COUNT(1)
        FROM academics.learning_units
        WHERE learning_structure_id = @StructureId AND code = @Code AND id <> @Id;
        """;

    private const string _updateMetadataOnlySql = """
        UPDATE academics.learning_units
        SET
            name = @Name,
            level = @Level,
            description = @Description,
            credit_weight = @CreditWeight,
            notes = @Notes,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private const string _updateFullUnitSql = """
        UPDATE academics.learning_units
        SET
            code = @Code,
            name = @Name,
            unit_type = @UnitType,
            parent_unit_id = @ParentUnitId,
            level = @Level,
            description = @Description,
            credit_weight = @CreditWeight,
            notes = @Notes,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private sealed record ExistingUnitWithStructureRow(
        Guid Id,
        Guid LearningStructureId,
        Guid? ParentUnitId,
        string Code,
        string Name,
        string UnitType,
        string? Level,
        string? Description,
        decimal? CreditWeight,
        string Status,
        string? Notes,
        string StructureStatus);

    private sealed record ParentUnitRow(Guid Id, Guid LearningStructureId, Guid? ParentUnitId);

    public async Task<Result<UpdateLearningUnitResponse>> HandleAsync(
        Guid id,
        UpdateLearningUnitRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Fetch existing unit with owning structure status
        var existing = await connection.QuerySingleOrDefaultAsync<ExistingUnitWithStructureRow>(new CommandDefinition(
            _selectUnitWithStructureSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<UpdateLearningUnitResponse>(
                DomainError.NotFound(
                    "Academic.LearningUnitNotFound",
                    $"Learning unit with ID '{id}' was not found."));
        }

        var normalizedUnitType = request.UnitType.Trim().ToLowerInvariant();
        var updatedAt = DateTimeOffset.UtcNow;
        var updatedBy = currentUser.Username ?? currentUser.Email ?? "system";

        // 2. Immutability checks based on structure status
        if (string.Equals(existing.StructureStatus, "superseded", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(existing.StructureStatus, "archived", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<UpdateLearningUnitResponse>(
                DomainError.Conflict(
                    "Academic.ImmutableLearningStructure",
                    $"Learning units cannot be modified on a learning structure in status '{existing.StructureStatus}'."));
        }

        if (string.Equals(existing.StructureStatus, "submitted", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<UpdateLearningUnitResponse>(
                DomainError.Conflict(
                    "Academic.ImmutableLearningStructure",
                    "Cannot modify learning units on a learning structure that has already been submitted for approval."));
        }

        if (string.Equals(existing.StructureStatus, "active", StringComparison.OrdinalIgnoreCase))
        {
            // Active frameworks: allow updates to name, level, description, credit_weight, notes only.
            // Attempted mutations to code, unit_type, or parent_unit_id are rejected.
            var codeChanged = !string.Equals(request.Code, existing.Code, StringComparison.Ordinal);
            var unitTypeChanged = !string.Equals(normalizedUnitType, existing.UnitType, StringComparison.OrdinalIgnoreCase);
            var parentChanged = request.ParentUnitId != existing.ParentUnitId;

            if (codeChanged || unitTypeChanged || parentChanged)
            {
                return Result.Failure<UpdateLearningUnitResponse>(
                    DomainError.Conflict(
                        "Academic.ImmutableStructuralData",
                        "Structural modifications are locked on active curriculum frameworks. Create a new version via supersede."));
            }

            // Perform metadata-only update
            var metadataParams = new
            {
                Id = id,
                request.Name,
                request.Level,
                request.Description,
                request.CreditWeight,
                request.Notes,
                UpdatedAt = updatedAt,
                UpdatedBy = updatedBy
            };

            await connection.ExecuteAsync(new CommandDefinition(
                _updateMetadataOnlySql,
                metadataParams,
                cancellationToken: cancellationToken));

            return Result.Success(new UpdateLearningUnitResponse(
                id,
                existing.LearningStructureId,
                existing.ParentUnitId,
                existing.Code,
                request.Name,
                existing.UnitType,
                request.Level,
                request.Description,
                request.CreditWeight,
                existing.Status,
                request.Notes,
                updatedAt,
                updatedBy));
        }

        // Structure is 'draft': full editing permitted
        // Check self-parenting
        var selfParentResult = LearningUnitHierarchyRules.ValidateSelfParenting(id, request.ParentUnitId);
        if (selfParentResult.IsFailure)
        {
            return Result.Failure<UpdateLearningUnitResponse>(selfParentResult.Error);
        }

        // Check duplicate code if code was changed
        if (!string.Equals(request.Code, existing.Code, StringComparison.Ordinal))
        {
            var duplicateCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                _checkDuplicateCodeSql,
                new { StructureId = existing.LearningStructureId, request.Code, Id = id },
                cancellationToken: cancellationToken));

            if (duplicateCount > 0)
            {
                return Result.Failure<UpdateLearningUnitResponse>(
                    DomainError.Conflict(
                        "Academic.DuplicateUnitCode",
                        $"A learning unit with code '{request.Code}' already exists in this learning structure."));
            }
        }

        // If parent is specified or changed, enforce 2-level hierarchy invariant
        if (request.ParentUnitId.HasValue)
        {
            var parent = await connection.QuerySingleOrDefaultAsync<ParentUnitRow>(new CommandDefinition(
                _selectParentUnitSql,
                new { ParentUnitId = request.ParentUnitId.Value },
                cancellationToken: cancellationToken));

            if (parent is null)
            {
                return Result.Failure<UpdateLearningUnitResponse>(
                    DomainError.NotFound(
                        "Academic.ParentUnitNotFound",
                        "Specified parent learning unit does not exist."));
            }

            if (parent.LearningStructureId != existing.LearningStructureId)
            {
                return Result.Failure<UpdateLearningUnitResponse>(
                    DomainError.Validation(
                        "Academic.CrossStructureParent",
                        "Parent unit must belong to the same learning structure."));
            }

            var depthResult = LearningUnitHierarchyRules.CalculateDepth(request.ParentUnitId, parent.ParentUnitId);
            if (depthResult.IsFailure)
            {
                return Result.Failure<UpdateLearningUnitResponse>(depthResult.Error);
            }

            // Also check if this unit already has children - if it does, it cannot become a child
            var childCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                _countChildUnitsSql,
                new { Id = id },
                cancellationToken: cancellationToken));

            var childCheckResult = LearningUnitHierarchyRules.ValidateNoExistingChildrenWhenBecomingChild(childCount, request.ParentUnitId);
            if (childCheckResult.IsFailure)
            {
                return Result.Failure<UpdateLearningUnitResponse>(childCheckResult.Error);
            }
        }

        var fullParams = new
        {
            Id = id,
            request.Code,
            request.Name,
            UnitType = normalizedUnitType,
            request.ParentUnitId,
            request.Level,
            request.Description,
            request.CreditWeight,
            request.Notes,
            UpdatedAt = updatedAt,
            UpdatedBy = updatedBy
        };

        await connection.ExecuteAsync(new CommandDefinition(
            _updateFullUnitSql,
            fullParams,
            cancellationToken: cancellationToken));

        return Result.Success(new UpdateLearningUnitResponse(
            id,
            existing.LearningStructureId,
            request.ParentUnitId,
            request.Code,
            request.Name,
            normalizedUnitType,
            request.Level,
            request.Description,
            request.CreditWeight,
            existing.Status,
            request.Notes,
            updatedAt,
            updatedBy));
    }
}
