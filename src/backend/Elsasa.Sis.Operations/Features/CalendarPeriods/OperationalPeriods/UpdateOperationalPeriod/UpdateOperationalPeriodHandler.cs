namespace Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.UpdateOperationalPeriod;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler updating an operational period with strict hierarchical containment validation (ADR-004, ADR-005).
/// </summary>
public sealed class UpdateOperationalPeriodHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectPeriodSql = """
        SELECT
            id AS Id,
            academic_year_id AS AcademicYearId,
            period_code AS PeriodCode,
            name AS Name,
            period_type AS PeriodType,
            start_date AS StartDate,
            end_date AS EndDate,
            parent_period_id AS ParentPeriodId,
            campus_context AS CampusContext,
            status AS Status,
            notes AS Notes
        FROM operations.operational_periods
        WHERE id = @Id;
        """;

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
          AND period_code = @PeriodCode
          AND id <> @Id;
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

    private const string _selectChildPeriodsSql = """
        SELECT
            id AS Id,
            period_code AS PeriodCode,
            start_date AS StartDate,
            end_date AS EndDate
        FROM operations.operational_periods
        WHERE parent_period_id = @Id;
        """;

    private const string _updatePeriodSql = """
        UPDATE operations.operational_periods
        SET
            period_code = @PeriodCode,
            name = @Name,
            period_type = @PeriodType,
            start_date = @StartDate,
            end_date = @EndDate,
            parent_period_id = @ParentPeriodId,
            campus_context = @CampusContext,
            notes = @Notes,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    /// <summary>
    /// Modifies an operational period while enforcing code uniqueness and parent/child date containment.
    /// </summary>
    public async Task<Result<UpdateOperationalPeriodResponse>> HandleAsync(
        Guid id,
        UpdateOperationalPeriodRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var existingPeriod = await connection.QuerySingleOrDefaultAsync<PeriodRow>(new CommandDefinition(
            _selectPeriodSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existingPeriod is null)
        {
            return Result.Failure<UpdateOperationalPeriodResponse>(
                DomainError.NotFound(
                    "Operations.OperationalPeriodNotFound",
                    $"Operational period with ID '{id}' was not found."));
        }

        if (string.Equals(existingPeriod.Status, "closed", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<UpdateOperationalPeriodResponse>(
                DomainError.Validation(
                    "Operations.InvalidPeriodStatus",
                    "Closed operational periods cannot be modified."));
        }

        var academicYear = await connection.QuerySingleOrDefaultAsync<AcademicYearRow>(new CommandDefinition(
            _selectAcademicYearSql,
            new { Id = existingPeriod.AcademicYearId },
            cancellationToken: cancellationToken));

        if (academicYear is null)
        {
            return Result.Failure<UpdateOperationalPeriodResponse>(
                DomainError.NotFound(
                    "Operations.AcademicYearNotFound",
                    $"Parent academic year with ID '{existingPeriod.AcademicYearId}' was not found."));
        }

        if (request.StartDate < academicYear.StartDate || request.EndDate > academicYear.EndDate)
        {
            return Result.Failure<UpdateOperationalPeriodResponse>(
                DomainError.Validation(
                    "Operations.PeriodDateRangeOutOfBounds",
                    $"Period dates ({request.StartDate:yyyy-MM-dd} - {request.EndDate:yyyy-MM-dd}) must fall within parent academic year date range ({academicYear.StartDate:yyyy-MM-dd} - {academicYear.EndDate:yyyy-MM-dd})."));
        }

        var duplicateCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _checkDuplicateCodeSql,
            new
            {
                AcademicYearId = existingPeriod.AcademicYearId,
                PeriodCode = request.PeriodCode.Trim(),
                Id = id,
            },
            cancellationToken: cancellationToken));

        if (duplicateCount > 0)
        {
            return Result.Failure<UpdateOperationalPeriodResponse>(
                DomainError.Conflict(
                    "Operations.DuplicatePeriodCode",
                    $"An operational period with code '{request.PeriodCode.Trim()}' already exists for this academic year."));
        }

        if (request.ParentPeriodId.HasValue)
        {
            if (request.ParentPeriodId.Value == id)
            {
                return Result.Failure<UpdateOperationalPeriodResponse>(
                    DomainError.Validation(
                        "Operations.CircularParentPeriod",
                        "An operational period cannot be its own parent."));
            }

            var parentPeriod = await connection.QuerySingleOrDefaultAsync<ParentPeriodRow>(new CommandDefinition(
                _selectParentPeriodSql,
                new
                {
                    ParentPeriodId = request.ParentPeriodId.Value,
                    AcademicYearId = existingPeriod.AcademicYearId,
                },
                cancellationToken: cancellationToken));

            if (parentPeriod is null)
            {
                return Result.Failure<UpdateOperationalPeriodResponse>(
                    DomainError.NotFound(
                        "Operations.ParentPeriodNotFound",
                        $"Parent operational period with ID '{request.ParentPeriodId.Value}' was not found in this academic year."));
            }

            if (request.StartDate < parentPeriod.StartDate || request.EndDate > parentPeriod.EndDate)
            {
                return Result.Failure<UpdateOperationalPeriodResponse>(
                    DomainError.Validation(
                        "Operations.ChildPeriodDateRangeOutOfBounds",
                        $"Child period dates ({request.StartDate:yyyy-MM-dd} - {request.EndDate:yyyy-MM-dd}) must fall strictly within parent period date range ({parentPeriod.StartDate:yyyy-MM-dd} - {parentPeriod.EndDate:yyyy-MM-dd})."));
            }
        }

        // Verify that updating dates does not orphan/invalidate existing child periods
        var childPeriods = (await connection.QueryAsync<ChildPeriodRow>(new CommandDefinition(
            _selectChildPeriodsSql,
            new { Id = id },
            cancellationToken: cancellationToken))).AsList();

        foreach (var child in childPeriods)
        {
            if (child.StartDate < request.StartDate || child.EndDate > request.EndDate)
            {
                return Result.Failure<UpdateOperationalPeriodResponse>(
                    DomainError.Validation(
                        "Operations.ParentPeriodBoundaryViolatesChildren",
                        $"Cannot update period boundaries: child period '{child.PeriodCode}' ({child.StartDate:yyyy-MM-dd} - {child.EndDate:yyyy-MM-dd}) falls outside the new dates."));
            }
        }

        var now = DateTimeOffset.UtcNow;
        var updatedBy = currentUser.Username ?? "system";
        var campusContext = string.IsNullOrWhiteSpace(request.CampusContext) ? "All" : request.CampusContext.Trim();
        var trimmedCode = request.PeriodCode.Trim();
        var trimmedName = request.Name.Trim();
        var trimmedType = request.PeriodType.Trim().ToLowerInvariant();

        await connection.ExecuteAsync(new CommandDefinition(
            _updatePeriodSql,
            new
            {
                Id = id,
                PeriodCode = trimmedCode,
                Name = trimmedName,
                PeriodType = trimmedType,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                ParentPeriodId = request.ParentPeriodId,
                CampusContext = campusContext,
                Notes = request.Notes,
                UpdatedAt = now,
                UpdatedBy = updatedBy,
            },
            cancellationToken: cancellationToken));

        return Result.Success(new UpdateOperationalPeriodResponse(
            id,
            existingPeriod.AcademicYearId,
            trimmedCode,
            trimmedName,
            trimmedType,
            request.StartDate,
            request.EndDate,
            request.ParentPeriodId,
            campusContext,
            existingPeriod.Status,
            request.Notes,
            now));
    }

    private sealed record PeriodRow(
        Guid Id,
        Guid AcademicYearId,
        string PeriodCode,
        string Name,
        string PeriodType,
        DateOnly StartDate,
        DateOnly EndDate,
        Guid? ParentPeriodId,
        string CampusContext,
        string Status,
        string? Notes);

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

    private sealed record ChildPeriodRow(
        Guid Id,
        string PeriodCode,
        DateOnly StartDate,
        DateOnly EndDate);
}
