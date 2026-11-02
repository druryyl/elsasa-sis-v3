namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolClosures.DeclareSchoolClosure;

using System.Text.Json;
using Dapper;
using Elsasa.Sis.Operations.Features.CalendarPeriods.Common;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing an atomic declaration of a school closure (TD-05, UC-OPS-12).
/// </summary>
public sealed class DeclareSchoolClosureHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectAcademicYearByIdSql = """
        SELECT
            id AS Id,
            start_date AS StartDate,
            end_date AS EndDate,
            status AS Status
        FROM operations.academic_years
        WHERE id = @AcademicYearId;
        """;

    private const string _selectAcademicYearByDateSql = """
        SELECT TOP 1
            id AS Id,
            start_date AS StartDate,
            end_date AS EndDate,
            status AS Status
        FROM operations.academic_years
        WHERE start_date <= @StartDate AND end_date >= @StartDate
        ORDER BY CASE WHEN status = 'active' THEN 1 ELSE 2 END, created_at DESC;
        """;

    private const string _selectCalendarByIdSql = """
        SELECT
            id AS Id,
            academic_year_id AS AcademicYearId,
            version AS Version,
            publication_status AS PublicationStatus
        FROM operations.school_calendars
        WHERE id = @CalendarId;
        """;

    private const string _selectCalendarByAcademicYearSql = """
        SELECT TOP 1
            id AS Id,
            academic_year_id AS AcademicYearId,
            version AS Version,
            publication_status AS PublicationStatus
        FROM operations.school_calendars
        WHERE academic_year_id = @AcademicYearId
        ORDER BY CASE WHEN publication_status = 'published' THEN 1 ELSE 2 END, created_at DESC;
        """;

    private const string _insertSchoolClosureSql = """
        INSERT INTO operations.school_closures
        (
            id,
            academic_year_id,
            closure_number,
            reason_category,
            reason_details,
            start_date,
            end_date,
            affected_campus,
            affected_population,
            status,
            authorized_by,
            authorized_date,
            communication_status,
            recovery_plan_notes,
            created_at,
            created_by
        )
        VALUES
        (
            @Id,
            @AcademicYearId,
            @ClosureNumber,
            @ReasonCategory,
            @ReasonDetails,
            @StartDate,
            @EndDate,
            @AffectedCampus,
            @AffectedPopulation,
            @Status,
            @AuthorizedBy,
            @AuthorizedDate,
            @CommunicationStatus,
            @RecoveryPlanNotes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private const string _insertCalendarEntrySql = """
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
    /// Declares a school closure atomically, adding closure records, non-instructional calendar entries,
    /// and logging versioned amendments if the target calendar is published (TD-05, UC-OPS-12).
    /// </summary>
    public async Task<Result<SchoolClosureResponse>> HandleAsync(
        DeclareSchoolClosureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Resolve parent academic year
        AcademicYearContextRow? academicYear = null;
        if (request.AcademicYearId.HasValue)
        {
            academicYear = await connection.QuerySingleOrDefaultAsync<AcademicYearContextRow>(new CommandDefinition(
                _selectAcademicYearByIdSql,
                new { AcademicYearId = request.AcademicYearId.Value },
                cancellationToken: cancellationToken));

            if (academicYear is null)
            {
                return Result.Failure<SchoolClosureResponse>(
                    DomainError.NotFound(
                        "Operations.AcademicYearNotFound",
                        $"Academic year with ID '{request.AcademicYearId.Value}' was not found."));
            }
        }
        else if (request.CalendarId.HasValue)
        {
            var calendarContext = await connection.QuerySingleOrDefaultAsync<CalendarContextRow>(new CommandDefinition(
                _selectCalendarByIdSql,
                new { CalendarId = request.CalendarId.Value },
                cancellationToken: cancellationToken));

            if (calendarContext is null)
            {
                return Result.Failure<SchoolClosureResponse>(
                    DomainError.NotFound(
                        "Operations.SchoolCalendarNotFound",
                        $"School calendar with ID '{request.CalendarId.Value}' was not found."));
            }

            academicYear = await connection.QuerySingleOrDefaultAsync<AcademicYearContextRow>(new CommandDefinition(
                _selectAcademicYearByIdSql,
                new { AcademicYearId = calendarContext.AcademicYearId },
                cancellationToken: cancellationToken));
        }
        else
        {
            academicYear = await connection.QuerySingleOrDefaultAsync<AcademicYearContextRow>(new CommandDefinition(
                _selectAcademicYearByDateSql,
                new { StartDate = request.StartDate },
                cancellationToken: cancellationToken));
        }

        if (academicYear is null)
        {
            return Result.Failure<SchoolClosureResponse>(
                DomainError.NotFound(
                    "Operations.AcademicYearNotFound",
                    $"No academic year could be resolved for closure commencement date '{request.StartDate:yyyy-MM-dd}'."));
        }

        // 2. Validate date boundaries against parent academic year
        if (request.StartDate < academicYear.StartDate || request.EndDate > academicYear.EndDate)
        {
            return Result.Failure<SchoolClosureResponse>(
                DomainError.Validation(
                    "Operations.DateOutOfBounds",
                    $"Closure dates must fall within academic year date range ({academicYear.StartDate:yyyy-MM-dd} to {academicYear.EndDate:yyyy-MM-dd})."));
        }

        // 3. Resolve parent school calendar
        CalendarContextRow? calendar = null;
        if (request.CalendarId.HasValue)
        {
            calendar = await connection.QuerySingleOrDefaultAsync<CalendarContextRow>(new CommandDefinition(
                _selectCalendarByIdSql,
                new { CalendarId = request.CalendarId.Value },
                cancellationToken: cancellationToken));
        }
        else
        {
            calendar = await connection.QuerySingleOrDefaultAsync<CalendarContextRow>(new CommandDefinition(
                _selectCalendarByAcademicYearSql,
                new { AcademicYearId = academicYear.Id },
                cancellationToken: cancellationToken));
        }

        if (calendar is null)
        {
            return Result.Failure<SchoolClosureResponse>(
                DomainError.NotFound(
                    "Operations.SchoolCalendarNotFound",
                    $"No school calendar was found for academic year ID '{academicYear.Id}'. A calendar must exist before declaring closures."));
        }

        var closureId = Guid.NewGuid();
        var calendarEntryId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var createdBy = currentUser.Username ?? "system";
        var authorizedBy = string.IsNullOrWhiteSpace(request.AuthorizedBy) ? createdBy : request.AuthorizedBy.Trim();
        var authorizedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var affectedCampus = string.IsNullOrWhiteSpace(request.AffectedCampus) ? "All" : request.AffectedCampus.Trim();
        var affectedPopulation = string.IsNullOrWhiteSpace(request.AffectedPopulation)
            ? "all"
            : request.AffectedPopulation.Trim().ToLowerInvariant();
        var isPublished = string.Equals(calendar.PublicationStatus, "published", StringComparison.OrdinalIgnoreCase);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // 4. Generate unique tracking code CLS-YYYY-NNN
        var closureNumber = await SchoolClosureHelper.GenerateClosureNumberAsync(
            connection,
            transaction,
            request.StartDate.Year,
            cancellationToken);

        // 5. Insert closure record into operations.school_closures
        await connection.ExecuteAsync(new CommandDefinition(
            _insertSchoolClosureSql,
            new
            {
                Id = closureId,
                AcademicYearId = academicYear.Id,
                ClosureNumber = closureNumber,
                ReasonCategory = request.ReasonCategory.Trim().ToLowerInvariant(),
                ReasonDetails = request.ReasonDetails.Trim(),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                AffectedCampus = affectedCampus,
                AffectedPopulation = affectedPopulation,
                Status = "authorized",
                AuthorizedBy = authorizedBy,
                AuthorizedDate = authorizedDate,
                CommunicationStatus = "pending",
                RecoveryPlanNotes = request.RecoveryPlanNotes,
                CreatedAt = createdAt,
                CreatedBy = createdBy,
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 6. Insert corresponding non-instructional entry into operations.calendar_entries
        var entryEndDate = request.EndDate == request.StartDate ? null : (DateOnly?)request.EndDate;
        await connection.ExecuteAsync(new CommandDefinition(
            _insertCalendarEntrySql,
            new
            {
                Id = calendarEntryId,
                CalendarId = calendar.Id,
                EntryDate = request.StartDate,
                EndDate = entryEndDate,
                Designation = "school_closure",
                Name = $"School Closure: {closureNumber}",
                Description = request.ReasonDetails.Trim(),
                IsInstructional = false,
                ClosureId = (Guid?)closureId,
                AppliesTo = affectedCampus,
                CreatedAt = createdAt,
                CreatedBy = createdBy,
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 7. If parent calendar is published, log amendment record and increment calendar minor version
        if (isPublished)
        {
            var fromVersion = calendar.Version;
            var toVersion = CalendarAmendmentHelper.BumpMinorVersion(fromVersion);
            var amendmentYear = DateTime.UtcNow.Year;
            var amendmentNumber = await CalendarAmendmentHelper.GenerateAmendmentNumberAsync(
                connection,
                transaction,
                amendmentYear,
                cancellationToken);

            var amendmentId = Guid.NewGuid();
            var changeSummary = $"School closure '{closureNumber}' declared ({request.ReasonCategory.Trim()}) from {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}.";
            var newValueJson = JsonSerializer.Serialize(new
            {
                ClosureId = closureId,
                ClosureNumber = closureNumber,
                request.StartDate,
                request.EndDate,
                ReasonCategory = request.ReasonCategory.Trim(),
                ReasonDetails = request.ReasonDetails.Trim(),
                AffectedCampus = affectedCampus,
                AffectedPopulation = affectedPopulation,
            });

            await connection.ExecuteAsync(new CommandDefinition(
                _insertAmendmentSql,
                new
                {
                    Id = amendmentId,
                    CalendarId = calendar.Id,
                    AmendmentNumber = amendmentNumber,
                    FromVersion = fromVersion,
                    ToVersion = toVersion,
                    AmendmentType = "closure_addition",
                    AffectedEntryId = (Guid?)calendarEntryId,
                    ChangeSummary = changeSummary,
                    PreviousValue = (string?)null,
                    NewValue = newValueJson,
                    Reason = request.ReasonDetails.Trim(),
                    EffectiveDate = request.StartDate,
                    AuthorizedBy = authorizedBy,
                    AuthorizedDate = authorizedDate,
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
                    CalendarId = calendar.Id,
                    ToVersion = toVersion,
                    UpdatedAt = createdAt,
                    UpdatedBy = createdBy,
                },
                transaction: transaction,
                cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new SchoolClosureResponse(
            closureId,
            academicYear.Id,
            closureNumber,
            request.ReasonCategory.Trim().ToLowerInvariant(),
            request.ReasonDetails.Trim(),
            request.StartDate,
            request.EndDate,
            affectedCampus,
            affectedPopulation,
            "authorized",
            authorizedBy,
            authorizedDate,
            "pending",
            request.RecoveryPlanNotes,
            createdAt,
            createdBy,
            null,
            null));
    }

    private sealed record AcademicYearContextRow(
        Guid Id,
        DateOnly StartDate,
        DateOnly EndDate,
        string Status);

    private sealed record CalendarContextRow(
        Guid Id,
        Guid AcademicYearId,
        string Version,
        string PublicationStatus);
}
