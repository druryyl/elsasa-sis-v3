namespace Elsasa.Sis.Academics.Features.AcademicRequirements.CreateAcademicRequirement;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler creating a new academic requirement under a learning structure (ADR-004, ADR-005, P3-S09).
/// Validates owning structure status, requirement scope constraints, and persists the record in <c>academics.academic_requirements</c>.
/// </summary>
public sealed class CreateAcademicRequirementHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectStructureSql = """
        SELECT id AS Id, status AS Status
        FROM academics.learning_structures
        WHERE id = @StructureId;
        """;

    private const string _selectUnitSql = """
        SELECT id AS Id, learning_structure_id AS LearningStructureId
        FROM academics.learning_units
        WHERE id = @LearningUnitId;
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
            notes,
            created_at,
            created_by
        )
        VALUES (
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
            @EndDate,
            @Status,
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private sealed record StructureRow(Guid Id, string Status);
    private sealed record UnitRow(Guid Id, Guid LearningStructureId);

    /// <summary>
    /// Executes academic requirement creation.
    /// </summary>
    public async Task<Result<CreateAcademicRequirementResponse>> HandleAsync(
        Guid structureId,
        CreateAcademicRequirementRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify owning learning structure exists and is in draft status
        var structure = await connection.QuerySingleOrDefaultAsync<StructureRow>(new CommandDefinition(
            _selectStructureSql,
            new { StructureId = structureId },
            cancellationToken: cancellationToken));

        if (structure is null)
        {
            return Result.Failure<CreateAcademicRequirementResponse>(
                DomainError.NotFound(
                    "Academics.LearningStructureNotFound",
                    $"Learning structure with ID '{structureId}' was not found."));
        }

        if (!string.Equals(structure.Status, "draft", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<CreateAcademicRequirementResponse>(
                DomainError.Conflict(
                    "Academics.ImmutableLearningStructure",
                    $"Academic requirements can only be created for learning structures in 'draft' status. Current status is '{structure.Status}'."));
        }

        // 2. Validate scope-specific invariants
        var normalizedScope = request.RequirementScope.Trim().ToLowerInvariant();
        var normalizedType = request.RequirementType.Trim().ToLowerInvariant();

        Guid? learningUnitId = null;
        string? level = null;

        if (normalizedScope == "learning_unit")
        {
            if (!request.LearningUnitId.HasValue || request.LearningUnitId.Value == Guid.Empty)
            {
                return Result.Failure<CreateAcademicRequirementResponse>(
                    DomainError.Validation(
                        "Academics.LearningUnitRequired",
                        "Learning unit ID is required when requirement scope is 'learning_unit'."));
            }

            var unit = await connection.QuerySingleOrDefaultAsync<UnitRow>(new CommandDefinition(
                _selectUnitSql,
                new { LearningUnitId = request.LearningUnitId.Value },
                cancellationToken: cancellationToken));

            if (unit is null)
            {
                return Result.Failure<CreateAcademicRequirementResponse>(
                    DomainError.NotFound(
                        "Academics.LearningUnitNotFound",
                        $"Learning unit with ID '{request.LearningUnitId.Value}' was not found."));
            }

            if (unit.LearningStructureId != structureId)
            {
                return Result.Failure<CreateAcademicRequirementResponse>(
                    DomainError.Validation(
                        "Academics.CrossStructureLearningUnit",
                        "Learning unit must belong to the specified learning structure."));
            }

            learningUnitId = request.LearningUnitId.Value;
        }
        else if (normalizedScope == "level")
        {
            if (string.IsNullOrWhiteSpace(request.Level))
            {
                return Result.Failure<CreateAcademicRequirementResponse>(
                    DomainError.Validation(
                        "Academics.LevelRequired",
                        "Level is required when requirement scope is 'level'."));
            }

            level = request.Level.Trim();
        }

        // 3. Persist requirement with draft status
        var requirementId = Guid.NewGuid();
        const string initialStatus = "draft";
        var createdAt = DateTimeOffset.UtcNow;
        var createdBy = currentUser.Username ?? currentUser.Email ?? "system";

        var parameters = new
        {
            Id = requirementId,
            request.Name,
            RequirementScope = normalizedScope,
            LearningStructureId = structureId,
            LearningUnitId = learningUnitId,
            Level = level,
            RequirementType = normalizedType,
            request.Description,
            request.Version,
            request.EffectiveDate,
            request.EndDate,
            Status = initialStatus,
            request.Notes,
            CreatedAt = createdAt,
            CreatedBy = createdBy
        };

        await connection.ExecuteAsync(new CommandDefinition(
            _insertRequirementSql,
            parameters,
            cancellationToken: cancellationToken));

        return Result.Success(new CreateAcademicRequirementResponse(
            requirementId,
            request.Name,
            normalizedScope,
            structureId,
            learningUnitId,
            level,
            normalizedType,
            request.Description,
            request.Version,
            request.EffectiveDate,
            request.EndDate,
            initialStatus,
            request.Notes,
            createdAt,
            createdBy));
    }
}
