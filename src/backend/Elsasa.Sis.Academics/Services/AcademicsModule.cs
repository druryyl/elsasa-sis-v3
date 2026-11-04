namespace Elsasa.Sis.Academics.Services;

using Dapper;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Authoritative implementation of the synchronous Academics cross-domain query contract (ADR-001, ADR-003, TD-01, TD-09).
/// Provides parameterized read operations for active curriculum structures, learning units, and academic requirements.
/// </summary>
public sealed class AcademicsModule(ISqlConnectionFactory connectionFactory) : IAcademicsModule
{
    static AcademicsModule()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectActiveLearningStructureSql = """
        SELECT
            id AS Id,
            name AS Name,
            academic_period_id AS AcademicPeriodId,
            academic_period_label AS AcademicPeriodLabel,
            version AS Version,
            effective_date AS EffectiveDate,
            status AS Status,
            approved_by AS ApprovedBy,
            approved_date AS ApprovedDate
        FROM academics.learning_structures
        WHERE academic_period_id = @AcademicPeriodId
          AND status = 'active';
        """;

    private const string _selectLearningUnitsByStructureSql = """
        SELECT
            id AS Id,
            learning_structure_id AS LearningStructureId,
            parent_unit_id AS ParentUnitId,
            code AS Code,
            name AS Name,
            unit_type AS UnitType,
            level AS Level,
            credit_weight AS CreditWeight,
            status AS Status
        FROM academics.learning_units
        WHERE learning_structure_id = @LearningStructureId
        ORDER BY code ASC;
        """;

    private const string _selectLearningUnitByIdSql = """
        SELECT
            id AS Id,
            learning_structure_id AS LearningStructureId,
            parent_unit_id AS ParentUnitId,
            code AS Code,
            name AS Name,
            unit_type AS UnitType,
            level AS Level,
            credit_weight AS CreditWeight,
            status AS Status
        FROM academics.learning_units
        WHERE id = @LearningUnitId;
        """;

    private const string _selectAcademicRequirementsByStructureSql = """
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
            status AS Status
        FROM academics.academic_requirements
        WHERE learning_structure_id = @LearningStructureId
        ORDER BY requirement_scope ASC, name ASC;
        """;

    /// <inheritdoc />
    public async Task<Result<ActiveLearningStructureDto?>> GetActiveLearningStructureForPeriodAsync(
        Guid academicPeriodId,
        CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var activeStructure = await connection.QuerySingleOrDefaultAsync<ActiveLearningStructureDto>(new CommandDefinition(
                _selectActiveLearningStructureSql,
                new { AcademicPeriodId = academicPeriodId },
                cancellationToken: ct));

            return Result.Success(activeStructure);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<ActiveLearningStructureDto?>(
                DomainError.Failure(
                    "Academics.ActiveLearningStructureQueryFailed",
                    $"Failed to query active learning structure for academic period '{academicPeriodId}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LearningUnitDto>>> GetLearningUnitsByStructureAsync(
        Guid learningStructureId,
        CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var units = (await connection.QueryAsync<LearningUnitDto>(new CommandDefinition(
                _selectLearningUnitsByStructureSql,
                new { LearningStructureId = learningStructureId },
                cancellationToken: ct))).AsList();

            return Result.Success<IReadOnlyList<LearningUnitDto>>(units);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<LearningUnitDto>>(
                DomainError.Failure(
                    "Academics.LearningUnitsQueryFailed",
                    $"Failed to query learning units for structure '{learningStructureId}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<LearningUnitDto?>> GetLearningUnitByIdAsync(
        Guid learningUnitId,
        CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var unit = await connection.QuerySingleOrDefaultAsync<LearningUnitDto>(new CommandDefinition(
                _selectLearningUnitByIdSql,
                new { LearningUnitId = learningUnitId },
                cancellationToken: ct));

            return Result.Success(unit);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<LearningUnitDto?>(
                DomainError.Failure(
                    "Academics.LearningUnitQueryFailed",
                    $"Failed to query learning unit '{learningUnitId}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AcademicRequirementDto>>> GetAcademicRequirementsByStructureAsync(
        Guid learningStructureId,
        CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var requirements = (await connection.QueryAsync<AcademicRequirementDto>(new CommandDefinition(
                _selectAcademicRequirementsByStructureSql,
                new { LearningStructureId = learningStructureId },
                cancellationToken: ct))).AsList();

            return Result.Success<IReadOnlyList<AcademicRequirementDto>>(requirements);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<AcademicRequirementDto>>(
                DomainError.Failure(
                    "Academics.AcademicRequirementsQueryFailed",
                    $"Failed to query academic requirements for structure '{learningStructureId}': {ex.Message}"));
        }
    }
}
