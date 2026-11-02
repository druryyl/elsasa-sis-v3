namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.PublishSchoolCalendar;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing the transition of a school calendar to 'published' (ADR-004, ADR-005).
/// </summary>
public sealed class PublishSchoolCalendarHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectSql = """
        SELECT
            id AS Id,
            academic_year_id AS AcademicYearId,
            name AS Name,
            version AS Version,
            status AS Status,
            publication_status AS PublicationStatus
        FROM operations.school_calendars
        WHERE id = @Id;
        """;

    private const string _publishSql = """
        UPDATE operations.school_calendars
        SET publication_status = 'published',
            status = 'approved',
            published_at = @PublishedAt,
            published_by = @PublishedBy,
            approved_by = COALESCE(approved_by, @PublishedBy),
            approved_date = COALESCE(approved_date, @ApprovedDate),
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    /// <summary>
    /// Publishes a school calendar and records publisher identity and timestamp.
    /// </summary>
    public async Task<Result<PublishSchoolCalendarResponse>> HandleAsync(
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
            return Result.Failure<PublishSchoolCalendarResponse>(
                DomainError.NotFound(
                    "Operations.SchoolCalendarNotFound",
                    $"School calendar with ID '{id}' was not found."));
        }

        if (string.Equals(existing.PublicationStatus, "published", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<PublishSchoolCalendarResponse>(
                DomainError.Validation(
                    "Operations.CalendarAlreadyPublished",
                    "School calendar is already published."));
        }

        var publishedBy = currentUser.Username ?? "system";
        var publishedAt = DateTimeOffset.UtcNow;
        var approvedDate = DateOnly.FromDateTime(DateTime.UtcNow);

        await connection.ExecuteAsync(new CommandDefinition(
            _publishSql,
            new
            {
                Id = id,
                PublishedAt = publishedAt,
                PublishedBy = publishedBy,
                ApprovedDate = approvedDate,
                UpdatedAt = publishedAt,
                UpdatedBy = publishedBy,
            },
            cancellationToken: cancellationToken));

        return Result.Success(new PublishSchoolCalendarResponse(
            existing.Id,
            existing.AcademicYearId,
            existing.Name,
            existing.Version,
            "approved",
            "published",
            publishedAt,
            publishedBy));
    }

    private sealed record SchoolCalendarRow(
        Guid Id,
        Guid AcademicYearId,
        string Name,
        string Version,
        string Status,
        string PublicationStatus);
}
