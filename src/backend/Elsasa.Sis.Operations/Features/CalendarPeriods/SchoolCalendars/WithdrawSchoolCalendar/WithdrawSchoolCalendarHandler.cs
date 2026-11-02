namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.WithdrawSchoolCalendar;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing the transition of a school calendar to 'withdrawn' (ADR-004, ADR-005).
/// </summary>
public sealed class WithdrawSchoolCalendarHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectSql = """
        SELECT
            id AS Id,
            academic_year_id AS AcademicYearId,
            name AS Name,
            version AS Version,
            publication_status AS PublicationStatus
        FROM operations.school_calendars
        WHERE id = @Id;
        """;

    private const string _withdrawSql = """
        UPDATE operations.school_calendars
        SET publication_status = 'withdrawn',
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    /// <summary>
    /// Withdraws a published school calendar.
    /// </summary>
    public async Task<Result<WithdrawSchoolCalendarResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var existing = await connection.QuerySingleOrDefaultAsync<SchoolCalendarRow>(new CommandDefinition(
            _selectSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<WithdrawSchoolCalendarResponse>(
                DomainError.NotFound(
                    "Operations.SchoolCalendarNotFound",
                    $"School calendar with ID '{id}' was not found."));
        }

        if (!string.Equals(existing.PublicationStatus, "published", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<WithdrawSchoolCalendarResponse>(
                DomainError.Validation(
                    "Operations.InvalidPublicationStatus",
                    $"Cannot withdraw calendar with publication status '{existing.PublicationStatus}'. Only published calendars can be withdrawn."));
        }

        var updatedAt = DateTimeOffset.UtcNow;
        var updatedBy = currentUser.Username ?? "system";

        await connection.ExecuteAsync(new CommandDefinition(
            _withdrawSql,
            new
            {
                Id = id,
                UpdatedAt = updatedAt,
                UpdatedBy = updatedBy,
            },
            cancellationToken: cancellationToken));

        return Result.Success(new WithdrawSchoolCalendarResponse(
            existing.Id,
            existing.AcademicYearId,
            existing.Name,
            existing.Version,
            "withdrawn"));
    }

    private sealed record SchoolCalendarRow(
        Guid Id,
        Guid AcademicYearId,
        string Name,
        string Version,
        string PublicationStatus);
}
