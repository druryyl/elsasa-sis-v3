namespace Elsasa.Sis.Academics.Features.AcademicRequirements.GetAcademicRequirementById;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler inspecting a single academic requirement by identifier (ADR-004, ADR-005, P3-S09).
/// </summary>
public sealed class GetAcademicRequirementByIdHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _selectRequirementDetailSql = """
        SELECT
            ar.id AS Id,
            ar.name AS Name,
            ar.requirement_scope AS RequirementScope,
            ar.learning_structure_id AS LearningStructureId,
            ls.name AS LearningStructureName,
            ls.version AS LearningStructureVersion,
            ls.status AS LearningStructureStatus,
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
        INNER JOIN academics.learning_structures ls ON ar.learning_structure_id = ls.id
        LEFT JOIN academics.learning_units lu ON ar.learning_unit_id = lu.id
        WHERE ar.id = @Id;
        """;

    /// <summary>
    /// Executes the detail requirement retrieval.
    /// </summary>
    public async Task<Result<AcademicRequirementDetailResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var requirement = await connection.QuerySingleOrDefaultAsync<AcademicRequirementDetailResponse>(new CommandDefinition(
            _selectRequirementDetailSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (requirement is null)
        {
            return Result.Failure<AcademicRequirementDetailResponse>(
                DomainError.NotFound(
                    "Academics.AcademicRequirementNotFound",
                    $"Academic requirement with ID '{id}' was not found."));
        }

        return Result.Success(requirement);
    }
}
