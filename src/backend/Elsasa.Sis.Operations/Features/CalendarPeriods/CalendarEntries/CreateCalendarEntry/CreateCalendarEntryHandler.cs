namespace Elsasa.Sis.Operations.Features.CalendarPeriods.CalendarEntries.CreateCalendarEntry;

using System.Text.Json;
using Dapper;
using Elsasa.Sis.Operations.Features.CalendarPeriods.Common;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing the creation of a calendar entry with automated amendment tracking (ADR-004, ADR-005, TD-04, TD-05).
/// </summary>
public sealed class CreateCalendarEntryHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectCalendarContextSql = """
        SELECT
            c.id AS CalendarId,
            c.academic_year_id AS AcademicYearId,
            c.version AS Version,
            c.publication_status AS PublicationStatus,
            ay.start_date AS AcademicYearStartDate,
            ay.end_date AS AcademicYearEndDate
        FROM operations.school_calendars c
        INNER JOIN operations.academic_years ay ON c.academic_year_id = ay.id
        WHERE c.id = @CalendarId;
        """;

    private const string _insertEntrySql = """
        INSERT INTO operations.calendar_entries
        (
            id,
            calendar_id,
            entry_date,
            end_date,
            designation,
            name,
            description,
            is_instructional,
            closure_id,
            applies_to,
            created_at,
            created_by
        )
        VALUES
        (
            @Id,
            @CalendarId,
            @EntryDate,
            @EndDate,
            @Designation,
            @Name,
            @Description,
            @IsInstructional,
            @ClosureId,
            @AppliesTo,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private const string _insertAmendmentSql = """
        INSERT INTO operations.calendar_amendments
        (
            id,
            calendar_id,
            amendment_number,
            from_version,
            to_version,
            amendment_type,
            affected_entry_id,
            change_summary,
            previous_value,
            new_value,
            reason,
            effective_date,
            authorized_by,
            authorized_date,
            downstream_impact_reviewed,
            created_at,
            created_by
        )
        VALUES
        (
            @Id,
            @CalendarId,
            @AmendmentNumber,
            @FromVersion,
            @ToVersion,
            @AmendmentType,
            @AffectedEntryId,
            @ChangeSummary,
            @PreviousValue,
            @NewValue,
            @Reason,
            @EffectiveDate,
            @AuthorizedBy,
            @AuthorizedDate,
            @DownstreamImpactReviewed,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private const string _updateCalendarVersionSql = """
        UPDATE operations.school_calendars
        SET version = @ToVersion,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @CalendarId;
        """;

    /// <summary>
    /// Creates a calendar entry, validating date containment within the parent academic year
    /// and generating a versioned amendment if the calendar has already been published.
    /// </summary>
    public async Task<Result<CreateCalendarEntryResponse>> HandleAsync(
        Guid calendarId,
        CreateCalendarEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var context = await connection.QuerySingleOrDefaultAsync<CalendarContextRow>(new CommandDefinition(
            _selectCalendarContextSql,
            new { CalendarId = calendarId },
            cancellationToken: cancellationToken));

        if (context is null)
        {
            return Result.Failure<CreateCalendarEntryResponse>(
                DomainError.NotFound(
                    "Operations.SchoolCalendarNotFound",
                    $"School calendar with ID '{calendarId}' was not found."));
        }

        var entryEndDate = request.EndDate ?? request.EntryDate;
        if (request.EntryDate < context.AcademicYearStartDate || entryEndDate > context.AcademicYearEndDate)
        {
            return Result.Failure<CreateCalendarEntryResponse>(
                DomainError.Validation(
                    "Operations.DateOutOfBounds",
                    $"Calendar entry date must fall within academic year date range ({context.AcademicYearStartDate:yyyy-MM-dd} to {context.AcademicYearEndDate:yyyy-MM-dd})."));
        }

        var entryId = Guid.NewGuid();
        var appliesTo = string.IsNullOrWhiteSpace(request.AppliesTo) ? "All" : request.AppliesTo.Trim();
        var createdAt = DateTimeOffset.UtcNow;
        var createdBy = currentUser.Username ?? "system";
        var isPublished = string.Equals(context.PublicationStatus, "published", StringComparison.OrdinalIgnoreCase);

        string? updatedVersion = context.Version;
        string? generatedAmendmentNumber = null;

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // 1. Insert calendar entry
        await connection.ExecuteAsync(new CommandDefinition(
            _insertEntrySql,
            new
            {
                Id = entryId,
                CalendarId = calendarId,
                EntryDate = request.EntryDate,
                EndDate = request.EndDate,
                Designation = request.Designation.Trim(),
                Name = request.Name.Trim(),
                Description = request.Description,
                IsInstructional = request.IsInstructional,
                ClosureId = (Guid?)null,
                AppliesTo = appliesTo,
                CreatedAt = createdAt,
                CreatedBy = createdBy,
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 2. If calendar is published, automatically record calendar amendment and increment version
        if (isPublished)
        {
            var fromVersion = context.Version;
            updatedVersion = CalendarAmendmentHelper.BumpMinorVersion(fromVersion);
            var amendmentYear = DateTime.UtcNow.Year;
            generatedAmendmentNumber = await CalendarAmendmentHelper.GenerateAmendmentNumberAsync(
                connection,
                transaction,
                amendmentYear,
                cancellationToken);

            var amendmentId = Guid.NewGuid();
            var changeSummary = $"Added calendar entry '{request.Name.Trim()}' ({request.Designation.Trim()}) on {request.EntryDate:yyyy-MM-dd}.";
            var newValueJson = JsonSerializer.Serialize(new
            {
                request.EntryDate,
                request.EndDate,
                Designation = request.Designation.Trim(),
                Name = request.Name.Trim(),
                request.Description,
                request.IsInstructional,
                AppliesTo = appliesTo,
            });

            var reason = string.IsNullOrWhiteSpace(request.AmendmentReason)
                ? "Operational calendar entry added post-publication."
                : request.AmendmentReason.Trim();

            await connection.ExecuteAsync(new CommandDefinition(
                _insertAmendmentSql,
                new
                {
                    Id = amendmentId,
                    CalendarId = calendarId,
                    AmendmentNumber = generatedAmendmentNumber,
                    FromVersion = fromVersion,
                    ToVersion = updatedVersion,
                    AmendmentType = "add_entry",
                    AffectedEntryId = entryId,
                    ChangeSummary = changeSummary,
                    PreviousValue = (string?)null,
                    NewValue = newValueJson,
                    Reason = reason,
                    EffectiveDate = request.EntryDate,
                    AuthorizedBy = createdBy,
                    AuthorizedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    DownstreamImpactReviewed = false,
                    CreatedAt = createdAt,
                    CreatedBy = createdBy,
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                _updateCalendarVersionSql,
                new
                {
                    CalendarId = calendarId,
                    ToVersion = updatedVersion,
                    UpdatedAt = createdAt,
                    UpdatedBy = createdBy,
                },
                transaction: transaction,
                cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new CreateCalendarEntryResponse(
            entryId,
            calendarId,
            request.EntryDate,
            request.EndDate,
            request.Designation.Trim(),
            request.Name.Trim(),
            request.Description,
            request.IsInstructional,
            appliesTo,
            updatedVersion,
            generatedAmendmentNumber,
            createdAt));
    }

    private sealed record CalendarContextRow(
        Guid CalendarId,
        Guid AcademicYearId,
        string Version,
        string PublicationStatus,
        DateOnly AcademicYearStartDate,
        DateOnly AcademicYearEndDate);
}
