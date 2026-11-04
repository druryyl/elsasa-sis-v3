namespace Elsasa.Sis.Academics.Features.LearningStructures.GetLearningStructureById;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler retrieving a single learning structure framework by identifier with complete lineage (ADR-004, ADR-005, P3-S06).
/// </summary>
public sealed class GetLearningStructureByIdHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _selectSql = """
        SELECT
            ls.id AS Id,
            ls.name AS Name,
            ls.description AS Description,
            ls.academic_period_id AS AcademicPeriodId,
            ls.academic_period_label AS AcademicPeriodLabel,
            ls.version AS Version,
            ls.effective_date AS EffectiveDate,
            ls.end_date AS EndDate,
            ls.status AS Status,
            ls.approved_by AS ApprovedBy,
            ls.approved_date AS ApprovedDate,
            ls.supersedes_id AS SupersedesId,
            pred.name AS PredecessorName,
            pred.version AS PredecessorVersion,
            ls.notes AS Notes,
            ls.created_at AS CreatedAt,
            ls.created_by AS CreatedBy,
            ls.updated_at AS UpdatedAt,
            ls.updated_by AS UpdatedBy,
            (SELECT COUNT(1) FROM academics.learning_units u WHERE u.learning_structure_id = ls.id) AS UnitCount,
            (SELECT COUNT(1) FROM academics.academic_requirements r WHERE r.learning_structure_id = ls.id) AS RequirementCount,
            CASE WHEN ls.supersedes_id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasPredecessor
        FROM academics.learning_structures ls
        LEFT JOIN academics.learning_structures pred ON pred.id = ls.supersedes_id
        WHERE ls.id = @Id;
        """;

    /// <summary>
    /// Projects detailed framework record for the given identifier.
    /// </summary>
    /// <param name="id">The learning structure identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the framework detail response or not found error.</returns>
    public async Task<Result<LearningStructureDetailResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var structure = await connection.QuerySingleOrDefaultAsync<LearningStructureDetailResponse>(new CommandDefinition(
            _selectSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (structure is null)
        {
            return Result.Failure<LearningStructureDetailResponse>(
                DomainError.NotFound(
                    "Academics.LearningStructureNotFound",
                    $"Learning structure with ID '{id}' was not found."));
        }

        return Result.Success(structure);
    }
}
