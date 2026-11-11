namespace Elsasa.Sis.Operations.Services;

using Dapper;
using Elsasa.Sis.Contracts.Kpis;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Resolves KPI period context, prior semester comparison baselines, and available semester options
/// from <c>operations.operational_periods</c> and <c>operations.academic_years</c>.
/// </summary>
public sealed class KpiPeriodResolver(ISqlConnectionFactory connectionFactory) : IKpiPeriodResolver
{
    private const string _selectSpecificPeriodSql = """
        SELECT
            op.id AS PeriodId,
            op.academic_year_id AS AcademicYearId,
            ay.code AS AcademicYearCode,
            op.name AS PeriodName,
            op.start_date AS StartDate
        FROM operations.operational_periods op
        INNER JOIN operations.academic_years ay ON ay.id = op.academic_year_id
        WHERE op.id = @PeriodId;
        """;

    private const string _selectDefaultActivePeriodSql = """
        SELECT TOP (1)
            op.id AS PeriodId,
            op.academic_year_id AS AcademicYearId,
            ay.code AS AcademicYearCode,
            op.name AS PeriodName,
            op.start_date AS StartDate
        FROM operations.operational_periods op
        INNER JOIN operations.academic_years ay ON ay.id = op.academic_year_id
        WHERE op.period_type = 'semester'
          AND op.status = 'active'
          AND (@AcademicYearId IS NULL OR op.academic_year_id = @AcademicYearId)
        ORDER BY op.start_date DESC;
        """;

    private const string _selectFallbackPeriodForYearSql = """
        SELECT TOP (1)
            op.id AS PeriodId,
            op.academic_year_id AS AcademicYearId,
            ay.code AS AcademicYearCode,
            op.name AS PeriodName,
            op.start_date AS StartDate
        FROM operations.operational_periods op
        INNER JOIN operations.academic_years ay ON ay.id = op.academic_year_id
        WHERE op.academic_year_id = @AcademicYearId
          AND op.period_type = 'semester'
          AND op.status IN ('active', 'closed')
        ORDER BY op.start_date DESC;
        """;

    private const string _selectPriorSemesterSql = """
        SELECT TOP (1)
            op.id AS PeriodId,
            op.name AS PeriodName
        FROM operations.operational_periods op
        WHERE op.academic_year_id = @AcademicYearId
          AND op.period_type = 'semester'
          AND op.start_date < @CurrentStartDate
        ORDER BY op.start_date DESC;
        """;

    private const string _selectAvailablePeriodsSql = """
        SELECT
            op.id AS Id,
            op.period_code AS Code,
            op.name AS Name,
            op.status AS Status,
            op.academic_year_id AS AcademicYearId,
            ay.code AS AcademicYearCode
        FROM operations.operational_periods op
        INNER JOIN operations.academic_years ay ON ay.id = op.academic_year_id
        WHERE op.academic_year_id = @AcademicYearId
          AND op.period_type = 'semester'
          AND op.status IN ('active', 'closed')
        ORDER BY op.start_date ASC;
        """;

    /// <inheritdoc />
    public async Task<ResolvedKpiContextDto> ResolveContextAsync(
        GlobalKpiFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        ResolvedPeriodRow? currentPeriod = null;

        if (filter.AcademicPeriodId.HasValue)
        {
            currentPeriod = await connection.QuerySingleOrDefaultAsync<ResolvedPeriodRow>(new CommandDefinition(
                _selectSpecificPeriodSql,
                new { PeriodId = filter.AcademicPeriodId.Value },
                cancellationToken: cancellationToken));
        }

        if (currentPeriod is null && filter.AcademicYearId.HasValue)
        {
            currentPeriod = await connection.QuerySingleOrDefaultAsync<ResolvedPeriodRow>(new CommandDefinition(
                _selectDefaultActivePeriodSql,
                new { AcademicYearId = (Guid?)filter.AcademicYearId.Value },
                cancellationToken: cancellationToken));

            currentPeriod ??= await connection.QuerySingleOrDefaultAsync<ResolvedPeriodRow>(new CommandDefinition(
                _selectFallbackPeriodForYearSql,
                new { AcademicYearId = filter.AcademicYearId.Value },
                cancellationToken: cancellationToken));
        }

        currentPeriod ??= await connection.QuerySingleOrDefaultAsync<ResolvedPeriodRow>(new CommandDefinition(
            _selectDefaultActivePeriodSql,
            new { AcademicYearId = (Guid?)null },
            cancellationToken: cancellationToken));

        if (currentPeriod is null)
        {
            throw new InvalidOperationException("No active semester operational period found to resolve KPI context.");
        }

        var priorPeriod = await connection.QuerySingleOrDefaultAsync<PriorPeriodRow>(new CommandDefinition(
            _selectPriorSemesterSql,
            new
            {
                currentPeriod.AcademicYearId,
                CurrentStartDate = currentPeriod.StartDate
            },
            cancellationToken: cancellationToken));

        return new ResolvedKpiContextDto(
            AcademicYearId: currentPeriod.AcademicYearId,
            AcademicYearCode: currentPeriod.AcademicYearCode,
            AcademicPeriodId: currentPeriod.PeriodId,
            AcademicPeriodName: currentPeriod.PeriodName,
            PriorAcademicPeriodId: priorPeriod?.PeriodId,
            PriorAcademicPeriodName: priorPeriod?.PeriodName,
            Campus: NormalizeCampus(filter.Campus),
            SchoolLevel: NormalizeSchoolLevel(filter.SchoolLevel));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<KpiPeriodOptionDto>> GetAvailablePeriodsAsync(
        Guid academicYearId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var periods = await connection.QueryAsync<KpiPeriodOptionDto>(new CommandDefinition(
            _selectAvailablePeriodsSql,
            new { AcademicYearId = academicYearId },
            cancellationToken: cancellationToken));

        return periods.AsList();
    }

    private static string? NormalizeCampus(string? campus)
    {
        var normalized = NormalizeOptionalFilter(campus, "all campuses");
        return normalized?.ToLowerInvariant();
    }

    private static string? NormalizeSchoolLevel(string? schoolLevel)
    {
        var normalized = NormalizeOptionalFilter(schoolLevel, "all levels");
        return normalized?.ToUpperInvariant();
    }

    private static string? NormalizeOptionalFilter(string? value, string allLabel)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Equals("all", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals(allLabel, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return trimmed;
    }

    private sealed record ResolvedPeriodRow(
        Guid PeriodId,
        Guid AcademicYearId,
        string AcademicYearCode,
        string PeriodName,
        DateOnly StartDate);

    private sealed record PriorPeriodRow(
        Guid PeriodId,
        string PeriodName);
}
