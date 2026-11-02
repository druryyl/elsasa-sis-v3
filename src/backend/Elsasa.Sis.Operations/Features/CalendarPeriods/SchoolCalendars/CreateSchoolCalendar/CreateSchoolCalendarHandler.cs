namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.CreateSchoolCalendar;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing the creation of a school calendar container (ADR-004, ADR-005).
/// </summary>
public sealed class CreateSchoolCalendarHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _checkAcademicYearSql = """
        SELECT COUNT(1)
        FROM operations.academic_years
        WHERE id = @AcademicYearId;
        """;

    private const string _insertCalendarSql = """
        INSERT INTO operations.school_calendars
        (
            id,
            academic_year_id,
            name,
            scope_type,
            scope_reference,
            version,
            status,
            publication_status,
            notes,
            created_at,
            created_by
        )
        VALUES
        (
            @Id,
            @AcademicYearId,
            @Name,
            @ScopeType,
            @ScopeReference,
            @Version,
            @Status,
            @PublicationStatus,
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    /// <summary>
    /// Creates a school calendar in 'draft' status with initial version '1.0'.
    /// </summary>
    public async Task<Result<CreateSchoolCalendarResponse>> HandleAsync(
        Guid academicYearId,
        CreateSchoolCalendarRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var yearCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _checkAcademicYearSql,
            new { AcademicYearId = academicYearId },
            cancellationToken: cancellationToken));

        if (yearCount == 0)
        {
            return Result.Failure<CreateSchoolCalendarResponse>(
                DomainError.NotFound(
                    "Operations.AcademicYearNotFound",
                    $"Academic year with ID '{academicYearId}' was not found."));
        }

        var calendarId = Guid.NewGuid();
        var scopeType = string.IsNullOrWhiteSpace(request.ScopeType) ? "institution" : request.ScopeType.Trim();
        var scopeReference = string.IsNullOrWhiteSpace(request.ScopeReference) ? "All" : request.ScopeReference.Trim();
        const string version = "1.0";
        const string status = "draft";
        const string publicationStatus = "draft";
        var createdAt = DateTimeOffset.UtcNow;
        var createdBy = currentUser.Username ?? "system";

        await connection.ExecuteAsync(new CommandDefinition(
            _insertCalendarSql,
            new
            {
                Id = calendarId,
                AcademicYearId = academicYearId,
                Name = request.Name.Trim(),
                ScopeType = scopeType,
                ScopeReference = scopeReference,
                Version = version,
                Status = status,
                PublicationStatus = publicationStatus,
                Notes = request.Notes,
                CreatedAt = createdAt,
                CreatedBy = createdBy,
            },
            cancellationToken: cancellationToken));

        return Result.Success(new CreateSchoolCalendarResponse(
            calendarId,
            academicYearId,
            request.Name.Trim(),
            scopeType,
            scopeReference,
            version,
            status,
            publicationStatus,
            request.Notes,
            createdAt));
    }
}
