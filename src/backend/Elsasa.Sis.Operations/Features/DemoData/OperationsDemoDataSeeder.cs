namespace Elsasa.Sis.Operations.Features.DemoData;

using System.Data;
using System.Globalization;
using Dapper;
using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Autonomous seeder for the Operations domain calendar structure (ADR-002, ADR-004, GAP-007).
/// Dynamically anchors all academic calendars, periods, entries, and closures
/// around execution date Today (DateTime.UtcNow.Date), situating Today
/// squarely four weeks into an ongoing, active Semester 2 while preserving
/// an immutable, certified Semester 1 in the recent past.
/// </summary>
public sealed class OperationsDemoDataSeeder(
    ISqlConnectionFactory connectionFactory) : IOperationsDemoDataSeeder
{
    private const string _createdBy = "demo-data-seeder";
    private const string _campusMain = "campus-main";

    private const string _insertAcademicYearSql = """
        INSERT INTO operations.academic_years (
            id, code, name, start_date, end_date, status,
            approved_by, approved_date, notes, created_at, created_by
        )
        VALUES (
            @Id, @Code, @Name, @StartDate, @EndDate, @Status,
            @ApprovedBy, @ApprovedDate, @Notes, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertOperationalPeriodSql = """
        INSERT INTO operations.operational_periods (
            id, academic_year_id, period_code, name, period_type,
            start_date, end_date, parent_period_id, campus_context,
            status, notes, created_at, created_by
        )
        VALUES (
            @Id, @AcademicYearId, @PeriodCode, @Name, @PeriodType,
            @StartDate, @EndDate, @ParentPeriodId, @CampusContext,
            @Status, @Notes, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertSchoolCalendarSql = """
        INSERT INTO operations.school_calendars (
            id, academic_year_id, name, scope_type, scope_reference,
            version, status, publication_status, published_at, published_by,
            approved_by, approved_date, notes, created_at, created_by
        )
        VALUES (
            @Id, @AcademicYearId, @Name, @ScopeType, @ScopeReference,
            @Version, @Status, @PublicationStatus, @PublishedAt, @PublishedBy,
            @ApprovedBy, @ApprovedDate, @Notes, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertCalendarEntrySql = """
        INSERT INTO operations.calendar_entries (
            id, calendar_id, entry_date, end_date, designation, name,
            description, is_instructional, closure_id, applies_to,
            created_at, created_by
        )
        VALUES (
            @Id, @CalendarId, @EntryDate, @EndDate, @Designation, @Name,
            @Description, @IsInstructional, @ClosureId, @AppliesTo,
            @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertSchoolClosureSql = """
        INSERT INTO operations.school_closures (
            id, academic_year_id, closure_number, reason_category,
            reason_details, start_date, end_date, affected_campus,
            affected_population, status, authorized_by, authorized_date,
            communication_status, recovery_plan_notes, created_at, created_by
        )
        VALUES (
            @Id, @AcademicYearId, @ClosureNumber, @ReasonCategory,
            @ReasonDetails, @StartDate, @EndDate, @AffectedCampus,
            @AffectedPopulation, @Status, @AuthorizedBy, @AuthorizedDate,
            @CommunicationStatus, @RecoveryPlanNotes, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertCalendarAmendmentSql = """
        INSERT INTO operations.calendar_amendments (
            id, calendar_id, amendment_number, from_version, to_version,
            amendment_type, affected_entry_id, change_summary,
            previous_value, new_value, reason, effective_date,
            authorized_by, authorized_date, downstream_impact_reviewed,
            created_at, created_by
        )
        VALUES (
            @Id, @CalendarId, @AmendmentNumber, @FromVersion, @ToVersion,
            @AmendmentType, @AffectedEntryId, @ChangeSummary,
            @PreviousValue, @NewValue, @Reason, @EffectiveDate,
            @AuthorizedBy, @AuthorizedDate, @DownstreamImpactReviewed,
            @CreatedAt, @CreatedBy
        );
        """;

    public async Task<OperationsSeedResult> SeedOperationsAsync(
        DateTime baseDate,
        CancellationToken cancellationToken = default)
    {
        var today = baseDate.Date;

        // Compute dynamic timeline relative to baseDate (GAP-007, OQ-011)
        var academicYearStartDate = today.AddDays(-180);
        var academicYearEndDate = today.AddDays(120);

        var sem1StartDate = today.AddDays(-180);
        var sem1EndDate = today.AddDays(-35);

        var sem2StartDate = today.AddDays(-30);
        var sem2EndDate = today.AddDays(120);

        var sem3StartDate = today.AddDays(125);
        var sem3EndDate = today.AddDays(270);

        var academicYearId = Guid.NewGuid();
        var sem1Id = Guid.NewGuid();
        var sem2Id = Guid.NewGuid();
        var sem3Id = Guid.NewGuid();
        var schoolCalendarId = Guid.NewGuid();
        var closure1Id = Guid.NewGuid();
        var closure2Id = Guid.NewGuid();
        var amendmentId = Guid.NewGuid();

        var now = DateTimeOffset.UtcNow;
        var academicYearCode = "AY-2026-2027";

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Academic Year
            await connection.ExecuteAsync(new CommandDefinition(
                _insertAcademicYearSql,
                new
                {
                    Id = academicYearId,
                    Code = academicYearCode,
                    Name = "Tahun Pelajaran 2026/2027",
                    StartDate = academicYearStartDate,
                    EndDate = academicYearEndDate,
                    Status = "active",
                    ApprovedBy = "Kepala Sekolah",
                    ApprovedDate = academicYearStartDate,
                    Notes = "Hagios School of Life Academic Year 2026/2027",
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // 2. Operational Periods (Semesters)
            // Semester 1 (Historical, closed)
            await connection.ExecuteAsync(new CommandDefinition(
                _insertOperationalPeriodSql,
                new
                {
                    Id = sem1Id,
                    AcademicYearId = academicYearId,
                    PeriodCode = $"SEM1-2026",
                    Name = "Semester 1 TP 2026/2027",
                    PeriodType = "semester",
                    StartDate = sem1StartDate,
                    EndDate = sem1EndDate,
                    ParentPeriodId = (Guid?)null,
                    CampusContext = "All Campuses",
                    Status = "closed",
                    Notes = "Historical Semester 1 - certified and closed",
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // Semester 2 (Current active - Today is ~4 weeks in)
            await connection.ExecuteAsync(new CommandDefinition(
                _insertOperationalPeriodSql,
                new
                {
                    Id = sem2Id,
                    AcademicYearId = academicYearId,
                    PeriodCode = $"SEM2-2027",
                    Name = "Semester 2 TP 2026/2027",
                    PeriodType = "semester",
                    StartDate = sem2StartDate,
                    EndDate = sem2EndDate,
                    ParentPeriodId = (Guid?)null,
                    CampusContext = "All Campuses",
                    Status = "active",
                    Notes = "Current active semester - Today is approximately 4 weeks into this period",
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // Semester 3 (Planned intake)
            await connection.ExecuteAsync(new CommandDefinition(
                _insertOperationalPeriodSql,
                new
                {
                    Id = sem3Id,
                    AcademicYearId = academicYearId,
                    PeriodCode = $"SEM1-2027",
                    Name = "Semester 1 TP 2027/2028",
                    PeriodType = "semester",
                    StartDate = sem3StartDate,
                    EndDate = sem3EndDate,
                    ParentPeriodId = (Guid?)null,
                    CampusContext = "All Campuses",
                    Status = "planned",
                    Notes = "Planned intake period for next academic year",
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // 3. School Calendar
            await connection.ExecuteAsync(new CommandDefinition(
                _insertSchoolCalendarSql,
                new
                {
                    Id = schoolCalendarId,
                    AcademicYearId = academicYearId,
                    Name = "Kalender Hagios School of Life 2026/2027 - Campus Main",
                    ScopeType = "campus",
                    ScopeReference = "campus-main",
                    Version = "1.0",
                    Status = "approved",
                    PublicationStatus = "published",
                    PublishedAt = now,
                    PublishedBy = "Calendar Authority",
                    ApprovedBy = "Kepala Sekolah",
                    ApprovedDate = academicYearStartDate,
                    Notes = "Main campus school calendar for AY 2026/2027",
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // 4. Calendar Entries - Weekday instructional days across Sem 1 and Sem 2
            // Semester 1 instructional days (sample: first 2 weeks)
            for (int week = 0; week < 10; week++)
            {
                for (int day = 1; day <= 5; day++) // Mon-Fri
                {
                    var entryDate = sem1StartDate.AddDays(week * 7 + day - 1);
                    if (entryDate > sem1EndDate) break;

                    await connection.ExecuteAsync(new CommandDefinition(
                        _insertCalendarEntrySql,
                        new
                        {
                            Id = Guid.NewGuid(),
                            CalendarId = schoolCalendarId,
                            EntryDate = entryDate,
                            EndDate = (DateOnly?)null,
                            Designation = "instructional_day",
                            Name = $"Hari Pelajaran {entryDate:dddd}",
                            Description = "Regular instructional day",
                            IsInstructional = true,
                            ClosureId = (Guid?)null,
                            AppliesTo = "All",
                            CreatedAt = now,
                            CreatedBy = _createdBy
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                }
            }

            // Semester 2 instructional days (sample: first 4 weeks to cover Today + future)
            for (int week = 0; week < 8; week++)
            {
                for (int day = 1; day <= 5; day++) // Mon-Fri
                {
                    var entryDate = sem2StartDate.AddDays(week * 7 + day - 1);
                    if (entryDate > sem2EndDate) break;

                    await connection.ExecuteAsync(new CommandDefinition(
                        _insertCalendarEntrySql,
                        new
                        {
                            Id = Guid.NewGuid(),
                            CalendarId = schoolCalendarId,
                            EntryDate = entryDate,
                            EndDate = (DateOnly?)null,
                            Designation = "instructional_day",
                            Name = $"Hari Pelajaran {entryDate:dddd}",
                            Description = "Regular instructional day",
                            IsInstructional = true,
                            ClosureId = (Guid?)null,
                            AppliesTo = "All",
                            CreatedAt = now,
                            CreatedBy = _createdBy
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                }
            }

            // 5. School Closures
            // Scheduled mid-term retreat closure (10-8 days before Today)
            var retreatStart = today.AddDays(-10);
            var retreatEnd = today.AddDays(-8);
            await connection.ExecuteAsync(new CommandDefinition(
                _insertSchoolClosureSql,
                new
                {
                    Id = closure1Id,
                    AcademicYearId = academicYearId,
                    ClosureNumber = "CLS-2026-001",
                    ReasonCategory = "institutional_policy",
                    ReasonDetails = "Scheduled mid-term spiritual retreat for all students and staff",
                    StartDate = retreatStart,
                    EndDate = retreatEnd,
                    AffectedCampus = _campusMain,
                    AffectedPopulation = "all",
                    Status = "lifted",
                    AuthorizedBy = "Kepala Sekolah",
                    AuthorizedDate = retreatStart.AddDays(-30),
                    CommunicationStatus = "announced",
                    RecoveryPlanNotes = "Make-up instructional days scheduled during enrichment week",
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // Calendar entries for retreat closure
            for (var d = retreatStart; d <= retreatEnd; d = d.AddDays(1))
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    _insertCalendarEntrySql,
                    new
                    {
                        Id = Guid.NewGuid(),
                        CalendarId = schoolCalendarId,
                        EntryDate = d,
                        EndDate = (DateOnly?)null,
                        Designation = "school_closure",
                        Name = "Mid-Term Spiritual Retreat",
                        Description = "School closed for scheduled spiritual retreat",
                        IsInstructional = false,
                        ClosureId = closure1Id,
                        AppliesTo = "All",
                        CreatedAt = now,
                        CreatedBy = _createdBy
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
            }

            // Emergency weather closure (4 days before Today)
            var emergencyDate = today.AddDays(-4);
            await connection.ExecuteAsync(new CommandDefinition(
                _insertSchoolClosureSql,
                new
                {
                    Id = closure2Id,
                    AcademicYearId = academicYearId,
                    ClosureNumber = "CLS-2026-002",
                    ReasonCategory = "weather_disaster",
                    ReasonDetails = "Severe flooding in Yogyakarta area - campus inaccessible",
                    StartDate = emergencyDate,
                    EndDate = emergencyDate,
                    AffectedCampus = _campusMain,
                    AffectedPopulation = "all",
                    Status = "lifted",
                    AuthorizedBy = "Kepala Sekolah",
                    AuthorizedDate = emergencyDate.AddDays(-1),
                    CommunicationStatus = "announced",
                    RecoveryPlanNotes = "Single day closure; lesson plans shifted to following week",
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                _insertCalendarEntrySql,
                new
                {
                    Id = Guid.NewGuid(),
                    CalendarId = schoolCalendarId,
                    EntryDate = emergencyDate,
                    EndDate = (DateOnly?)null,
                    Designation = "school_closure",
                    Name = "Emergency Weather Closure - Flooding",
                    Description = "School closed due to severe flooding",
                    IsInstructional = false,
                    ClosureId = closure2Id,
                    AppliesTo = "All",
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // 6. Calendar Amendment for the emergency closure
            await connection.ExecuteAsync(new CommandDefinition(
                _insertCalendarAmendmentSql,
                new
                {
                    Id = amendmentId,
                    CalendarId = schoolCalendarId,
                    AmendmentNumber = "AMD-CAL-2026-001",
                    FromVersion = "1.0",
                    ToVersion = "1.1",
                    AmendmentType = "closure_addition",
                    AffectedEntryId = (Guid?)null,
                    ChangeSummary = "Added emergency closure entry for severe flooding on " + emergencyDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    PreviousValue = (string?)null,
                    NewValue = (string?)null,
                    Reason = "Emergency weather event requiring immediate calendar amendment",
                    EffectiveDate = emergencyDate,
                    AuthorizedBy = "Kepala Sekolah",
                    AuthorizedDate = emergencyDate.AddDays(-1),
                    DownstreamImpactReviewed = true,
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);

            return new OperationsSeedResult(
                AcademicYearId: academicYearId,
                AcademicYearCode: academicYearCode,
                HistoricalPeriodId: sem1Id,
                ActivePeriodId: sem2Id,
                FuturePeriodId: sem3Id,
                SchoolCalendarId: schoolCalendarId,
                BaseDate: today);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
