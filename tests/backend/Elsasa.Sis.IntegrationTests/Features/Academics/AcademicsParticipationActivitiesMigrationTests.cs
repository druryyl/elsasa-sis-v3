namespace Elsasa.Sis.IntegrationTests.Features.Academics;

using Elsasa.Sis.Database;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

/// <summary>
/// Integration test suite verifying DbUp schema migration 20261105_0401 (IS009 / CR-ACD-PARTICIPATION-LEARNING-ACTIVITIES, P1-S01, P6-S15, ADR-002, ADR-006).
/// Validates script journaling via DatabaseMigrator, provisioning of all 6 authoritative Submodule 2 domain tables in schema academics
/// (academic_participations, learning_activities, attendance_records, attendance_corrections, assignments, submissions),
/// columns, indexes, intra-schema foreign keys with ON DELETE NO ACTION, filtered unique index UQ_academics_participations_active,
/// unique constraint UQ_academics_attendance_records_activity_student, and check constraints across all 6 tables.
/// </summary>
public sealed class AcademicsParticipationActivitiesMigrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _migrationScriptName = "20261105_0401_Academics_ParticipationActivities_Tables.sql";

    [SkippableFact]
    public void AcademicsParticipationActivitiesMigration_Should_RunSuccessfullyViaDatabaseMigrator()
    {
        SkipIfDatabaseUnavailable();

        var success = DatabaseMigrator.UpgradeDatabase(Factory.ContainerFixture.ConnectionString);

        success.Should().BeTrue("DatabaseMigrator.UpgradeDatabase should succeed idempotently on target database");
    }

    [SkippableFact]
    public async Task AcademicsParticipationActivitiesMigration_Should_BeJournaledInSchemaVersions()
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
    public async Task AcademicParticipationsTable_Should_ContainAllColumnsAndIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'academic_participations';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "student_id",
            "learning_unit_id",
            "academic_period_id",
            "academic_period_label",
            "participation_type",
            "attempt_number",
            "source",
            "section_label",
            "status",
            "start_date",
            "end_date",
            "completion_date",
            "source_institution",
            "external_grade_or_credit",
            "recognition_basis",
            "approved_by",
            "student_learning_plan_id",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.academic_participations') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_academic_participations");
        indexes.Should().Contain("UQ_academics_participations_active");
        indexes.Should().Contain("IX_academics_participations_unit_period");
        indexes.Should().Contain("IX_academics_participations_student_period");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('academics.academic_participations');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_academic_participations_unit");
        var unitFk = fks.Single(fk => fk.Name == "FK_academics_academic_participations_unit");
        unitFk.DeleteReferentialAction.Should().Be(0, "academic participations unit FK must specify ON DELETE NO ACTION");
    }

    [SkippableFact]
    public async Task LearningActivitiesTable_Should_ContainAllColumnsAndIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'learning_activities';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "learning_unit_id",
            "activity_type",
            "title",
            "description",
            "section_label",
            "activity_date",
            "start_time",
            "end_time",
            "teacher_assignment_id",
            "teacher_name",
            "status",
            "is_attendance_required",
            "delivery_notes",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.learning_activities') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_learning_activities");
        indexes.Should().Contain("IX_academics_learning_activities_unit_date");
        indexes.Should().Contain("IX_academics_learning_activities_date_status");
        indexes.Should().Contain("IX_academics_learning_activities_section_date");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('academics.learning_activities');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_learning_activities_unit");
        var unitFk = fks.Single(fk => fk.Name == "FK_academics_learning_activities_unit");
        unitFk.DeleteReferentialAction.Should().Be(0, "learning activities unit FK must specify ON DELETE NO ACTION");
    }

    [SkippableFact]
    public async Task AttendanceRecordsTable_Should_ContainAllColumnsAndIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'attendance_records';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "learning_activity_id",
            "student_id",
            "status",
            "recorded_by",
            "recorded_date",
            "evidence",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.attendance_records') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_attendance_records");
        indexes.Should().Contain("UQ_academics_attendance_records_activity_student");
        indexes.Should().Contain("IX_academics_attendance_records_student_date");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('academics.attendance_records');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_attendance_records_activity");
        var activityFk = fks.Single(fk => fk.Name == "FK_academics_attendance_records_activity");
        activityFk.DeleteReferentialAction.Should().Be(0, "attendance records activity FK must specify ON DELETE NO ACTION");
    }

    [SkippableFact]
    public async Task AttendanceCorrectionsTable_Should_ContainAllColumnsAndIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'attendance_corrections';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "attendance_record_id",
            "previous_status",
            "new_status",
            "reason",
            "corrected_by",
            "corrected_date",
            "evidence",
            "created_at"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.attendance_corrections') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_attendance_corrections");
        indexes.Should().Contain("IX_academics_attendance_corrections_record");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('academics.attendance_corrections');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_attendance_corrections_record");
        var recordFk = fks.Single(fk => fk.Name == "FK_academics_attendance_corrections_record");
        recordFk.DeleteReferentialAction.Should().Be(0, "attendance corrections record FK must specify ON DELETE NO ACTION");
    }

    [SkippableFact]
    public async Task AssignmentsTable_Should_ContainAllColumnsAndIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'assignments';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "learning_activity_id",
            "title",
            "description",
            "release_date",
            "due_date",
            "status",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.assignments') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_assignments");
        indexes.Should().Contain("IX_academics_assignments_activity");
        indexes.Should().Contain("IX_academics_assignments_release_status");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('academics.assignments');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_assignments_activity");
        var activityFk = fks.Single(fk => fk.Name == "FK_academics_assignments_activity");
        activityFk.DeleteReferentialAction.Should().Be(0, "assignments activity FK must specify ON DELETE NO ACTION");
    }

    [SkippableFact]
    public async Task SubmissionsTable_Should_ContainAllColumnsAndIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'submissions';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "assignment_id",
            "student_id",
            "status",
            "submitted_date",
            "submission_reference",
            "is_resubmission",
            "previous_submission_id",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.submissions') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_submissions");
        indexes.Should().Contain("IX_academics_submissions_assignment_student");
        indexes.Should().Contain("IX_academics_submissions_student_status");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('academics.submissions');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        var fkNames = fks.Select(f => f.Name).ToList();
        fkNames.Should().Contain("FK_academics_submissions_assignment");
        fkNames.Should().Contain("FK_academics_submissions_previous");

        fks.Should().AllSatisfy(fk =>
            fk.DeleteReferentialAction.Should().Be(0, "submissions foreign keys must specify ON DELETE NO ACTION"));
    }

    [SkippableFact]
    public async Task AcademicParticipations_FilteredUniqueIndex_Should_PreventMultipleActiveParticipations_ForSameStudentAndUnitAndPeriod()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum Unique Participation Test");
        var unitAId = await CreateLearningUnitAsync(structureId, "UNIT-A", "Unit Alpha");
        var unitBId = await CreateLearningUnitAsync(structureId, "UNIT-B", "Unit Beta");
        var studentId = Guid.NewGuid();
        var period1Id = Guid.NewGuid();
        var period2Id = Guid.NewGuid();

        const string insertParticipationSql = """
            INSERT INTO academics.academic_participations (
                id, student_id, learning_unit_id, academic_period_id, academic_period_label,
                participation_type, attempt_number, source, status, start_date, created_by
            ) VALUES (
                @Id, @StudentId, @LearningUnitId, @AcademicPeriodId, '2026/2027 Ganjil',
                'standard', 1, 'assigned', @Status, '2026-08-01', 'test-user'
            );
            """;

        // 1. First active participation for student in Unit A during Period 1 must succeed
        var firstActiveId = Guid.NewGuid();
        await ExecuteAsync(insertParticipationSql, new
        {
            Id = firstActiveId,
            StudentId = studentId,
            LearningUnitId = unitAId,
            AcademicPeriodId = period1Id,
            Status = "in_progress"
        });

        // 2. Second active participation for same student, unit, and period must violate filtered unique index
        var actDuplicate = async () =>
        {
            await ExecuteAsync(insertParticipationSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                LearningUnitId = unitAId,
                AcademicPeriodId = period1Id,
                Status = "in_progress"
            });
        };

        var ex = await actDuplicate.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Match(num => num == 2601 || num == 2627,
            "duplicate active (in_progress) participation must trigger SQL unique index violation (error 2601 or 2627)");

        // 3. Second participation for same student, unit, and period with status 'completed' must succeed
        var actCompleted = async () =>
        {
            await ExecuteAsync(insertParticipationSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                LearningUnitId = unitAId,
                AcademicPeriodId = period1Id,
                Status = "completed"
            });
        };
        await actCompleted.Should().NotThrowAsync();

        // 4. Second participation for same student, unit, and period with status 'withdrawn' must succeed
        var actWithdrawn = async () =>
        {
            await ExecuteAsync(insertParticipationSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                LearningUnitId = unitAId,
                AcademicPeriodId = period1Id,
                Status = "withdrawn"
            });
        };
        await actWithdrawn.Should().NotThrowAsync();

        // 5. Active participation for same student in DIFFERENT unit (Unit B) during same period must succeed
        var actDifferentUnit = async () =>
        {
            await ExecuteAsync(insertParticipationSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                LearningUnitId = unitBId,
                AcademicPeriodId = period1Id,
                Status = "in_progress"
            });
        };
        await actDifferentUnit.Should().NotThrowAsync();

        // 6. Active participation for same student in same unit (Unit A) in DIFFERENT period (Period 2) must succeed
        var actDifferentPeriod = async () =>
        {
            await ExecuteAsync(insertParticipationSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                LearningUnitId = unitAId,
                AcademicPeriodId = period2Id,
                Status = "in_progress"
            });
        };
        await actDifferentPeriod.Should().NotThrowAsync();
    }

    [SkippableFact]
    public async Task AttendanceRecords_UniqueConstraint_Should_PreventDuplicateRecords_ForSameActivityAndStudent()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum Unique Attendance Test");
        var unitId = await CreateLearningUnitAsync(structureId, "UNIT-ATT", "Unit Attendance");
        var activity1Id = await CreateLearningActivityAsync(unitId, "Lecture 1", "lesson");
        var activity2Id = await CreateLearningActivityAsync(unitId, "Lecture 2", "lesson");
        var student1Id = Guid.NewGuid();
        var student2Id = Guid.NewGuid();

        const string insertAttendanceSql = """
            INSERT INTO academics.attendance_records (
                id, learning_activity_id, student_id, status, recorded_by, recorded_date, created_by
            ) VALUES (
                @Id, @LearningActivityId, @StudentId, 'present', 'teacher-01', '2026-09-01', 'test-user'
            );
            """;

        // 1. First attendance record for student 1 in activity 1 must succeed
        await ExecuteAsync(insertAttendanceSql, new
        {
            Id = Guid.NewGuid(),
            LearningActivityId = activity1Id,
            StudentId = student1Id
        });

        // 2. Duplicate attendance record for same student 1 in same activity 1 must violate UQ_academics_attendance_records_activity_student
        var actDuplicate = async () =>
        {
            await ExecuteAsync(insertAttendanceSql, new
            {
                Id = Guid.NewGuid(),
                LearningActivityId = activity1Id,
                StudentId = student1Id
            });
        };

        var ex = await actDuplicate.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Match(num => num == 2601 || num == 2627,
            "duplicate attendance record for same activity and student must trigger SQL unique constraint violation (error 2601 or 2627)");

        // 3. Attendance record for different student 2 in same activity 1 must succeed
        var actDifferentStudent = async () =>
        {
            await ExecuteAsync(insertAttendanceSql, new
            {
                Id = Guid.NewGuid(),
                LearningActivityId = activity1Id,
                StudentId = student2Id
            });
        };
        await actDifferentStudent.Should().NotThrowAsync();

        // 4. Attendance record for same student 1 in different activity 2 must succeed
        var actDifferentActivity = async () =>
        {
            await ExecuteAsync(insertAttendanceSql, new
            {
                Id = Guid.NewGuid(),
                LearningActivityId = activity2Id,
                StudentId = student1Id
            });
        };
        await actDifferentActivity.Should().NotThrowAsync();
    }

    [SkippableFact]
    public async Task AcademicParticipations_CheckConstraints_Should_EnforceValidEnums()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum Participation CK Test");
        var unitId = await CreateLearningUnitAsync(structureId, "UNIT-CK", "Unit Check Constraints");

        const string baseSql = """
            INSERT INTO academics.academic_participations (
                id, student_id, learning_unit_id, academic_period_id, academic_period_label,
                participation_type, attempt_number, source, status, start_date, created_by
            ) VALUES (
                @Id, @StudentId, @LearningUnitId, @AcademicPeriodId, '2026/2027 Ganjil',
                @ParticipationType, 1, @Source, @Status, '2026-08-01', 'test-user'
            );
            """;

        // Invalid participation_type
        var actType = async () =>
        {
            await ExecuteAsync(baseSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                LearningUnitId = unitId,
                AcademicPeriodId = Guid.NewGuid(),
                ParticipationType = "invalid_type",
                Source = "assigned",
                Status = "in_progress"
            });
        };
        var exType = await actType.Should().ThrowAsync<SqlException>();
        exType.Which.Number.Should().Be(547, "invalid participation_type must trigger check constraint violation (547)");

        // Invalid source
        var actSource = async () =>
        {
            await ExecuteAsync(baseSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                LearningUnitId = unitId,
                AcademicPeriodId = Guid.NewGuid(),
                ParticipationType = "standard",
                Source = "invalid_source",
                Status = "in_progress"
            });
        };
        var exSource = await actSource.Should().ThrowAsync<SqlException>();
        exSource.Which.Number.Should().Be(547, "invalid participation source must trigger check constraint violation (547)");

        // Invalid status
        var actStatus = async () =>
        {
            await ExecuteAsync(baseSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                LearningUnitId = unitId,
                AcademicPeriodId = Guid.NewGuid(),
                ParticipationType = "standard",
                Source = "assigned",
                Status = "invalid_status"
            });
        };
        var exStatus = await actStatus.Should().ThrowAsync<SqlException>();
        exStatus.Which.Number.Should().Be(547, "invalid participation status must trigger check constraint violation (547)");
    }

    [SkippableFact]
    public async Task LearningActivities_CheckConstraints_Should_EnforceValidEnums()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum Activity CK Test");
        var unitId = await CreateLearningUnitAsync(structureId, "UNIT-ACT-CK", "Unit Activity CK");

        const string baseSql = """
            INSERT INTO academics.learning_activities (
                id, learning_unit_id, activity_type, title, activity_date, status, created_by
            ) VALUES (
                @Id, @UnitId, @ActivityType, 'Session Title', '2026-09-01', @Status, 'test-user'
            );
            """;

        // Invalid activity_type
        var actType = async () =>
        {
            await ExecuteAsync(baseSql, new
            {
                Id = Guid.NewGuid(),
                UnitId = unitId,
                ActivityType = "invalid_type",
                Status = "planned"
            });
        };
        var exType = await actType.Should().ThrowAsync<SqlException>();
        exType.Which.Number.Should().Be(547, "invalid activity_type must trigger check constraint violation (547)");

        // Invalid status
        var actStatus = async () =>
        {
            await ExecuteAsync(baseSql, new
            {
                Id = Guid.NewGuid(),
                UnitId = unitId,
                ActivityType = "lesson",
                Status = "invalid_status"
            });
        };
        var exStatus = await actStatus.Should().ThrowAsync<SqlException>();
        exStatus.Which.Number.Should().Be(547, "invalid activity status must trigger check constraint violation (547)");
    }

    [SkippableFact]
    public async Task AttendanceRecords_CheckConstraints_Should_EnforceValidEnums()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum Attendance CK Test");
        var unitId = await CreateLearningUnitAsync(structureId, "UNIT-REC-CK", "Unit Rec CK");
        var activityId = await CreateLearningActivityAsync(unitId, "Lecture CK", "lesson");

        const string baseSql = """
            INSERT INTO academics.attendance_records (
                id, learning_activity_id, student_id, status, recorded_by, recorded_date, created_by
            ) VALUES (
                @Id, @ActivityId, @StudentId, @Status, 'teacher-01', '2026-09-01', 'test-user'
            );
            """;

        // Invalid attendance status
        var actStatus = async () =>
        {
            await ExecuteAsync(baseSql, new
            {
                Id = Guid.NewGuid(),
                ActivityId = activityId,
                StudentId = Guid.NewGuid(),
                Status = "invalid_attendance_status"
            });
        };
        var exStatus = await actStatus.Should().ThrowAsync<SqlException>();
        exStatus.Which.Number.Should().Be(547, "invalid attendance status must trigger check constraint violation (547)");
    }

    [SkippableFact]
    public async Task AttendanceCorrections_CheckConstraint_Should_RejectBlankStatuses()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum Correction CK Test");
        var unitId = await CreateLearningUnitAsync(structureId, "UNIT-COR-CK", "Unit Cor CK");
        var activityId = await CreateLearningActivityAsync(unitId, "Lecture For Correction", "lesson");
        var recordId = await CreateAttendanceRecordAsync(activityId, Guid.NewGuid(), "absent");

        const string baseSql = """
            INSERT INTO academics.attendance_corrections (
                id, attendance_record_id, previous_status, new_status, reason, corrected_by, corrected_date
            ) VALUES (
                @Id, @RecordId, @PreviousStatus, @NewStatus, 'Doctor note provided', 'teacher-01', '2026-09-02'
            );
            """;

        // Empty previous_status
        var actEmptyPrev = async () =>
        {
            await ExecuteAsync(baseSql, new
            {
                Id = Guid.NewGuid(),
                RecordId = recordId,
                PreviousStatus = "   ",
                NewStatus = "present"
            });
        };
        var exPrev = await actEmptyPrev.Should().ThrowAsync<SqlException>();
        exPrev.Which.Number.Should().Be(547, "blank previous_status must trigger check constraint violation (547)");

        // Empty new_status
        var actEmptyNew = async () =>
        {
            await ExecuteAsync(baseSql, new
            {
                Id = Guid.NewGuid(),
                RecordId = recordId,
                PreviousStatus = "absent",
                NewStatus = ""
            });
        };
        var exNew = await actEmptyNew.Should().ThrowAsync<SqlException>();
        exNew.Which.Number.Should().Be(547, "blank new_status must trigger check constraint violation (547)");
    }

    [SkippableFact]
    public async Task Assignments_CheckConstraints_Should_EnforceValidEnums()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum Assignment CK Test");
        var unitId = await CreateLearningUnitAsync(structureId, "UNIT-ASG-CK", "Unit Asg CK");
        var activityId = await CreateLearningActivityAsync(unitId, "Lecture For Assignment", "lesson");

        const string baseSql = """
            INSERT INTO academics.assignments (
                id, learning_activity_id, title, release_date, status, created_by
            ) VALUES (
                @Id, @ActivityId, 'Essay 1', '2026-09-01', @Status, 'test-user'
            );
            """;

        // Invalid assignment status
        var actStatus = async () =>
        {
            await ExecuteAsync(baseSql, new
            {
                Id = Guid.NewGuid(),
                ActivityId = activityId,
                Status = "invalid_assignment_status"
            });
        };
        var exStatus = await actStatus.Should().ThrowAsync<SqlException>();
        exStatus.Which.Number.Should().Be(547, "invalid assignment status must trigger check constraint violation (547)");
    }

    [SkippableFact]
    public async Task Submissions_CheckConstraints_Should_EnforceValidEnums()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum Submission CK Test");
        var unitId = await CreateLearningUnitAsync(structureId, "UNIT-SUB-CK", "Unit Sub CK");
        var activityId = await CreateLearningActivityAsync(unitId, "Lecture For Submission", "lesson");
        var assignmentId = await CreateAssignmentAsync(activityId, "Project 1", "released");

        const string baseSql = """
            INSERT INTO academics.submissions (
                id, assignment_id, student_id, status, created_by
            ) VALUES (
                @Id, @AssignmentId, @StudentId, @Status, 'test-user'
            );
            """;

        // Invalid submission status
        var actStatus = async () =>
        {
            await ExecuteAsync(baseSql, new
            {
                Id = Guid.NewGuid(),
                AssignmentId = assignmentId,
                StudentId = Guid.NewGuid(),
                Status = "invalid_submission_status"
            });
        };
        var exStatus = await actStatus.Should().ThrowAsync<SqlException>();
        exStatus.Which.Number.Should().Be(547, "invalid submission status must trigger check constraint violation (547)");
    }

    [SkippableFact]
    public async Task IntraSchemaForeignKeys_Should_EnforceReferentialIntegrity_And_NoActionOnDelete()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum FK Test");
        var unitId = await CreateLearningUnitAsync(structureId, "UNIT-FK", "Unit FK Test");
        var nonExistentId = Guid.NewGuid();

        // 1. Foreign key on academic_participations.learning_unit_id -> learning_units(id)
        var actInvalidParticipationUnit = async () =>
        {
            await ExecuteAsync("""
                INSERT INTO academics.academic_participations (
                    id, student_id, learning_unit_id, academic_period_id, academic_period_label,
                    participation_type, attempt_number, source, status, start_date, created_by
                ) VALUES (
                    @Id, @StudentId, @LearningUnitId, @AcademicPeriodId, '2026/2027 Ganjil',
                    'standard', 1, 'assigned', 'in_progress', '2026-08-01', 'test-user'
                );
                """, new
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                LearningUnitId = nonExistentId,
                AcademicPeriodId = Guid.NewGuid()
            });
        };
        var ex1 = await actInvalidParticipationUnit.Should().ThrowAsync<SqlException>();
        ex1.Which.Number.Should().Be(547, "non-existent learning_unit_id in participation must violate FK constraint");

        // 2. Foreign key on learning_activities.learning_unit_id -> learning_units(id)
        var actInvalidActivityUnit = async () =>
        {
            await ExecuteAsync("""
                INSERT INTO academics.learning_activities (
                    id, learning_unit_id, activity_type, title, activity_date, status, created_by
                ) VALUES (
                    @Id, @UnitId, 'lesson', 'Invalid Activity', '2026-09-01', 'planned', 'test-user'
                );
                """, new
            {
                Id = Guid.NewGuid(),
                UnitId = nonExistentId
            });
        };
        var ex2 = await actInvalidActivityUnit.Should().ThrowAsync<SqlException>();
        ex2.Which.Number.Should().Be(547, "non-existent learning_unit_id in activity must violate FK constraint");

        // 3. Foreign key on attendance_records.learning_activity_id -> learning_activities(id)
        var actInvalidAttendanceActivity = async () =>
        {
            await ExecuteAsync("""
                INSERT INTO academics.attendance_records (
                    id, learning_activity_id, student_id, status, recorded_by, recorded_date, created_by
                ) VALUES (
                    @Id, @ActivityId, @StudentId, 'present', 'teacher-01', '2026-09-01', 'test-user'
                );
                """, new
            {
                Id = Guid.NewGuid(),
                ActivityId = nonExistentId,
                StudentId = Guid.NewGuid()
            });
        };
        var ex3 = await actInvalidAttendanceActivity.Should().ThrowAsync<SqlException>();
        ex3.Which.Number.Should().Be(547, "non-existent learning_activity_id in attendance record must violate FK constraint");

        // 4. Foreign key on attendance_corrections.attendance_record_id -> attendance_records(id)
        var actInvalidCorrectionRecord = async () =>
        {
            await ExecuteAsync("""
                INSERT INTO academics.attendance_corrections (
                    id, attendance_record_id, previous_status, new_status, reason, corrected_by, corrected_date
                ) VALUES (
                    @Id, @RecordId, 'absent', 'present', 'Administrative mistake', 'teacher-01', '2026-09-02'
                );
                """, new
            {
                Id = Guid.NewGuid(),
                RecordId = nonExistentId
            });
        };
        var ex4 = await actInvalidCorrectionRecord.Should().ThrowAsync<SqlException>();
        ex4.Which.Number.Should().Be(547, "non-existent attendance_record_id in correction must violate FK constraint");

        // 5. Foreign key on assignments.learning_activity_id -> learning_activities(id)
        var actInvalidAssignmentActivity = async () =>
        {
            await ExecuteAsync("""
                INSERT INTO academics.assignments (
                    id, learning_activity_id, title, release_date, status, created_by
                ) VALUES (
                    @Id, @ActivityId, 'Quiz 1', '2026-09-01', 'draft', 'test-user'
                );
                """, new
            {
                Id = Guid.NewGuid(),
                ActivityId = nonExistentId
            });
        };
        var ex5 = await actInvalidAssignmentActivity.Should().ThrowAsync<SqlException>();
        ex5.Which.Number.Should().Be(547, "non-existent learning_activity_id in assignment must violate FK constraint");

        // 6. Foreign key on submissions.assignment_id -> assignments(id)
        var actInvalidSubmissionAssignment = async () =>
        {
            await ExecuteAsync("""
                INSERT INTO academics.submissions (
                    id, assignment_id, student_id, status, created_by
                ) VALUES (
                    @Id, @AssignmentId, @StudentId, 'submitted', 'test-user'
                );
                """, new
            {
                Id = Guid.NewGuid(),
                AssignmentId = nonExistentId,
                StudentId = Guid.NewGuid()
            });
        };
        var ex6 = await actInvalidSubmissionAssignment.Should().ThrowAsync<SqlException>();
        ex6.Which.Number.Should().Be(547, "non-existent assignment_id in submission must violate FK constraint");

        // 7. Foreign key on submissions.previous_submission_id -> submissions(id)
        var actInvalidSubmissionPrevious = async () =>
        {
            var validActivityId = await CreateLearningActivityAsync(unitId, "Lecture For Sub Previous FK", "lesson");
            var validAssignmentId = await CreateAssignmentAsync(validActivityId, "Task With Previous", "released");

            await ExecuteAsync("""
                INSERT INTO academics.submissions (
                    id, assignment_id, student_id, status, is_resubmission, previous_submission_id, created_by
                ) VALUES (
                    @Id, @AssignmentId, @StudentId, 'submitted', 1, @PreviousId, 'test-user'
                );
                """, new
            {
                Id = Guid.NewGuid(),
                AssignmentId = validAssignmentId,
                StudentId = Guid.NewGuid(),
                PreviousId = nonExistentId
            });
        };
        var ex7 = await actInvalidSubmissionPrevious.Should().ThrowAsync<SqlException>();
        ex7.Which.Number.Should().Be(547, "non-existent previous_submission_id in submission must violate FK constraint");

        // 8. Test ON DELETE NO ACTION restricts deletion of parent when child exists
        var deleteRestrictedUnitId = await CreateLearningUnitAsync(structureId, "UNIT-DEL-CK", "Unit Delete Restrict");
        await CreateLearningActivityAsync(deleteRestrictedUnitId, "Activity Preventing Delete", "lesson");

        var actDeleteParentUnit = async () =>
        {
            await ExecuteAsync("DELETE FROM academics.learning_units WHERE id = @Id;", new { Id = deleteRestrictedUnitId });
        };
        var exDelete = await actDeleteParentUnit.Should().ThrowAsync<SqlException>();
        exDelete.Which.Number.Should().Be(547, "deleting learning unit with existing activities must fail due to ON DELETE NO ACTION");
    }

    private async Task<Guid> CreateLearningStructureAsync(string name)
    {
        var id = Guid.NewGuid();
        const string sql = """
            INSERT INTO academics.learning_structures (
                id, name, academic_period_id, academic_period_label, version, effective_date, status, created_by
            ) VALUES (
                @Id, @Name, @PeriodId, '2026/2027 Ganjil', '1.0', '2026-08-01', 'draft', 'test-user'
            );
            """;

        await ExecuteAsync(sql, new
        {
            Id = id,
            Name = name,
            PeriodId = Guid.NewGuid()
        });

        return id;
    }

    private async Task<Guid> CreateLearningUnitAsync(Guid structureId, string code, string name)
    {
        var id = Guid.NewGuid();
        const string sql = """
            INSERT INTO academics.learning_units (
                id, learning_structure_id, code, name, unit_type, status, created_by
            ) VALUES (
                @Id, @StructureId, @Code, @Name, 'subject', 'draft', 'test-user'
            );
            """;

        await ExecuteAsync(sql, new
        {
            Id = id,
            StructureId = structureId,
            Code = code,
            Name = name
        });

        return id;
    }

    private async Task<Guid> CreateLearningActivityAsync(Guid unitId, string title, string activityType)
    {
        var id = Guid.NewGuid();
        const string sql = """
            INSERT INTO academics.learning_activities (
                id, learning_unit_id, activity_type, title, activity_date, status, created_by
            ) VALUES (
                @Id, @UnitId, @ActivityType, @Title, '2026-09-01', 'planned', 'test-user'
            );
            """;

        await ExecuteAsync(sql, new
        {
            Id = id,
            UnitId = unitId,
            ActivityType = activityType,
            Title = title
        });

        return id;
    }

    private async Task<Guid> CreateAttendanceRecordAsync(Guid activityId, Guid studentId, string status)
    {
        var id = Guid.NewGuid();
        const string sql = """
            INSERT INTO academics.attendance_records (
                id, learning_activity_id, student_id, status, recorded_by, recorded_date, created_by
            ) VALUES (
                @Id, @ActivityId, @StudentId, @Status, 'teacher-01', '2026-09-01', 'test-user'
            );
            """;

        await ExecuteAsync(sql, new
        {
            Id = id,
            ActivityId = activityId,
            StudentId = studentId,
            Status = status
        });

        return id;
    }

    private async Task<Guid> CreateAssignmentAsync(Guid activityId, string title, string status)
    {
        var id = Guid.NewGuid();
        const string sql = """
            INSERT INTO academics.assignments (
                id, learning_activity_id, title, release_date, status, created_by
            ) VALUES (
                @Id, @ActivityId, @Title, '2026-09-01', @Status, 'test-user'
            );
            """;

        await ExecuteAsync(sql, new
        {
            Id = id,
            ActivityId = activityId,
            Title = title,
            Status = status
        });

        return id;
    }

    private sealed record ForeignKeyInfo(string Name, byte DeleteReferentialAction);
}
