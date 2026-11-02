namespace Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.ApplyAcademicYearPreset;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing the institutional preset generation for standard Indonesian 2-semester periods (ADR-004, ADR-005).
/// </summary>
public sealed class ApplyAcademicYearPresetHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectAcademicYearSql = """
        SELECT
            id AS Id,
            code AS Code,
            name AS Name,
            start_date AS StartDate,
            end_date AS EndDate,
            status AS Status
        FROM operations.academic_years
        WHERE id = @Id;
        """;

    private const string _checkExistingPeriodsSql = """
        SELECT COUNT(1)
        FROM operations.operational_periods
        WHERE academic_year_id = @AcademicYearId;
        """;

    private const string _insertPeriodSql = """
        INSERT INTO operations.operational_periods (
            id,
            academic_year_id,
            period_code,
            name,
            period_type,
            start_date,
            end_date,
            parent_period_id,
            campus_context,
            status,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @AcademicYearId,
            @PeriodCode,
            @Name,
            @PeriodType,
            @StartDate,
            @EndDate,
            @ParentPeriodId,
            @CampusContext,
            @Status,
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    /// <summary>
    /// Generates standard Semester 1 and Semester 2 records for the academic year.
    /// </summary>
    public async Task<Result<ApplyAcademicYearPresetResponse>> HandleAsync(
        Guid id,
        ApplyAcademicYearPresetRequest? request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var academicYear = await connection.QuerySingleOrDefaultAsync<AcademicYearRow>(new CommandDefinition(
            _selectAcademicYearSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (academicYear is null)
        {
            return Result.Failure<ApplyAcademicYearPresetResponse>(
                DomainError.NotFound(
                    "Operations.AcademicYearNotFound",
                    $"Academic year with ID '{id}' was not found."));
        }

        if (string.Equals(academicYear.Status, "completed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(academicYear.Status, "archived", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ApplyAcademicYearPresetResponse>(
                DomainError.Validation(
                    "Operations.InvalidStatus",
                    $"Cannot apply presets to academic year with status '{academicYear.Status}'."));
        }

        var existingPeriodsCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _checkExistingPeriodsSql,
            new { AcademicYearId = id },
            cancellationToken: cancellationToken));

        if (existingPeriodsCount > 0)
        {
            return Result.Failure<ApplyAcademicYearPresetResponse>(
                DomainError.Conflict(
                    "Operations.PeriodsAlreadyExist",
                    "Operational periods have already been established for this academic year."));
        }

        // Generate Indonesian 2-semester preset (TD-07)
        // Semester 1 (Ganjil): July 15 – December 20
        // Semester 2 (Genap): January 5 – June 25
        var year1 = academicYear.StartDate.Year;
        var year2 = academicYear.EndDate.Year >= year1 ? academicYear.EndDate.Year : year1 + 1;

        DateOnly sem1Start;
        DateOnly sem1End;
        DateOnly sem2Start;
        DateOnly sem2End;

        if (year2 > year1)
        {
            var targetSem1Start = new DateOnly(year1, 7, 15);
            sem1Start = targetSem1Start >= academicYear.StartDate ? targetSem1Start : academicYear.StartDate;
            sem1End = new DateOnly(year1, 12, 20);

            sem2Start = new DateOnly(year2, 1, 5);
            var targetSem2End = new DateOnly(year2, 6, 25);
            sem2End = targetSem2End <= academicYear.EndDate ? targetSem2End : academicYear.EndDate;
        }
        else
        {
            sem1Start = academicYear.StartDate;
            sem1End = new DateOnly(year1, 6, 30);
            sem2Start = new DateOnly(year1, 7, 1);
            sem2End = academicYear.EndDate;
        }

        var sem1Id = Guid.NewGuid();
        var sem2Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var createdBy = currentUser.Username ?? "system";

        var sem1 = new OperationalPeriodPresetItemResponse(
            sem1Id,
            id,
            $"SEM-1-{year1}",
            $"Semester 1 {year1}/{year2}",
            "semester",
            sem1Start,
            sem1End,
            "draft",
            "Preset: Indonesian standard 2-semester baseline (Semester 1 / Ganjil)");

        var sem2 = new OperationalPeriodPresetItemResponse(
            sem2Id,
            id,
            $"SEM-2-{year2}",
            $"Semester 2 {year1}/{year2}",
            "semester",
            sem2Start,
            sem2End,
            "draft",
            "Preset: Indonesian standard 2-semester baseline (Semester 2 / Genap)");

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            _insertPeriodSql,
            new
            {
                Id = sem1.Id,
                AcademicYearId = id,
                PeriodCode = sem1.PeriodCode,
                Name = sem1.Name,
                PeriodType = sem1.PeriodType,
                StartDate = sem1.StartDate,
                EndDate = sem1.EndDate,
                ParentPeriodId = (Guid?)null,
                CampusContext = "All",
                Status = sem1.Status,
                Notes = sem1.Notes,
                CreatedAt = now,
                CreatedBy = createdBy,
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            _insertPeriodSql,
            new
            {
                Id = sem2.Id,
                AcademicYearId = id,
                PeriodCode = sem2.PeriodCode,
                Name = sem2.Name,
                PeriodType = sem2.PeriodType,
                StartDate = sem2.StartDate,
                EndDate = sem2.EndDate,
                ParentPeriodId = (Guid?)null,
                CampusContext = "All",
                Status = sem2.Status,
                Notes = sem2.Notes,
                CreatedAt = now,
                CreatedBy = createdBy,
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new ApplyAcademicYearPresetResponse(id, [sem1, sem2]));
    }

    private sealed record AcademicYearRow(
        Guid Id,
        string Code,
        string Name,
        DateOnly StartDate,
        DateOnly EndDate,
        string Status);
}
