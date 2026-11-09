namespace Elsasa.Sis.Operations.Services;

using System.Diagnostics.CodeAnalysis;
using Dapper;
using Elsasa.Sis.Contracts.Operations;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Concrete implementation of the cross-domain query contract <see cref="IOperationsCalendarQueryService"/>
/// and backwards-compatible <see cref="IOperationsModule"/> for the Operations bounded context (ADR-001, ADR-003, TD-03).
/// </summary>
public sealed class OperationsCalendarQueryService(ISqlConnectionFactory connectionFactory)
    : IOperationsCalendarQueryService, IOperationsModule
{
    private const string _selectActiveAcademicYearSql = """
        SELECT
            id AS Id,
            code AS Code,
            name AS Name,
            start_date AS StartDate,
            end_date AS EndDate,
            status AS Status
        FROM operations.academic_years
        WHERE status = 'active';
        """;

    private const string _selectOperationalPeriodByIdSql = """
        SELECT
            id AS Id,
            academic_year_id AS AcademicYearId,
            period_code AS PeriodCode,
            name AS Name,
            period_type AS PeriodType,
            start_date AS StartDate,
            end_date AS EndDate,
            parent_period_id AS ParentPeriodId,
            status AS Status
        FROM operations.operational_periods
        WHERE id = @PeriodId;
        """;

    private const string _selectAcademicYearAsPeriodByIdSql = """
        SELECT
            id AS Id,
            id AS AcademicYearId,
            code AS PeriodCode,
            name AS Name,
            'academic_year' AS PeriodType,
            start_date AS StartDate,
            end_date AS EndDate,
            CAST(NULL AS UNIQUEIDENTIFIER) AS ParentPeriodId,
            status AS Status
        FROM operations.academic_years
        WHERE id = @PeriodId;
        """;

    private const string _selectActiveAdmissionWindowsSql = """
        SELECT
            id AS Id,
            academic_year_id AS AcademicYearId,
            period_code AS PeriodCode,
            name AS Name,
            period_type AS PeriodType,
            start_date AS StartDate,
            end_date AS EndDate,
            parent_period_id AS ParentPeriodId,
            status AS Status
        FROM operations.operational_periods
        WHERE period_type = 'admission_window'
          AND status = 'active'
          AND start_date <= @Date
          AND end_date >= @Date
        ORDER BY start_date ASC, end_date ASC;
        """;

    private const string _checkActiveAcademicYearCoversDateSql = """
        SELECT COUNT(1)
        FROM operations.academic_years
        WHERE status = 'active'
          AND start_date <= @Date
          AND end_date >= @Date;
        """;

    private const string _selectCalendarEntriesForDateSql = """
        SELECT
            ce.is_instructional AS IsInstructional,
            ce.designation AS Designation
        FROM operations.calendar_entries ce
        INNER JOIN operations.school_calendars sc ON ce.calendar_id = sc.id
        INNER JOIN operations.academic_years ay ON sc.academic_year_id = ay.id
        WHERE ay.status = 'active'
          AND sc.publication_status = 'published'
          AND (
              (ce.end_date IS NULL AND ce.entry_date = @Date)
              OR
              (ce.end_date IS NOT NULL AND ce.entry_date <= @Date AND ce.end_date >= @Date)
          )
          AND (ce.applies_to = 'All' OR @Campus = 'All' OR ce.applies_to = @Campus)
        ORDER BY ce.is_instructional ASC;
        """;

    /// <inheritdoc />
    public async Task<Result<ActiveAcademicYearDto?>> GetActiveAcademicYearAsync(CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var activeYear = await connection.QuerySingleOrDefaultAsync<ActiveAcademicYearDto>(new CommandDefinition(
                _selectActiveAcademicYearSql,
                cancellationToken: ct));

            return Result.Success(activeYear);
        }
        catch (Exception ex)
        {
            return Result.Failure<ActiveAcademicYearDto?>(
                DomainError.Failure(
                    "Operations.ActiveAcademicYearQueryFailed",
                    $"Failed to query active academic year: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<OperationalPeriodDto?>> GetOperationalPeriodByIdAsync(
        Guid periodId,
        CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var period = await connection.QuerySingleOrDefaultAsync<OperationalPeriodDto>(new CommandDefinition(
                _selectOperationalPeriodByIdSql,
                new { PeriodId = periodId },
                cancellationToken: ct));

            if (period is null)
            {
                period = await connection.QuerySingleOrDefaultAsync<OperationalPeriodDto>(new CommandDefinition(
                    _selectAcademicYearAsPeriodByIdSql,
                    new { PeriodId = periodId },
                    cancellationToken: ct));
            }

            return Result.Success(period);
        }
        catch (Exception ex)
        {
            return Result.Failure<OperationalPeriodDto?>(
                DomainError.Failure(
                    "Operations.OperationalPeriodQueryFailed",
                    $"Failed to query operational period '{periodId}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Parameter name 'date' matches interface specification.")]
    public async Task<Result<IReadOnlyList<OperationalPeriodDto>>> GetActiveAdmissionWindowsAsync(
        DateOnly date,
        CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var windows = await connection.QueryAsync<OperationalPeriodDto>(new CommandDefinition(
                _selectActiveAdmissionWindowsSql,
                new { Date = date },
                cancellationToken: ct));

            return Result.Success<IReadOnlyList<OperationalPeriodDto>>(windows.AsList());
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<OperationalPeriodDto>>(
                DomainError.Failure(
                    "Operations.AdmissionWindowsQueryFailed",
                    $"Failed to query active admission windows for date '{date:yyyy-MM-dd}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Parameter name 'date' matches interface specification.")]
    public async Task<Result<bool>> IsInstructionalDayAsync(
        DateOnly date,
        string campus = "All",
        CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            // Verify active academic year covers the requested date
            var ayCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                _checkActiveAcademicYearCoversDateSql,
                new { Date = date },
                cancellationToken: ct));

            if (ayCount == 0)
            {
                return Result.Success(false);
            }

            // Baseline: Monday through Friday are instructional, Saturday & Sunday are non-instructional
            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var isInstructional = !isWeekend;

            // Check published school calendar entries for explicit overrides (holidays, closures, special instructional days)
            var entries = (await connection.QueryAsync<CalendarEntryCheckRow>(new CommandDefinition(
                _selectCalendarEntriesForDateSql,
                new { Date = date, Campus = campus },
                cancellationToken: ct))).ToList();

            if (entries.Count > 0)
            {
                // Any non-instructional day entry (holiday, closure, staff day) takes precedence
                var hasNonInstructional = entries.Any(e => !e.IsInstructional);
                isInstructional = !hasNonInstructional;
            }

            return Result.Success(isInstructional);
        }
        catch (Exception ex)
        {
            return Result.Failure<bool>(
                DomainError.Failure(
                    "Operations.InstructionalDayEvaluationFailed",
                    $"Failed to evaluate instructional day for date '{date:yyyy-MM-dd}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    async Task<Result<bool>> IOperationsModule.IsActiveAcademicPeriodAsync(
        Guid periodId,
        CancellationToken ct)
    {
        var periodResult = await GetOperationalPeriodByIdAsync(periodId, ct);
        if (!periodResult.IsSuccess)
        {
            return Result.Failure<bool>(periodResult.Error);
        }

        var isActive = periodResult.Value is not null && string.Equals(periodResult.Value.Status, "active", StringComparison.OrdinalIgnoreCase);
        return Result.Success(isActive);
    }

    /// <inheritdoc />
    async Task<Result<Guid?>> IOperationsModule.GetActiveAcademicYearIdAsync(CancellationToken ct)
    {
        var yearResult = await GetActiveAcademicYearAsync(ct);
        if (!yearResult.IsSuccess)
        {
            return Result.Failure<Guid?>(yearResult.Error);
        }

        return Result.Success(yearResult.Value?.Id);
    }

    private sealed record CalendarEntryCheckRow(
        bool IsInstructional,
        string Designation);
}
