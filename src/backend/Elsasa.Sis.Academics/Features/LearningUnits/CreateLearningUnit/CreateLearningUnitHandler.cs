namespace Elsasa.Sis.Academics.Features.LearningUnits.CreateLearningUnit;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler creating a learning unit within a learning structure framework (ADR-004, ADR-005, P3-S08).
/// Enforces the 2-level hierarchy invariant and code uniqueness within the structure.
/// </summary>
public sealed class CreateLearningUnitHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectStructureStatusSql = """
        SELECT id AS Id, status AS Status
        FROM academics.learning_structures
        WHERE id = @StructureId;
        """;

    private const string _checkDuplicateCodeSql = """
        SELECT COUNT(1)
        FROM academics.learning_units
        WHERE learning_structure_id = @StructureId AND code = @Code;
        """;

    private const string _selectParentUnitSql = """
        SELECT id AS Id, learning_structure_id AS LearningStructureId, parent_unit_id AS ParentUnitId
        FROM academics.learning_units
        WHERE id = @ParentUnitId;
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
            created_by
        )
        VALUES (
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
            @CreatedBy
        );
        """;

    private sealed record StructureStatusRow(Guid Id, string Status);
    private sealed record ParentUnitRow(Guid Id, Guid LearningStructureId, Guid? ParentUnitId);

    public async Task<Result<CreateLearningUnitResponse>> HandleAsync(
        Guid structureId,
        CreateLearningUnitRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify structure exists
        var structure = await connection.QuerySingleOrDefaultAsync<StructureStatusRow>(new CommandDefinition(
            _selectStructureStatusSql,
            new { StructureId = structureId },
            cancellationToken: cancellationToken));

        if (structure is null)
        {
            return Result.Failure<CreateLearningUnitResponse>(
                DomainError.NotFound(
                    "Academic.LearningStructureNotFound",
                    $"Learning structure with ID '{structureId}' was not found."));
        }

        // 2. Enforce structure status rules
        if (string.Equals(structure.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<CreateLearningUnitResponse>(
                DomainError.Conflict(
                    "Academic.ImmutableStructuralData",
                    "Structural modifications are locked on active curriculum frameworks. Create a new version via supersede."));
        }

        if (!string.Equals(structure.Status, "draft", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<CreateLearningUnitResponse>(
                DomainError.Conflict(
                    "Academic.ImmutableLearningStructure",
                    $"Learning units can only be added to draft learning structures. Current status is '{structure.Status}'."));
        }

        // 3. Unique unit code within owning structure
        var duplicateCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _checkDuplicateCodeSql,
            new { StructureId = structureId, request.Code },
            cancellationToken: cancellationToken));

        if (duplicateCount > 0)
        {
            return Result.Failure<CreateLearningUnitResponse>(
                DomainError.Conflict(
                    "Academic.DuplicateUnitCode",
                    $"A learning unit with code '{request.Code}' already exists in this learning structure."));
        }

        // 4. Enforce 2-Level Hierarchy Invariant
        if (request.ParentUnitId.HasValue)
        {
            var parent = await connection.QuerySingleOrDefaultAsync<ParentUnitRow>(new CommandDefinition(
                _selectParentUnitSql,
                new { ParentUnitId = request.ParentUnitId.Value },
                cancellationToken: cancellationToken));

            if (parent is null)
            {
                return Result.Failure<CreateLearningUnitResponse>(
                    DomainError.NotFound(
                        "Academic.ParentUnitNotFound",
                        "Specified parent learning unit does not exist."));
            }

            if (parent.LearningStructureId != structureId)
            {
                return Result.Failure<CreateLearningUnitResponse>(
                    DomainError.Validation(
                        "Academic.CrossStructureParent",
                        "Parent unit must belong to the same learning structure."));
            }

            var depthResult = LearningUnitHierarchyRules.CalculateDepth(request.ParentUnitId, parent.ParentUnitId);
            if (depthResult.IsFailure)
            {
                return Result.Failure<CreateLearningUnitResponse>(depthResult.Error);
            }
        }

        // 5. Persist unit
        var unitId = Guid.NewGuid();
        const string initialStatus = "draft";
        var createdAt = DateTimeOffset.UtcNow;
        var createdBy = currentUser.Username ?? currentUser.Email ?? "system";
        var normalizedUnitType = request.UnitType.Trim().ToLowerInvariant();

        var parameters = new
        {
            Id = unitId,
            LearningStructureId = structureId,
            request.ParentUnitId,
            request.Code,
            request.Name,
            UnitType = normalizedUnitType,
            request.Level,
            request.Description,
            request.CreditWeight,
            Status = initialStatus,
            request.Notes,
            CreatedAt = createdAt,
            CreatedBy = createdBy
        };

        await connection.ExecuteAsync(new CommandDefinition(
            _insertUnitSql,
            parameters,
            cancellationToken: cancellationToken));

        return Result.Success(new CreateLearningUnitResponse(
            unitId,
            structureId,
            request.ParentUnitId,
            request.Code,
            request.Name,
            normalizedUnitType,
            request.Level,
            request.Description,
            request.CreditWeight,
            initialStatus,
            request.Notes,
            createdAt,
            createdBy));
    }
}
