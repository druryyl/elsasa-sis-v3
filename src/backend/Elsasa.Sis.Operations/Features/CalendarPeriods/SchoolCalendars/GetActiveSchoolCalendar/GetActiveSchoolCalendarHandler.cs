namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.GetActiveSchoolCalendar;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying the active published school calendar for an academic year (ADR-004, ADR-005).
/// </summary>
public sealed class GetActiveSchoolCalendarHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _querySql = """
        SELECT TOP 1
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
            created_at AS CreatedAt
        FROM operations.school_calendars
        WHERE academic_year_id = @AcademicYearId
          AND publication_status = 'published'
        ORDER BY created_at DESC;
        """;

    /// <summary>
    /// Retrieves the most recent published school calendar for the given academic year.
    /// </summary>
    public async Task<Result<ActiveSchoolCalendarResponse>> HandleAsync(
        Guid academicYearId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var calendar = await connection.QuerySingleOrDefaultAsync<ActiveSchoolCalendarResponse>(new CommandDefinition(
            _querySql,
            new { AcademicYearId = academicYearId },
            cancellationToken: cancellationToken));

        if (calendar is null)
        {
            return Result.Failure<ActiveSchoolCalendarResponse>(
                DomainError.NotFound(
                    "Operations.ActiveCalendarNotFound",
                    $"No active published school calendar was found for academic year ID '{academicYearId}'."));
        }

        return Result.Success(calendar);
    }
}
