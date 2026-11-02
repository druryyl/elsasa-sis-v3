namespace Elsasa.Sis.Operations.Features.CalendarPeriods.RecordCorrections.GetRecordCorrectionsList;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying historical record corrections for a specific academic year (ADR-004, ADR-005, UC-OPS-13).
/// </summary>
public sealed class GetRecordCorrectionsListHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _checkAcademicYearSql = """
        SELECT COUNT(1)
        FROM operations.academic_years
        WHERE id = @AcademicYearId;
        """;

    private const string _selectCorrectionsSql = """
        SELECT
            id AS Id,
            academic_year_id AS AcademicYearId,
            target_table_name AS TargetTableName,
            target_record_id AS TargetRecordId,
            field_name AS FieldName,
            old_value AS OldValue,
            new_value AS NewValue,
            justification_reason AS JustificationReason,
            authorized_by AS AuthorizedBy,
            authorized_at AS AuthorizedAt,
            created_at AS CreatedAt,
            created_by AS CreatedBy
        FROM operations.operations_record_corrections
        WHERE academic_year_id = @AcademicYearId
        ORDER BY authorized_at DESC;
        """;

    public async Task<Result<IReadOnlyList<RecordCorrectionItemResponse>>> HandleAsync(
        Guid academicYearId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var exists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _checkAcademicYearSql,
            new { AcademicYearId = academicYearId },
            cancellationToken: cancellationToken));

        if (exists == 0)
        {
            return Result.Failure<IReadOnlyList<RecordCorrectionItemResponse>>(
                DomainError.NotFound(
                    "Operations.AcademicYearNotFound",
                    $"Academic year with ID '{academicYearId}' was not found."));
        }

        var items = await connection.QueryAsync<RecordCorrectionItemResponse>(new CommandDefinition(
            _selectCorrectionsSql,
            new { AcademicYearId = academicYearId },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<RecordCorrectionItemResponse>>(items.AsList());
    }
}
