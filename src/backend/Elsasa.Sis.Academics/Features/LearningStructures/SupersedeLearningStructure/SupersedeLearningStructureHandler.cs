namespace Elsasa.Sis.Academics.Features.LearningStructures.SupersedeLearningStructure;

using Dapper;
using Elsasa.Sis.Academics.Features.LearningStructures.GetLearningStructureById;
using Elsasa.Sis.Academics.Services;
using Elsasa.Sis.Contracts.Operations;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler branching an active curriculum structure into a draft framework via transactional deep clone (P3-S07, GAP-003, TD-03).
/// </summary>
public sealed class SupersedeLearningStructureHandler(
    ISqlConnectionFactory connectionFactory,
    ICurriculumCloningService cloningService,
    IOperationsCalendarQueryService calendarQueryService,
    ICurrentUser currentUser)
{
    static SupersedeLearningStructureHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectStructureDetailSql = """
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
            (SELECT COUNT(1) FROM academics.learning_units lu WHERE lu.learning_structure_id = ls.id) AS UnitCount,
            (SELECT COUNT(1) FROM academics.academic_requirements ar WHERE ar.learning_structure_id = ls.id) AS RequirementCount,
            CASE WHEN ls.supersedes_id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasPredecessor
        FROM academics.learning_structures ls
        LEFT JOIN academics.learning_structures pred ON ls.supersedes_id = pred.id
        WHERE ls.id = @Id;
        """;

    /// <summary>
    /// Executes the deep clone-on-supersede versioning workflow.
    /// </summary>
    public async Task<Result<LearningStructureDetailResponse>> HandleAsync(
        Guid predecessorId,
        SupersedeStructureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string? periodLabel = request.AcademicPeriodLabel;

        // 1. If academic period is overridden, verify target period exists via cross-domain query contract
        if (request.AcademicPeriodId.HasValue)
        {
            var periodResult = await calendarQueryService.GetOperationalPeriodByIdAsync(
                request.AcademicPeriodId.Value,
                cancellationToken);

            if (!periodResult.IsSuccess || periodResult.Value is null)
            {
                return Result.Failure<LearningStructureDetailResponse>(
                    DomainError.NotFound(
                        "Academics.AcademicPeriodNotFound",
                        $"Operational academic period with ID '{request.AcademicPeriodId.Value}' was not found."));
            }

            if (string.IsNullOrWhiteSpace(periodLabel))
            {
                var period = periodResult.Value;
                periodLabel = !string.IsNullOrWhiteSpace(period.Name) ? period.Name : period.PeriodCode;
            }
        }

        var auditUser = currentUser.Username ?? currentUser.Email ?? "system";

        // 2. Open connection and begin transaction for deep cloning
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // 3. Invoke deep clone versioning engine (P2-S05)
        var cloneResult = await cloningService.CloneStructureToDraftAsync(
            predecessorId,
            request.NewVersion,
            request.EffectiveDate,
            request.AcademicPeriodId,
            periodLabel,
            auditUser,
            connection,
            transaction,
            cancellationToken);

        if (!cloneResult.IsSuccess)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<LearningStructureDetailResponse>(cloneResult.Error);
        }

        var newStructureId = cloneResult.Value;

        // 4. Query cloned structure details inside transaction
        var newStructure = await connection.QuerySingleOrDefaultAsync<LearningStructureDetailResponse>(new CommandDefinition(
            _selectStructureDetailSql,
            new { Id = newStructureId },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        if (newStructure is null)
        {
            return Result.Failure<LearningStructureDetailResponse>(
                DomainError.NotFound(
                    "Academics.LearningStructureNotFound",
                    $"Cloned learning structure with ID '{newStructureId}' could not be loaded."));
        }

        return Result.Success(newStructure);
    }
}
