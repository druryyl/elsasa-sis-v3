namespace Elsasa.Sis.Academics.Features.AcademicRequirements.GetAcademicRequirements;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying academic requirements for a learning structure with scope and type filters (ADR-004, ADR-005, P3-S09).
/// </summary>
public sealed class GetAcademicRequirementsHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _selectStructureExistsSql = """
        SELECT 1
        FROM academics.learning_structures
        WHERE id = @StructureId;
        """;

    private const string _selectRequirementsSql = """
        SELECT
            ar.id AS Id,
            ar.name AS Name,
            ar.requirement_scope AS RequirementScope,
            ar.learning_structure_id AS LearningStructureId,
            ar.learning_unit_id AS LearningUnitId,
            lu.code AS LearningUnitCode,
            lu.name AS LearningUnitName,
            ar.level AS Level,
            ar.requirement_type AS RequirementType,
            ar.description AS Description,
            ar.version AS Version,
            ar.effective_date AS EffectiveDate,
            ar.end_date AS EndDate,
            ar.status AS Status,
            ar.approved_by AS ApprovedBy,
            ar.approved_date AS ApprovedDate,
            ar.supersedes_id AS SupersedesId,
            ar.notes AS Notes,
            ar.created_at AS CreatedAt,
            ar.created_by AS CreatedBy,
            ar.updated_at AS UpdatedAt,
            ar.updated_by AS UpdatedBy
        FROM academics.academic_requirements ar
        LEFT JOIN academics.learning_units lu ON ar.learning_unit_id = lu.id
        WHERE ar.learning_structure_id = @StructureId
          AND (@Scope IS NULL OR ar.requirement_scope = @Scope)
          AND (@Type IS NULL OR ar.requirement_type = @Type)
        ORDER BY ar.requirement_scope ASC, ar.name ASC;
        """;

    /// <summary>
    /// Executes the requirements inquiry.
    /// </summary>
    public async Task<Result<GetAcademicRequirementsResponse>> HandleAsync(
        Guid structureId,
        string? scope = null,
        string? type = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify target learning structure exists
        var exists = await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
            _selectStructureExistsSql,
            new { StructureId = structureId },
            cancellationToken: cancellationToken));

        if (!exists.HasValue)
        {
            return Result.Failure<GetAcademicRequirementsResponse>(
                DomainError.NotFound(
                    "Academics.LearningStructureNotFound",
                    $"Learning structure with ID '{structureId}' was not found."));
        }

        // 2. Query filtered requirements
        var normalizedScope = string.IsNullOrWhiteSpace(scope) ? null : scope.Trim().ToLowerInvariant();
        var normalizedType = string.IsNullOrWhiteSpace(type) ? null : type.Trim().ToLowerInvariant();

        var requirements = (await connection.QueryAsync<AcademicRequirementListItemDto>(new CommandDefinition(
            _selectRequirementsSql,
            new
            {
                StructureId = structureId,
                Scope = normalizedScope,
                Type = normalizedType
            },
            cancellationToken: cancellationToken))).AsList();

        return Result.Success(new GetAcademicRequirementsResponse(structureId, requirements));
    }
}
