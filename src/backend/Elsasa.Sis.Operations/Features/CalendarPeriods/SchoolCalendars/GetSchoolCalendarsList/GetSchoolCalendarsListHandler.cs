namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.GetSchoolCalendarsList;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying all school calendar containers for an academic year (ADR-004, ADR-005).
/// </summary>
public sealed class GetSchoolCalendarsListHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _selectAcademicYearExistsSql = """
        SELECT 1
        FROM operations.academic_years
        WHERE id = @AcademicYearId;
        """;

    private const string _selectCalendarsSql = """
        SELECT
            id AS Id,
            academic_year_id AS AcademicYearId,
            name AS Name,
            scope_type AS ScopeType,
            scope_reference AS ScopeReference,
            version AS Version,
            status AS Status,
            publication_status AS PublicationStatus,
            published_at AS PublishedAt,
            published_by AS PublishedBy,
            approved_by AS ApprovedBy,
            approved_date AS ApprovedDate,
            notes AS Notes,
            created_at AS CreatedAt,
            created_by AS CreatedBy,
            updated_at AS UpdatedAt,
            updated_by AS UpdatedBy
        FROM operations.school_calendars
        WHERE academic_year_id = @AcademicYearId
        ORDER BY created_at DESC;
        """;

    /// <summary>
    /// Retrieves all school calendars declared under the specified academic year.
    /// </summary>
    public async Task<Result<IReadOnlyList<SchoolCalendarResponse>>> HandleAsync(
        Guid academicYearId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var academicYearExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            _selectAcademicYearExistsSql,
            new { AcademicYearId = academicYearId },
            cancellationToken: cancellationToken));

        if (!academicYearExists)
        {
            return Result.Failure<IReadOnlyList<SchoolCalendarResponse>>(
                DomainError.NotFound(
                    "Operations.AcademicYearNotFound",
                    $"Academic year with ID '{academicYearId}' was not found."));
        }

        var calendars = await connection.QueryAsync<SchoolCalendarResponse>(new CommandDefinition(
            _selectCalendarsSql,
            new { AcademicYearId = academicYearId },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<SchoolCalendarResponse>>(calendars.AsList());
    }
}