namespace Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.CreateOperationalPeriod;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler creating a hierarchical operational period with strict date containment validation (ADR-004, ADR-005).
/// </summary>
public sealed class CreateOperationalPeriodHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectAcademicYearSql = """
        SELECT
            id AS Id,
            start_date AS StartDate,
            end_date AS EndDate,
            status AS Status
        FROM operations.academic_years
        WHERE id = @Id;
        """;

    private const string _checkDuplicateCodeSql = """
        SELECT COUNT(1)
        FROM operations.operational_periods
        WHERE academic_year_id = @AcademicYearId
          AND period_code = @PeriodCode;
        """;

    private const string _selectParentPeriodSql = """
        SELECT
            id AS Id,
            academic_year_id AS AcademicYearId,
            period_code AS PeriodCode,
            start_date AS StartDate,
            end_date AS EndDate
        FROM operations.operational_periods
        WHERE id = @ParentPeriodId
          AND academic_year_id = @AcademicYearId;
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
    /// Establishes a new operational period within the specified academic year.
    /// </summary>
    public async Task<Result<CreateOperationalPeriodResponse>> HandleAsync(
        Guid academicYearId,
        CreateOperationalPeriodRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var academicYear = await connection.QuerySingleOrDefaultAsync<AcademicYearRow>(new CommandDefinition(
            _selectAcademicYearSql,
            new { Id = academicYearId },
            cancellationToken: cancellationToken));

        if (academicYear is null)
        {
            return Result.Failure<CreateOperationalPeriodResponse>(
                DomainError.NotFound(
                    "Operations.AcademicYearNotFound",
                    $"Academic year with ID '{academicYearId}' was not found."));
        }

        if (request.StartDate < academicYear.StartDate || request.EndDate > academicYear.EndDate)
        {
            return Result.Failure<CreateOperationalPeriodResponse>(
                DomainError.Validation(
                    "Operations.PeriodDateRangeOutOfBounds",
                    $"Period dates ({request.StartDate:yyyy-MM-dd} - {request.EndDate:yyyy-MM-dd}) must fall within parent academic year date range ({academicYear.StartDate:yyyy-MM-dd} - {academicYear.EndDate:yyyy-MM-dd})."));
        }

        var duplicateCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _checkDuplicateCodeSql,
            new
            {
                AcademicYearId = academicYearId,
                PeriodCode = request.PeriodCode.Trim(),
            },
            cancellationToken: cancellationToken));

        if (duplicateCount > 0)
        {
            return Result.Failure<CreateOperationalPeriodResponse>(
                DomainError.Conflict(
                    "Operations.DuplicatePeriodCode",
                    $"An operational period with code '{request.PeriodCode.Trim()}' already exists for this academic year."));
        }

        if (request.ParentPeriodId.HasValue)
        {
            var parentPeriod = await connection.QuerySingleOrDefaultAsync<ParentPeriodRow>(new CommandDefinition(
                _selectParentPeriodSql,
                new
                {
                    ParentPeriodId = request.ParentPeriodId.Value,
                    AcademicYearId = academicYearId,
                },
                cancellationToken: cancellationToken));

            if (parentPeriod is null)
            {
                return Result.Failure<CreateOperationalPeriodResponse>(
                    DomainError.NotFound(
                        "Operations.ParentPeriodNotFound",
                        $"Parent operational period with ID '{request.ParentPeriodId.Value}' was not found in this academic year."));
            }

            if (request.StartDate < parentPeriod.StartDate || request.EndDate > parentPeriod.EndDate)
            {
                return Result.Failure<CreateOperationalPeriodResponse>(
                    DomainError.Validation(
                        "Operations.ChildPeriodDateRangeOutOfBounds",
                        $"Child period dates ({request.StartDate:yyyy-MM-dd} - {request.EndDate:yyyy-MM-dd}) must fall strictly within parent period date range ({parentPeriod.StartDate:yyyy-MM-dd} - {parentPeriod.EndDate:yyyy-MM-dd})."));
            }
        }

        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var createdBy = currentUser.Username ?? "system";
        var campusContext = string.IsNullOrWhiteSpace(request.CampusContext) ? "All" : request.CampusContext.Trim();
        var trimmedCode = request.PeriodCode.Trim();
        var trimmedName = request.Name.Trim();
        var trimmedType = request.PeriodType.Trim().ToLowerInvariant();

        await connection.ExecuteAsync(new CommandDefinition(
            _insertPeriodSql,
            new
            {
                Id = id,
                AcademicYearId = academicYearId,
                PeriodCode = trimmedCode,
                Name = trimmedName,
                PeriodType = trimmedType,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                ParentPeriodId = request.ParentPeriodId,
                CampusContext = campusContext,
                Status = "draft",
                Notes = request.Notes,
                CreatedAt = now,
                CreatedBy = createdBy,
            },
            cancellationToken: cancellationToken));

        return Result.Success(new CreateOperationalPeriodResponse(
            id,
            academicYearId,
            trimmedCode,
            trimmedName,
            trimmedType,
            request.StartDate,
            request.EndDate,
            request.ParentPeriodId,
            campusContext,
            "draft",
            request.Notes,
            now));
    }

    private sealed record AcademicYearRow(
        Guid Id,
        DateOnly StartDate,
        DateOnly EndDate,
        string Status);

    private sealed record ParentPeriodRow(
        Guid Id,
        Guid AcademicYearId,
        string PeriodCode,
        DateOnly StartDate,
        DateOnly EndDate);
}
