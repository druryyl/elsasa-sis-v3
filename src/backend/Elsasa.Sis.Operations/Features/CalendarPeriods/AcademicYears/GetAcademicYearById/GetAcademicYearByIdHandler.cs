namespace Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.GetAcademicYearById;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler retrieving an academic year by unique identifier (ADR-004, ADR-005).
/// </summary>
public sealed class GetAcademicYearByIdHandler(ISqlConnectionFactory connectionFactory)
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
        WHERE id = @Id;
        """;

    /// <summary>
    /// Retrieves a single academic year record by ID.
    /// </summary>
    public async Task<Result<GetAcademicYearByIdResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var record = await connection.QuerySingleOrDefaultAsync<GetAcademicYearByIdResponse>(new CommandDefinition(
            _selectSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (record is null)
        {
            return Result.Failure<GetAcademicYearByIdResponse>(
                DomainError.NotFound(
                    "Operations.AcademicYearNotFound",
                    $"Academic year with ID '{id}' was not found."));
        }

        return Result.Success(record);
    }
}
