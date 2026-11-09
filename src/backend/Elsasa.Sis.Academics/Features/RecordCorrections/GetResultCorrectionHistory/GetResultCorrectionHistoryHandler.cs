namespace Elsasa.Sis.Academics.Features.RecordCorrections.GetResultCorrectionHistory;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler querying historical alteration audit records for a given academic result (UC-ACD-19, TD-06, P6-S14).
/// </summary>
public sealed class GetResultCorrectionHistoryHandler(ISqlConnectionFactory connectionFactory)
{
    static GetResultCorrectionHistoryHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkResultExistsSql = """
        SELECT 1
        FROM academics.academic_results
        WHERE id = @Id;
        """;

    private const string _selectCorrectionsSql = """
        SELECT
            id AS Id,
            record_type AS RecordType,
            record_id AS RecordId,
            original_value AS OriginalValue,
            corrected_value AS CorrectedValue,
            reason AS Reason,
            evidence AS Evidence,
            corrected_by AS CorrectedBy,
            corrected_date AS CorrectedDate,
            is_material AS IsMaterial,
            notes AS Notes,
            created_at AS CreatedAt,
            created_by AS CreatedBy
        FROM academics.academic_record_corrections
        WHERE record_type = 'academic_result'
          AND record_id = @RecordId
        ORDER BY corrected_date DESC, created_at DESC;
        """;

    public async Task<Result<IReadOnlyList<AcademicRecordCorrectionDto>>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify target academic result exists
        var exists = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                _checkResultExistsSql,
                new { Id = id },
                cancellationToken: cancellationToken));

        if (!exists)
        {
            return Result.Failure<IReadOnlyList<AcademicRecordCorrectionDto>>(
                DomainError.NotFound(
                    "Academics.AcademicResults.NotFound",
                    $"Academic result '{id}' was not found."));
        }

        // 2. Query all correction entries logged for this result
        var corrections = (await connection.QueryAsync<AcademicRecordCorrectionDto>(
            new CommandDefinition(
                _selectCorrectionsSql,
                new { RecordId = id },
                cancellationToken: cancellationToken))).ToList();

        return Result.Success<IReadOnlyList<AcademicRecordCorrectionDto>>(corrections);
    }
}
