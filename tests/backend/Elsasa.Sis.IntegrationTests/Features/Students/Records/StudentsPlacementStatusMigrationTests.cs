namespace Elsasa.Sis.IntegrationTests.Features.Students.Records;

using Elsasa.Sis.Database;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

/// <summary>
/// Integration tests verifying DbUp schema migration 20261104_0101 (IS005 / CR-STU-PLACEMENT-STATUS, P1-S01, P4-S10).
/// Validates script journaling and execution via DatabaseMigrator, domain table structures,
/// partial unique index single-active invariant enforcement, foreign keys, and idempotent data backfill.
/// </summary>
public sealed class StudentsPlacementStatusMigrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _migrationScriptName = "20261104_0101_Students_PlacementStatus_Tables.sql";

    [SkippableFact]
    public void PlacementMigration_Should_RunSuccessfullyViaDatabaseMigrator()
    {
        SkipIfDatabaseUnavailable();

        var success = DatabaseMigrator.UpgradeDatabase(Factory.ContainerFixture.ConnectionString);

        success.Should().BeTrue("DatabaseMigrator.UpgradeDatabase should succeed idempotently on target database");
    }

    [SkippableFact]
    public async Task PlacementMigration_Should_BeJournaledInSchemaVersions()
    {
        SkipIfDatabaseUnavailable();

        const string checkScriptSql = """
            SELECT COUNT(1)
            FROM dbo._SchemaVersions
            WHERE ScriptName LIKE '%' + @ScriptName;
            """;

        var count = await ExecuteScalarAsync<int>(checkScriptSql, new { ScriptName = _migrationScriptName });
        count.Should().Be(1, $"migration script '{_migrationScriptName}' should be applied and journaled");
    }

    [SkippableFact]
    public async Task AdministrativePlacementsTable_Should_ContainAllColumnsAndIndexes()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'students' AND TABLE_NAME = 'administrative_placements';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "student_id",
            "campus",
            "school_level",
            "grade",
            "academic_year",
            "effective_date",
            "program",
            "stream",
            "cohort",
            "homeroom",
            "end_date",
            "is_current",
            "reason",
            "authorized_by",
            "decision_reference",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name, has_filter, filter_definition
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('students.administrative_placements') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<dynamic>(indexesSql)).ToList();
        var indexNames = indexes.Select(i => (string)i.name).ToList();

        indexNames.Should().Contain("ux_administrative_placements_student_active");
        indexNames.Should().Contain("ix_students_placements_student");

        var partialUniqueIndex = indexes.Single(i => (string)i.name == "ux_administrative_placements_student_active");
        ((bool)partialUniqueIndex.has_filter).Should().BeTrue("ux_administrative_placements_student_active must be a filtered index");
        ((string)partialUniqueIndex.filter_definition).Should().Contain("is_current");
    }

    [SkippableFact]
    public async Task StudentStatusesTable_Should_ContainAllColumnsAndIndexes()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'students' AND TABLE_NAME = 'student_statuses';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "student_id",
            "status",
            "effective_date",
            "end_date",
            "is_current",
            "reason",
            "authorized_by",
            "decision_reference",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name, has_filter, filter_definition
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('students.student_statuses') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<dynamic>(indexesSql)).ToList();
        var indexNames = indexes.Select(i => (string)i.name).ToList();

        indexNames.Should().Contain("ux_student_statuses_student_active");
        indexNames.Should().Contain("ix_students_statuses_student");

        var partialUniqueIndex = indexes.Single(i => (string)i.name == "ux_student_statuses_student_active");
        ((bool)partialUniqueIndex.has_filter).Should().BeTrue("ux_student_statuses_student_active must be a filtered index");
        ((string)partialUniqueIndex.filter_definition).Should().Contain("is_current");
    }

    [SkippableFact]
    public async Task AdministrativeHistoryEntriesTable_Should_ContainAllColumnsAndIndexes()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'students' AND TABLE_NAME = 'administrative_history_entries';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "student_id",
            "event_type",
            "event_date",
            "detail",
            "authorized_by",
            "decision_reference",
            "is_correction",
            "corrects_entry_id",
            "created_at",
            "created_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('students.administrative_history_entries') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("ix_students_history_student");
    }

    [SkippableFact]
    public async Task PlacementAndStatusForeignKeys_Should_SpecifyNoAction()
    {
        SkipIfDatabaseUnavailable();

        const string fkSql = """
            SELECT delete_referential_action
            FROM sys.foreign_keys
            WHERE parent_object_id IN (
                OBJECT_ID('students.administrative_placements'),
                OBJECT_ID('students.student_statuses'),
                OBJECT_ID('students.administrative_history_entries')
            );
            """;

        var deleteActions = (await QueryAsync<int>(fkSql)).ToList();
        deleteActions.Should().NotBeEmpty();
        deleteActions.Should().AllSatisfy(action =>
            action.Should().Be(0, "all placement, status, and history foreign keys must specify ON DELETE NO ACTION"));
    }

    [SkippableFact]
    public async Task SingleActivePlacementInvariant_Should_RejectDuplicateActivePlacements()
    {
        SkipIfDatabaseUnavailable();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await CreateStudentAsync(suffix);

        const string insertPlacementSql = """
            INSERT INTO students.administrative_placements (
                id, student_id, campus, school_level, grade, academic_year,
                effective_date, is_current, created_at
            ) VALUES (
                @Id, @StudentId, @Campus, @SchoolLevel, @Grade, @AcademicYear,
                @EffectiveDate, @IsCurrent, SYSUTCDATETIME()
            );
            """;

        // Insert first active placement (is_current = 1) -> must succeed
        await ExecuteAsync(insertPlacementSql, new
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            Campus = "Main Campus",
            SchoolLevel = "Grade 1",
            Grade = "Grade 1",
            AcademicYear = "2026/2027",
            EffectiveDate = new DateTime(2026, 8, 1),
            IsCurrent = true
        });

        // Insert second active placement (is_current = 1) for the same student -> must violate ux_administrative_placements_student_active
        var actDuplicate = async () =>
        {
            await ExecuteAsync(insertPlacementSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                Campus = "North Campus",
                SchoolLevel = "Grade 2",
                Grade = "Grade 2",
                AcademicYear = "2026/2027",
                EffectiveDate = new DateTime(2026, 9, 1),
                IsCurrent = true
            });
        };

        var ex = await actDuplicate.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Match(num => num == 2601 || num == 2627,
            "duplicate active placement must trigger SQL unique index violation (error 2601 or 2627)");

        // Insert inactive placement (is_current = 0) for the same student -> must succeed
        var actInactive = async () =>
        {
            await ExecuteAsync(insertPlacementSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                Campus = "Past Campus",
                SchoolLevel = "Kindergarten",
                Grade = "KG",
                AcademicYear = "2025/2026",
                EffectiveDate = new DateTime(2025, 8, 1),
                IsCurrent = false
            });
        };

        await actInactive.Should().NotThrowAsync("multiple inactive placements are permitted by partial index filter");
    }

    [SkippableFact]
    public async Task SingleActiveStatusInvariant_Should_RejectDuplicateActiveStatuses()
    {
        SkipIfDatabaseUnavailable();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await CreateStudentAsync(suffix);

        const string insertStatusSql = """
            INSERT INTO students.student_statuses (
                id, student_id, status, effective_date, is_current, created_at
            ) VALUES (
                @Id, @StudentId, @Status, @EffectiveDate, @IsCurrent, SYSUTCDATETIME()
            );
            """;

        // Insert first active status (is_current = 1) -> must succeed
        await ExecuteAsync(insertStatusSql, new
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            Status = "active",
            EffectiveDate = new DateTime(2026, 8, 1),
            IsCurrent = true
        });

        // Insert second active status (is_current = 1) for the same student -> must violate ux_student_statuses_student_active
        var actDuplicate = async () =>
        {
            await ExecuteAsync(insertStatusSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                Status = "withdrawn",
                EffectiveDate = new DateTime(2026, 9, 1),
                IsCurrent = true
            });
        };

        var ex = await actDuplicate.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Match(num => num == 2601 || num == 2627,
            "duplicate active status must trigger SQL unique index violation (error 2601 or 2627)");

        // Insert inactive status (is_current = 0) for the same student -> must succeed
        var actInactive = async () =>
        {
            await ExecuteAsync(insertStatusSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                Status = "enrolled",
                EffectiveDate = new DateTime(2026, 8, 1),
                IsCurrent = false
            });
        };

        await actInactive.Should().NotThrowAsync("multiple inactive statuses are permitted by partial index filter");
    }

    [SkippableFact]
    public async Task BackfillLogic_Should_IdempotentlyCreateActivePlacementActiveStatusAndHistoryForEnrolledStudent()
    {
        SkipIfDatabaseUnavailable();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentNumber = $"STU-BKF-{suffix}";
        var (studentId, enrollmentId) = await CreateEnrolledStudentWithoutPlacementOrStatusAsync(suffix, studentNumber);

        // Verify initial state: student and enrollment exist, but no placements, statuses, or history entries
        var initialPlacementCount = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM students.administrative_placements WHERE student_id = @StudentId;",
            new { StudentId = studentId });
        var initialStatusCount = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM students.student_statuses WHERE student_id = @StudentId;",
            new { StudentId = studentId });
        var initialHistoryCount = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM students.administrative_history_entries WHERE student_id = @StudentId;",
            new { StudentId = studentId });

        initialPlacementCount.Should().Be(0);
        initialStatusCount.Should().Be(0);
        initialHistoryCount.Should().Be(0);

        // Execute idempotent backfill script logic (matching migration 20261104_0101)
        await RunBackfillScriptsAsync();

        // Assert active placement backfilled
        var placement = await QuerySingleOrDefaultAsync<dynamic>(
            """
            SELECT campus, school_level, grade, academic_year, program, cohort, is_current, reason, authorized_by, decision_reference, created_by
            FROM students.administrative_placements
            WHERE student_id = @StudentId;
            """,
            new { StudentId = studentId });

        Assert.NotNull(placement);
        ((string)placement!.campus).Should().Be("West Campus");
        ((string)placement.school_level).Should().Be("Grade 3");
        ((string)placement.grade).Should().Be("Grade 3");
        ((string)placement.academic_year).Should().Be("2026/2027");
        ((string)placement.program).Should().Be("Standard Track");
        ((string)placement.cohort).Should().Be("3-B");
        ((bool)placement.is_current).Should().BeTrue();
        ((string)placement.reason).Should().Be("Historical enrollment data backfill");
        ((string)placement.authorized_by).Should().Be("Principal Smith");
        ((string)placement.decision_reference).Should().Be("MIGRATION_20261104");
        ((string)placement.created_by).Should().Be("SYSTEM_MIGRATION");

        // Assert active status backfilled
        var status = await QuerySingleOrDefaultAsync<dynamic>(
            """
            SELECT status, is_current, reason, authorized_by, decision_reference, created_by
            FROM students.student_statuses
            WHERE student_id = @StudentId;
            """,
            new { StudentId = studentId });

        Assert.NotNull(status);
        ((string)status!.status).Should().Be("active");
        ((bool)status.is_current).Should().BeTrue();
        ((string)status.reason).Should().Be("Historical status backfill");
        ((string)status.authorized_by).Should().Be("SYSTEM_MIGRATION");
        ((string)status.decision_reference).Should().Be("MIGRATION_20261104");
        ((string)status.created_by).Should().Be("SYSTEM_MIGRATION");

        // Assert baseline intake history entry backfilled
        var history = await QuerySingleOrDefaultAsync<dynamic>(
            """
            SELECT event_type, detail, authorized_by, decision_reference, is_correction, created_by
            FROM students.administrative_history_entries
            WHERE student_id = @StudentId;
            """,
            new { StudentId = studentId });

        Assert.NotNull(history);
        ((string)history!.event_type).Should().Be("enrollment_completed");
        ((string)history.detail).Should().Contain(studentNumber);
        ((string)history.authorized_by).Should().Be("SYSTEM_MIGRATION");
        ((string)history.decision_reference).Should().Be("MIGRATION_20261104");
        ((bool)history.is_correction).Should().BeFalse();
        ((string)history.created_by).Should().Be("SYSTEM_MIGRATION");

        // Verify idempotency: executing backfill again produces 0 duplicate records
        await RunBackfillScriptsAsync();

        var finalPlacementCount = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM students.administrative_placements WHERE student_id = @StudentId;",
            new { StudentId = studentId });
        var finalStatusCount = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM students.student_statuses WHERE student_id = @StudentId;",
            new { StudentId = studentId });
        var finalHistoryCount = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM students.administrative_history_entries WHERE student_id = @StudentId;",
            new { StudentId = studentId });

        finalPlacementCount.Should().Be(1, "backfill must be idempotent for placements");
        finalStatusCount.Should().Be(1, "backfill must be idempotent for statuses");
        finalHistoryCount.Should().Be(1, "backfill must be idempotent for history entries");
    }

    private async Task<Guid> CreateStudentAsync(string suffix)
    {
        var applicantId = Guid.NewGuid();
        const string insertApplicantSql = """
            INSERT INTO students.applicants (
                id, first_name, last_name, full_name, date_of_birth, gender, nationality, created_at
            ) VALUES (
                @Id, @FirstName, @LastName, @FullName, '2015-05-10', 'Male', 'Indonesian', SYSUTCDATETIME()
            );
            """;

        await ExecuteAsync(insertApplicantSql, new
        {
            Id = applicantId,
            FirstName = $"ApplicantFirst{suffix}",
            LastName = $"ApplicantLast{suffix}",
            FullName = $"Applicant Full Name {suffix}"
        });

        var studentId = Guid.NewGuid();
        const string insertStudentSql = """
            INSERT INTO students.students (
                id, student_number, applicant_id, full_name, date_of_birth, gender,
                nationality, current_status, enrollment_date, created_at
            ) VALUES (
                @Id, @StudentNumber, @ApplicantId, @FullName, '2015-05-10', 'Male',
                'Indonesian', 'active', '2026-08-01', SYSUTCDATETIME()
            );
            """;

        await ExecuteAsync(insertStudentSql, new
        {
            Id = studentId,
            StudentNumber = $"STU-INV-{suffix}",
            ApplicantId = applicantId,
            FullName = $"Student Full Name {suffix}"
        });

        return studentId;
    }

    private async Task<(Guid StudentId, Guid EnrollmentId)> CreateEnrolledStudentWithoutPlacementOrStatusAsync(
        string suffix,
        string studentNumber)
    {
        var applicantId = Guid.NewGuid();
        const string insertApplicantSql = """
            INSERT INTO students.applicants (
                id, first_name, last_name, full_name, date_of_birth, gender, nationality, created_at
            ) VALUES (
                @Id, @FirstName, @LastName, @FullName, '2016-03-15', 'Female', 'Indonesian', SYSUTCDATETIME()
            );
            """;

        await ExecuteAsync(insertApplicantSql, new
        {
            Id = applicantId,
            FirstName = $"BackfillFirst{suffix}",
            LastName = $"BackfillLast{suffix}",
            FullName = $"Backfill Candidate {suffix}"
        });

        var applicationId = Guid.NewGuid();
        const string insertApplicationSql = """
            INSERT INTO students.admission_applications (
                id, applicant_id, academic_year_id, admission_period_id, application_number, status, application_type,
                requested_entry_level, requested_campus, registration_date, created_at
            ) VALUES (
                @Id, @ApplicantId, @AcademicYearId, @AdmissionPeriodId, @AppNumber, 'accepted', 'new',
                'Grade 3', 'West Campus', '2026-07-15', SYSUTCDATETIME()
            );
            """;

        await ExecuteAsync(insertApplicationSql, new
        {
            Id = applicationId,
            ApplicantId = applicantId,
            AcademicYearId = Guid.NewGuid(),
            AdmissionPeriodId = Guid.NewGuid(),
            AppNumber = $"APP-BKF-{suffix}"
        });

        var studentId = Guid.NewGuid();
        const string insertStudentSql = """
            INSERT INTO students.students (
                id, student_number, applicant_id, full_name, date_of_birth, gender,
                nationality, current_status, enrollment_date, created_at
            ) VALUES (
                @Id, @StudentNumber, @ApplicantId, @FullName, '2016-03-15', 'Female',
                'Indonesian', 'active', '2026-08-01', SYSUTCDATETIME()
            );
            """;

        await ExecuteAsync(insertStudentSql, new
        {
            Id = studentId,
            StudentNumber = studentNumber,
            ApplicantId = applicantId,
            FullName = $"Backfill Candidate {suffix}"
        });

        var enrollmentId = Guid.NewGuid();
        const string insertEnrollmentSql = """
            INSERT INTO students.enrollments (
                id, student_id, application_id, enrollment_type, enrollment_date,
                initial_campus, initial_level, initial_academic_year, initial_program, initial_cohort,
                approved_by, payment_waived, created_at
            ) VALUES (
                @Id, @StudentId, @ApplicationId, 'initial', '2026-08-01',
                'West Campus', 'Grade 3', '2026/2027', 'Standard Track', '3-B',
                'Principal Smith', 1, SYSUTCDATETIME()
            );
            """;

        await ExecuteAsync(insertEnrollmentSql, new
        {
            Id = enrollmentId,
            StudentId = studentId,
            ApplicationId = applicationId
        });

        return (studentId, enrollmentId);
    }

    private async Task RunBackfillScriptsAsync()
    {
        const string backfillPlacementSql = """
            INSERT INTO [students].[administrative_placements] (
                [id], [student_id], [campus], [school_level], [grade], [academic_year],
                [effective_date], [program], [stream], [cohort], [homeroom],
                [end_date], [is_current], [reason], [authorized_by], [decision_reference],
                [created_at], [created_by]
            )
            SELECT
                NEWID(),
                s.id,
                COALESCE(latest_enr.initial_campus, N'Main Campus'),
                COALESCE(latest_enr.initial_level, N'Default Level'),
                COALESCE(latest_enr.initial_level, N'Default Level'),
                COALESCE(latest_enr.initial_academic_year, N'2026/2027'),
                s.enrollment_date,
                latest_enr.initial_program,
                NULL,
                latest_enr.initial_cohort,
                NULL,
                NULL,
                1,
                N'Historical enrollment data backfill',
                COALESCE(latest_enr.approved_by, N'SYSTEM_MIGRATION'),
                N'MIGRATION_20261104',
                SYSUTCDATETIME(),
                N'SYSTEM_MIGRATION'
            FROM [students].[students] s
            OUTER APPLY (
                SELECT TOP 1 *
                FROM [students].[enrollments] e
                WHERE e.student_id = s.id
                ORDER BY e.enrollment_date DESC, e.created_at DESC
            ) latest_enr
            WHERE NOT EXISTS (
                SELECT 1 FROM [students].[administrative_placements] ap WHERE ap.student_id = s.id
            );
            """;

        const string backfillStatusSql = """
            INSERT INTO [students].[student_statuses] (
                [id], [student_id], [status], [effective_date], [end_date], [is_current],
                [reason], [authorized_by], [decision_reference], [created_at], [created_by]
            )
            SELECT
                NEWID(),
                s.id,
                COALESCE(s.current_status, N'active'),
                s.enrollment_date,
                NULL,
                1,
                N'Historical status backfill',
                N'SYSTEM_MIGRATION',
                N'MIGRATION_20261104',
                SYSUTCDATETIME(),
                N'SYSTEM_MIGRATION'
            FROM [students].[students] s
            WHERE NOT EXISTS (
                SELECT 1 FROM [students].[student_statuses] st WHERE st.student_id = s.id
            );
            """;

        const string backfillHistorySql = """
            INSERT INTO [students].[administrative_history_entries] (
                [id], [student_id], [event_type], [event_date], [detail],
                [authorized_by], [decision_reference], [is_correction], [corrects_entry_id],
                [created_at], [created_by]
            )
            SELECT
                NEWID(),
                s.id,
                N'enrollment_completed',
                s.enrollment_date,
                CONCAT(N'Officially enrolled with student number ', s.student_number),
                N'SYSTEM_MIGRATION',
                N'MIGRATION_20261104',
                0,
                NULL,
                SYSUTCDATETIME(),
                N'SYSTEM_MIGRATION'
            FROM [students].[students] s
            WHERE NOT EXISTS (
                SELECT 1 FROM [students].[administrative_history_entries] h WHERE h.student_id = s.id
            );
            """;

        await ExecuteAsync(backfillPlacementSql);
        await ExecuteAsync(backfillStatusSql);
        await ExecuteAsync(backfillHistorySql);
    }
}
