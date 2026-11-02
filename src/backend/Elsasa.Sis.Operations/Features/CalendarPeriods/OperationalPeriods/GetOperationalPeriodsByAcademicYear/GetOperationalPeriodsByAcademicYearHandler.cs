namespace Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.GetOperationalPeriodsByAcademicYear;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler retrieving all operational periods for an academic year in both flat and hierarchical formats (ADR-004, ADR-005).
/// </summary>
public sealed class GetOperationalPeriodsByAcademicYearHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _selectAcademicYearSql = """
        SELECT id
        FROM operations.academic_years
        WHERE id = @Id;
        """;

    private const string _selectPeriodsSql = """
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
            notes AS Notes,
            created_at AS CreatedAt,
            updated_at AS UpdatedAt
        FROM operations.operational_periods
        WHERE academic_year_id = @AcademicYearId
        ORDER BY start_date ASC, end_date ASC, period_code ASC;
        """;

    /// <summary>
    /// Retrieves operational periods for the given academic year, structured as flat items and hierarchical tree.
    /// </summary>
    public async Task<Result<GetOperationalPeriodsByAcademicYearResponse>> HandleAsync(
        Guid academicYearId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var academicYearExists = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            _selectAcademicYearSql,
            new { Id = academicYearId },
            cancellationToken: cancellationToken));

        if (!academicYearExists.HasValue)
        {
            return Result.Failure<GetOperationalPeriodsByAcademicYearResponse>(
                DomainError.NotFound(
                    "Operations.AcademicYearNotFound",
                    $"Academic year with ID '{academicYearId}' was not found."));
        }

        var periods = (await connection.QueryAsync<OperationalPeriodItemResponse>(new CommandDefinition(
            _selectPeriodsSql,
            new { AcademicYearId = academicYearId },
            cancellationToken: cancellationToken))).AsList();

        var childrenByParent = periods
            .Where(p => p.ParentPeriodId.HasValue)
            .GroupBy(p => p.ParentPeriodId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        OperationalPeriodNodeResponse BuildNode(OperationalPeriodItemResponse item)
        {
            var children = childrenByParent.TryGetValue(item.Id, out var childList)
                ? childList.Select(BuildNode).ToList()
                : [];

            return new OperationalPeriodNodeResponse(
                item.Id,
                item.AcademicYearId,
                item.PeriodCode,
                item.Name,
                item.PeriodType,
                item.StartDate,
                item.EndDate,
                item.ParentPeriodId,
                item.CampusContext,
                item.Status,
                item.Notes,
                item.CreatedAt,
                item.UpdatedAt,
                children);
        }

        var tree = periods
            .Where(p => !p.ParentPeriodId.HasValue || !periods.Any(parent => parent.Id == p.ParentPeriodId.Value))
            .Select(BuildNode)
            .ToList();

        return Result.Success(new GetOperationalPeriodsByAcademicYearResponse(
            academicYearId,
            periods,
            tree));
    }
}
