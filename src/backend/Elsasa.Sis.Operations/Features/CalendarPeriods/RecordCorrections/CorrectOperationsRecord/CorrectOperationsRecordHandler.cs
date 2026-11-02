namespace Elsasa.Sis.Operations.Features.CalendarPeriods.RecordCorrections.CorrectOperationsRecord;

using System.Data;
using System.Globalization;
using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing auditable corrections to approved academic years or operational periods (ADR-004, ADR-005, UC-OPS-13).
/// </summary>
public sealed class CorrectOperationsRecordHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private static readonly Dictionary<string, string> _academicYearColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["code"] = "code",
        ["name"] = "name",
        ["start_date"] = "start_date",
        ["startdate"] = "start_date",
        ["end_date"] = "end_date",
        ["enddate"] = "end_date",
        ["status"] = "status",
        ["notes"] = "notes",
    };

    private static readonly Dictionary<string, string> _operationalPeriodColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["period_code"] = "period_code",
        ["periodcode"] = "period_code",
        ["code"] = "period_code",
        ["name"] = "name",
        ["period_type"] = "period_type",
        ["periodtype"] = "period_type",
        ["start_date"] = "start_date",
        ["startdate"] = "start_date",
        ["end_date"] = "end_date",
        ["enddate"] = "end_date",
        ["campus_context"] = "campus_context",
        ["campuscontext"] = "campus_context",
        ["status"] = "status",
        ["notes"] = "notes",
        ["parent_period_id"] = "parent_period_id",
        ["parentperiodid"] = "parent_period_id",
    };

    private const string _insertCorrectionSql = """
        INSERT INTO operations.operations_record_corrections
        (
            id,
            academic_year_id,
            target_table_name,
            target_record_id,
            field_name,
            old_value,
            new_value,
            justification_reason,
            authorized_by,
            authorized_at,
            created_at,
            created_by
        )
        VALUES
        (
            @Id,
            @AcademicYearId,
            @TargetTableName,
            @TargetRecordId,
            @FieldName,
            @OldValue,
            @NewValue,
            @JustificationReason,
            @AuthorizedBy,
            @AuthorizedAt,
            @CreatedAt,
            @CreatedBy
        );
        """;

    public async Task<Result<CorrectOperationsRecordResponse>> HandleAsync(
        CorrectOperationsRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedTable = request.TargetTableName.Trim().ToLowerInvariant();
        if (normalizedTable is not ("academic_years" or "operational_periods"))
        {
            return Result.Failure<CorrectOperationsRecordResponse>(
                DomainError.Validation(
                    "Operations.InvalidTargetTable",
                    "Target table must be either 'academic_years' or 'operational_periods'."));
        }

        var authorizedBy = !string.IsNullOrWhiteSpace(request.AuthorizedBy)
            ? request.AuthorizedBy.Trim()
            : (currentUser.Username ?? "system");

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        Guid academicYearId;
        string? oldValue;
        string canonicalColumn;

        if (normalizedTable == "academic_years")
        {
            if (!_academicYearColumns.TryGetValue(request.FieldName.Trim(), out var matchedColumn))
            {
                return Result.Failure<CorrectOperationsRecordResponse>(
                    DomainError.Validation(
                        "Operations.InvalidFieldName",
                        $"Field '{request.FieldName}' is not supported for correction on table 'academic_years'."));
            }

            canonicalColumn = matchedColumn;

            const string selectAySql = """
                SELECT
                    id AS Id,
                    code AS Code,
                    name AS Name,
                    start_date AS StartDate,
                    end_date AS EndDate,
                    status AS Status,
                    notes AS Notes
                FROM operations.academic_years
                WHERE id = @Id;
                """;

            var ay = await connection.QuerySingleOrDefaultAsync<AcademicYearRow>(new CommandDefinition(
                selectAySql,
                new { Id = request.TargetRecordId },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (ay is null)
            {
                return Result.Failure<CorrectOperationsRecordResponse>(
                    DomainError.NotFound(
                        "Operations.AcademicYearNotFound",
                        $"Academic year with ID '{request.TargetRecordId}' was not found."));
            }

            academicYearId = ay.Id;
            oldValue = canonicalColumn switch
            {
                "code" => ay.Code,
                "name" => ay.Name,
                "start_date" => ay.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                "end_date" => ay.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                "status" => ay.Status,
                "notes" => ay.Notes,
                _ => null,
            };

            if (canonicalColumn is "start_date" or "end_date")
            {
                if (!DateOnly.TryParse(request.NewValue, CultureInfo.InvariantCulture, out _))
                {
                    return Result.Failure<CorrectOperationsRecordResponse>(
                        DomainError.Validation(
                            "Operations.InvalidDateFormat",
                            $"Value '{request.NewValue}' is not a valid date for field '{canonicalColumn}'. Expected format YYYY-MM-DD."));
                }
            }

            var updateAySql = $"""
                UPDATE operations.academic_years
                SET [{canonicalColumn}] = @NewValue,
                    updated_at = @UpdatedAt,
                    updated_by = @UpdatedBy
                WHERE id = @Id;
                """;

            await connection.ExecuteAsync(new CommandDefinition(
                updateAySql,
                new
                {
                    Id = request.TargetRecordId,
                    NewValue = request.NewValue,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    UpdatedBy = authorizedBy,
                },
                transaction: transaction,
                cancellationToken: cancellationToken));
        }
        else
        {
            if (!_operationalPeriodColumns.TryGetValue(request.FieldName.Trim(), out var matchedColumn))
            {
                return Result.Failure<CorrectOperationsRecordResponse>(
                    DomainError.Validation(
                        "Operations.InvalidFieldName",
                        $"Field '{request.FieldName}' is not supported for correction on table 'operational_periods'."));
            }

            canonicalColumn = matchedColumn;

            const string selectPeriodSql = """
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

            var period = await connection.QuerySingleOrDefaultAsync<OperationalPeriodRow>(new CommandDefinition(
                selectPeriodSql,
                new { Id = request.TargetRecordId },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (period is null)
            {
                return Result.Failure<CorrectOperationsRecordResponse>(
                    DomainError.NotFound(
                        "Operations.OperationalPeriodNotFound",
                        $"Operational period with ID '{request.TargetRecordId}' was not found."));
            }

            academicYearId = period.AcademicYearId;
            oldValue = canonicalColumn switch
            {
                "period_code" => period.PeriodCode,
                "name" => period.Name,
                "period_type" => period.PeriodType,
                "start_date" => period.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                "end_date" => period.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                "parent_period_id" => period.ParentPeriodId?.ToString(),
                "campus_context" => period.CampusContext,
                "status" => period.Status,
                "notes" => period.Notes,
                _ => null,
            };

            if (canonicalColumn is "start_date" or "end_date")
            {
                if (!DateOnly.TryParse(request.NewValue, CultureInfo.InvariantCulture, out _))
                {
                    return Result.Failure<CorrectOperationsRecordResponse>(
                        DomainError.Validation(
                            "Operations.InvalidDateFormat",
                            $"Value '{request.NewValue}' is not a valid date for field '{canonicalColumn}'. Expected format YYYY-MM-DD."));
                }
            }
            else if (canonicalColumn == "parent_period_id" && !string.IsNullOrWhiteSpace(request.NewValue))
            {
                if (!Guid.TryParse(request.NewValue, out _))
                {
                    return Result.Failure<CorrectOperationsRecordResponse>(
                        DomainError.Validation(
                            "Operations.InvalidGuidFormat",
                            $"Value '{request.NewValue}' is not a valid GUID for field '{canonicalColumn}'."));
                }
            }

            var updatePeriodSql = $"""
                UPDATE operations.operational_periods
                SET [{canonicalColumn}] = @NewValue,
                    updated_at = @UpdatedAt,
                    updated_by = @UpdatedBy
                WHERE id = @Id;
                """;

            await connection.ExecuteAsync(new CommandDefinition(
                updatePeriodSql,
                new
                {
                    Id = request.TargetRecordId,
                    NewValue = request.NewValue,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    UpdatedBy = authorizedBy,
                },
                transaction: transaction,
                cancellationToken: cancellationToken));
        }

        var correctionId = Guid.NewGuid();
        var authorizedAt = DateTimeOffset.UtcNow;

        await connection.ExecuteAsync(new CommandDefinition(
            _insertCorrectionSql,
            new
            {
                Id = correctionId,
                AcademicYearId = academicYearId,
                TargetTableName = normalizedTable,
                TargetRecordId = request.TargetRecordId,
                FieldName = canonicalColumn,
                OldValue = oldValue,
                NewValue = request.NewValue,
                JustificationReason = request.JustificationReason.Trim(),
                AuthorizedBy = authorizedBy,
                AuthorizedAt = authorizedAt,
                CreatedAt = authorizedAt,
                CreatedBy = authorizedBy,
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new CorrectOperationsRecordResponse(
            Id: correctionId,
            AcademicYearId: academicYearId,
            TargetTableName: normalizedTable,
            TargetRecordId: request.TargetRecordId,
            FieldName: canonicalColumn,
            OldValue: oldValue,
            NewValue: request.NewValue,
            JustificationReason: request.JustificationReason.Trim(),
            AuthorizedBy: authorizedBy,
            AuthorizedAt: authorizedAt));
    }

    private sealed record AcademicYearRow(
        Guid Id,
        string Code,
        string Name,
        DateOnly StartDate,
        DateOnly EndDate,
        string Status,
        string? Notes);

    private sealed record OperationalPeriodRow(
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
}
