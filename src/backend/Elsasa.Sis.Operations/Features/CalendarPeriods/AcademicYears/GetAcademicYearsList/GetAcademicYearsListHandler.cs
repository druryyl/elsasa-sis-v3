namespace Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.GetAcademicYearsList;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler retrieving the list of academic years with optional status filtering (ADR-004, ADR-005).
/// </summary>
public sealed class GetAcademicYearsListHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _selectSql = """
        SELECT
            id AS Id,
            code AS Code,
            name AS Name,
            start_date AS StartDate,
            end_date AS EndDate,
            status AS Status,
            approved_by AS ApprovedBy,
            approved_date AS ApprovedDate,
            notes AS Notes,
            created_at AS CreatedAt,
            created_by AS CreatedBy,
            updated_at AS UpdatedAt,
            updated_by AS UpdatedBy
        FROM operations.academic_years
        WHERE (@Status IS NULL OR status = @Status)
        ORDER BY start_date DESC;
        """;

    /// <summary>
    /// Retrieves all academic years optionally filtered by lifecycle status.
    /// </summary>
    public async Task<Result<IReadOnlyList<AcademicYearListItemResponse>>> HandleAsync(
        string? status,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<AcademicYearListItemResponse>(new CommandDefinition(
            _selectSql,
            new { Status = string.IsNullOrWhiteSpace(status) ? null : status },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<AcademicYearListItemResponse>>(rows.AsList());
    }
}
