namespace Elsasa.Sis.Academics.Features.AcademicRequirements.UpdateAcademicRequirement;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing updates on academic requirements (ADR-004, ADR-005, P3-S09).
/// Enforces immutability guard rules: active structures permit updates to metadata (name, description, notes) only;
/// draft structures permit full editing; superseded/archived structures strictly reject modifications.
/// </summary>
public sealed class UpdateAcademicRequirementHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectRequirementWithStructureSql = """
        SELECT
            ar.id AS Id,
            ar.name AS Name,
            ar.requirement_scope AS RequirementScope,
            ar.learning_structure_id AS LearningStructureId,
            ar.learning_unit_id AS LearningUnitId,
            ar.level AS Level,
            ar.requirement_type AS RequirementType,
            ar.description AS Description,
            ar.version AS Version,
            ar.effective_date AS EffectiveDate,
            ar.end_date AS EndDate,
            ar.status AS Status,
            ar.notes AS Notes,
            ls.status AS LearningStructureStatus
        FROM academics.academic_requirements ar
        INNER JOIN academics.learning_structures ls ON ar.learning_structure_id = ls.id
        WHERE ar.id = @Id;
        """;

    private const string _selectUnitSql = """
        SELECT id AS Id, learning_structure_id AS LearningStructureId
        FROM academics.learning_units
        WHERE id = @LearningUnitId;
        """;

    private const string _updateMetadataSql = """
        UPDATE academics.academic_requirements
        SET
            name = @Name,
            description = @Description,
            notes = @Notes,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private const string _updateFullSql = """
        UPDATE academics.academic_requirements
        SET
            name = @Name,
            requirement_scope = @RequirementScope,
            learning_unit_id = @LearningUnitId,
            level = @Level,
            requirement_type = @RequirementType,
            description = @Description,
            version = @Version,
            effective_date = @EffectiveDate,
            end_date = @EndDate,
            notes = @Notes,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private sealed record RequirementWithStructureRow(
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
        string Status,
        string? Notes,
        string LearningStructureStatus);

    private sealed record UnitRow(Guid Id, Guid LearningStructureId);

    /// <summary>
    /// Executes the academic requirement update.
    /// </summary>
    public async Task<Result<UpdateAcademicRequirementResponse>> HandleAsync(
        Guid id,
        UpdateAcademicRequirementRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Fetch existing requirement and owning structure status
        var existing = await connection.QuerySingleOrDefaultAsync<RequirementWithStructureRow>(new CommandDefinition(
            _selectRequirementWithStructureSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<UpdateAcademicRequirementResponse>(
                DomainError.NotFound(
                    "Academics.AcademicRequirementNotFound",
                    $"Academic requirement with ID '{id}' was not found."));
        }

        var updatedAt = DateTimeOffset.UtcNow;
        var updatedBy = currentUser.Username ?? currentUser.Email ?? "system";

        // 2. Enforce status immutability guards
        if (string.Equals(existing.LearningStructureStatus, "superseded", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(existing.LearningStructureStatus, "archived", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<UpdateAcademicRequirementResponse>(
                DomainError.Conflict(
                    "Academics.ImmutableAcademicRequirement",
                    $"Academic requirement cannot be modified because owning learning structure is in '{existing.LearningStructureStatus}' status."));
        }

        if (string.Equals(existing.LearningStructureStatus, "active", StringComparison.OrdinalIgnoreCase))
        {
            // If active: permits updates to name, description, notes only.
            var hasStructuralChange =
                (request.RequirementScope is not null && !string.Equals(request.RequirementScope.Trim(), existing.RequirementScope, StringComparison.OrdinalIgnoreCase)) ||
                (request.RequirementType is not null && !string.Equals(request.RequirementType.Trim(), existing.RequirementType, StringComparison.OrdinalIgnoreCase)) ||
                (request.LearningUnitId.HasValue && request.LearningUnitId != existing.LearningUnitId) ||
                (request.Level is not null && !string.Equals(request.Level.Trim(), existing.Level, StringComparison.OrdinalIgnoreCase)) ||
                (request.Version is not null && !string.Equals(request.Version.Trim(), existing.Version, StringComparison.OrdinalIgnoreCase)) ||
                (request.EffectiveDate.HasValue && request.EffectiveDate != existing.EffectiveDate);

            if (hasStructuralChange)
            {
                return Result.Failure<UpdateAcademicRequirementResponse>(
                    DomainError.Conflict(
                        "Academics.ImmutableStructuralData",
                        "Structural modifications are locked on active curriculum frameworks. Only metadata (name, description, notes) may be updated. Create a new version via supersede."));
            }

            var metadataParams = new
            {
                Id = id,
                request.Name,
                request.Description,
                request.Notes,
                UpdatedAt = updatedAt,
                UpdatedBy = updatedBy
            };

            await connection.ExecuteAsync(new CommandDefinition(
                _updateMetadataSql,
                metadataParams,
                cancellationToken: cancellationToken));

            return Result.Success(new UpdateAcademicRequirementResponse(
                id,
                request.Name,
                existing.RequirementScope,
                existing.LearningStructureId,
                existing.LearningUnitId,
                existing.Level,
                existing.RequirementType,
                request.Description,
                existing.Version,
                existing.EffectiveDate,
                existing.EndDate,
                existing.Status,
                request.Notes,
                updatedAt,
                updatedBy));
        }

        if (!string.Equals(existing.LearningStructureStatus, "draft", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<UpdateAcademicRequirementResponse>(
                DomainError.Conflict(
                    "Academics.ImmutableAcademicRequirement",
                    $"Academic requirement cannot be modified because owning learning structure is in '{existing.LearningStructureStatus}' status."));
        }

        // 3. Draft structure status: full editing permitted
        var targetScope = !string.IsNullOrWhiteSpace(request.RequirementScope)
            ? request.RequirementScope.Trim().ToLowerInvariant()
            : existing.RequirementScope;

        var targetType = !string.IsNullOrWhiteSpace(request.RequirementType)
            ? request.RequirementType.Trim().ToLowerInvariant()
            : existing.RequirementType;

        var targetVersion = !string.IsNullOrWhiteSpace(request.Version)
            ? request.Version.Trim()
            : existing.Version;

        var targetEffectiveDate = request.EffectiveDate ?? existing.EffectiveDate;
        var targetEndDate = request.EndDate ?? existing.EndDate;

        Guid? targetUnitId = null;
        string? targetLevel = null;

        if (targetScope == "learning_unit")
        {
            var candidateUnitId = request.LearningUnitId ?? existing.LearningUnitId;
            if (!candidateUnitId.HasValue || candidateUnitId.Value == Guid.Empty)
            {
                return Result.Failure<UpdateAcademicRequirementResponse>(
                    DomainError.Validation(
                        "Academics.LearningUnitRequired",
                        "Learning unit ID is required when requirement scope is 'learning_unit'."));
            }

            var unit = await connection.QuerySingleOrDefaultAsync<UnitRow>(new CommandDefinition(
                _selectUnitSql,
                new { LearningUnitId = candidateUnitId.Value },
                cancellationToken: cancellationToken));

            if (unit is null)
            {
                return Result.Failure<UpdateAcademicRequirementResponse>(
                    DomainError.NotFound(
                        "Academics.LearningUnitNotFound",
                        $"Learning unit with ID '{candidateUnitId.Value}' was not found."));
            }

            if (unit.LearningStructureId != existing.LearningStructureId)
            {
                return Result.Failure<UpdateAcademicRequirementResponse>(
                    DomainError.Validation(
                        "Academics.CrossStructureLearningUnit",
                        "Learning unit must belong to the specified learning structure."));
            }

            targetUnitId = candidateUnitId.Value;
        }
        else if (targetScope == "level")
        {
            var candidateLevel = request.Level ?? existing.Level;
            if (string.IsNullOrWhiteSpace(candidateLevel))
            {
                return Result.Failure<UpdateAcademicRequirementResponse>(
                    DomainError.Validation(
                        "Academics.LevelRequired",
                        "Level is required when requirement scope is 'level'."));
            }

            targetLevel = candidateLevel.Trim();
        }

        var fullParams = new
        {
            Id = id,
            request.Name,
            RequirementScope = targetScope,
            LearningUnitId = targetUnitId,
            Level = targetLevel,
            RequirementType = targetType,
            request.Description,
            Version = targetVersion,
            EffectiveDate = targetEffectiveDate,
            EndDate = targetEndDate,
            request.Notes,
            UpdatedAt = updatedAt,
            UpdatedBy = updatedBy
        };

        await connection.ExecuteAsync(new CommandDefinition(
            _updateFullSql,
            fullParams,
            cancellationToken: cancellationToken));

        return Result.Success(new UpdateAcademicRequirementResponse(
            id,
            request.Name,
            targetScope,
            existing.LearningStructureId,
            targetUnitId,
            targetLevel,
            targetType,
            request.Description,
            targetVersion,
            targetEffectiveDate,
            targetEndDate,
            existing.Status,
            request.Notes,
            updatedAt,
            updatedBy));
    }
}
