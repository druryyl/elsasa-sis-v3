namespace Elsasa.Sis.IntegrationTests.Features.Academics;

using Elsasa.Sis.Database;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

/// <summary>
/// Integration test suite verifying DbUp schema migration 20261107_0402 (IS013 / CR-ACD-STUDENT-LEARNING-PLAN, P1-S01, P5-S14, ADR-001, ADR-002, ADR-006).
/// Validates script journaling via DatabaseMigrator, provisioning of all 4 authoritative Submodule 5 domain tables in schema academics
/// (student_learning_plans, slp_learning_units, academic_plan_adjustments, slp_approvals), column names and data types,
/// partial unique index UQ_academics_slp_active enforcement, non-active plan co-existence, foreign key cascade deletes,
/// academic_participations SET NULL behavior on plan deletion, check constraints, and zero cross-schema foreign keys.
/// </summary>
public sealed class AcademicsStudentLearningPlanMigrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _migrationScriptName = "20261107_0402_Academics_StudentLearningPlan_Tables.sql";

    [SkippableFact]
    public void AcademicsStudentLearningPlanMigration_Should_RunSuccessfullyViaDatabaseMigrator()
    {
        SkipIfDatabaseUnavailable();

        var success = DatabaseMigrator.UpgradeDatabase(Factory.ContainerFixture.ConnectionString);

        success.Should().BeTrue("DatabaseMigrator.UpgradeDatabase should succeed idempotently on target database");
    }

    [SkippableFact]
    public async Task AcademicsStudentLearningPlanMigration_Should_BeJournaledInSchemaVersions()
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
    public async Task StudentLearningPlansTable_Should_ContainAllColumnsAndDataTypesAndIndexes()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME AS Name, DATA_TYPE AS DataType
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'student_learning_plans';
            """;

        var columns = (await QueryAsync<ColumnInfo>(columnsSql)).ToDictionary(c => c.Name, c => c.DataType, StringComparer.OrdinalIgnoreCase);

        columns.Should().ContainKey("id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("student_id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("academic_period_id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("academic_period_label").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("status").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("version").WhoseValue.Should().Be("int");
        columns.Should().ContainKey("notes").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("created_at").WhoseValue.Should().Be("datetimeoffset");
        columns.Should().ContainKey("created_by").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("updated_at").WhoseValue.Should().Be("datetimeoffset");
        columns.Should().ContainKey("updated_by").WhoseValue.Should().Be("nvarchar");

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.student_learning_plans') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_student_learning_plans");
        indexes.Should().Contain("UQ_academics_slp_active");
        indexes.Should().Contain("IX_academics_slp_student_period");
    }

    [SkippableFact]
    public async Task SlpLearningUnitsTable_Should_ContainAllColumnsAndDataTypesAndIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME AS Name, DATA_TYPE AS DataType
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'slp_learning_units';
            """;

        var columns = (await QueryAsync<ColumnInfo>(columnsSql)).ToDictionary(c => c.Name, c => c.DataType, StringComparer.OrdinalIgnoreCase);

        columns.Should().ContainKey("id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("student_learning_plan_id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("learning_unit_id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("sequence_order").WhoseValue.Should().Be("int");
        columns.Should().ContainKey("is_required").WhoseValue.Should().Be("bit");
        columns.Should().ContainKey("notes").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("created_at").WhoseValue.Should().Be("datetimeoffset");
        columns.Should().ContainKey("created_by").WhoseValue.Should().Be("nvarchar");

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.slp_learning_units') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_slp_learning_units");
        indexes.Should().Contain("UQ_academics_slp_units_plan_unit");
        indexes.Should().Contain("IX_academics_slp_units_learning_unit");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('academics.slp_learning_units');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_slp_units_plan");
        fks.Single(fk => fk.Name == "FK_academics_slp_units_plan").DeleteReferentialAction
            .Should().Be(1, "slp_learning_units plan FK must specify ON DELETE CASCADE");

        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_slp_units_unit");
        fks.Single(fk => fk.Name == "FK_academics_slp_units_unit").DeleteReferentialAction
            .Should().Be(0, "slp_learning_units unit FK must specify ON DELETE NO ACTION");
    }

    [SkippableFact]
    public async Task AcademicPlanAdjustmentsTable_Should_ContainAllColumnsAndDataTypesAndIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME AS Name, DATA_TYPE AS DataType
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'academic_plan_adjustments';
            """;

        var columns = (await QueryAsync<ColumnInfo>(columnsSql)).ToDictionary(c => c.Name, c => c.DataType, StringComparer.OrdinalIgnoreCase);

        columns.Should().ContainKey("id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("student_learning_plan_id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("adjustment_type").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("description").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("rationale").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("effective_date").WhoseValue.Should().Be("date");
        columns.Should().ContainKey("end_date").WhoseValue.Should().Be("date");
        columns.Should().ContainKey("approved_by").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("approved_date").WhoseValue.Should().Be("date");
        columns.Should().ContainKey("notes").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("created_at").WhoseValue.Should().Be("datetimeoffset");
        columns.Should().ContainKey("created_by").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("updated_at").WhoseValue.Should().Be("datetimeoffset");
        columns.Should().ContainKey("updated_by").WhoseValue.Should().Be("nvarchar");

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.academic_plan_adjustments') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_academic_plan_adjustments");
        indexes.Should().Contain("IX_academics_adjustments_plan");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('academics.academic_plan_adjustments');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_adjustments_plan");
        fks.Single(fk => fk.Name == "FK_academics_adjustments_plan").DeleteReferentialAction
            .Should().Be(1, "academic_plan_adjustments plan FK must specify ON DELETE CASCADE");
    }

    [SkippableFact]
    public async Task SlpApprovalsTable_Should_ContainAllColumnsAndDataTypesAndIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME AS Name, DATA_TYPE AS DataType
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'slp_approvals';
            """;

        var columns = (await QueryAsync<ColumnInfo>(columnsSql)).ToDictionary(c => c.Name, c => c.DataType, StringComparer.OrdinalIgnoreCase);

        columns.Should().ContainKey("id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("student_learning_plan_id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("plan_version").WhoseValue.Should().Be("int");
        columns.Should().ContainKey("approved_by").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("approved_date").WhoseValue.Should().Be("date");
        columns.Should().ContainKey("approval_type").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("notes").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("created_at").WhoseValue.Should().Be("datetimeoffset");
        columns.Should().ContainKey("created_by").WhoseValue.Should().Be("nvarchar");

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.slp_approvals') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_slp_approvals");
        indexes.Should().Contain("IX_academics_slp_approvals_plan");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('academics.slp_approvals');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_slp_approvals_plan");
        fks.Single(fk => fk.Name == "FK_academics_slp_approvals_plan").DeleteReferentialAction
            .Should().Be(1, "slp_approvals plan FK must specify ON DELETE CASCADE");
    }

    [SkippableFact]
    public async Task AcademicParticipationsTable_Should_ContainSlpForeignKeyAndIndex()
    {
        SkipIfDatabaseUnavailable();

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('academics.academic_participations')
              AND name = 'FK_academics_academic_participations_slp';
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle("FK_academics_academic_participations_slp must exist on academics.academic_participations");
        fks[0].DeleteReferentialAction.Should().Be(2, "academic_participations slp FK must specify ON DELETE SET NULL");

        const string indexSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.academic_participations')
              AND name = 'IX_academics_participations_slp';
            """;

        var indexes = (await QueryAsync<string>(indexSql)).ToList();
        indexes.Should().ContainSingle("IX_academics_participations_slp index must exist on academics.academic_participations");
    }

    [SkippableFact]
    public async Task ForeignKeys_Should_EnforceZeroCrossSchemaReferences()
    {
        SkipIfDatabaseUnavailable();

        const string fkSchemaSql = """
            SELECT
                fk.name AS FkName,
                OBJECT_SCHEMA_NAME(fk.parent_object_id) AS ParentSchema,
                OBJECT_NAME(fk.parent_object_id) AS ParentTable,
                OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS ReferencedSchema,
                OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable
            FROM sys.foreign_keys fk
            WHERE fk.parent_object_id IN (
                OBJECT_ID('academics.student_learning_plans'),
                OBJECT_ID('academics.slp_learning_units'),
                OBJECT_ID('academics.academic_plan_adjustments'),
                OBJECT_ID('academics.slp_approvals')
            )
            OR (fk.parent_object_id = OBJECT_ID('academics.academic_participations') AND fk.name = 'FK_academics_academic_participations_slp');
            """;

        var fks = (await QueryAsync<ForeignKeySchemaInfo>(fkSchemaSql)).ToList();

        fks.Should().NotBeEmpty();
        fks.Should().AllSatisfy(fk =>
        {
            fk.ParentSchema.Should().Be("academics", "parent table must reside in schema academics");
            fk.ReferencedSchema.Should().Be("academics", $"foreign key {fk.FkName} must reference schema academics (zero cross-schema FKs per ADR-001, ADR-002)");
        });
    }

    [SkippableFact]
    public async Task StudentLearningPlans_FilteredUniqueIndex_Should_RejectDuplicateActivePlans_ForSameStudentAndPeriod()
    {
        SkipIfDatabaseUnavailable();

        var student1Id = Guid.NewGuid();
        var student2Id = Guid.NewGuid();
        var period1Id = Guid.NewGuid();
        var period2Id = Guid.NewGuid();

        // 1. Initial insert of active plan for (student1, period1) must succeed
        var plan1Id = await CreateStudentLearningPlanAsync(student1Id, period1Id, status: "active", version: 1);
        plan1Id.Should().NotBeEmpty();

        // 2. Second active plan for same (student1, period1) must violate partial unique index UQ_academics_slp_active (2601 or 2627)
        var actDuplicate = async () =>
        {
            await CreateStudentLearningPlanAsync(student1Id, period1Id, status: "active", version: 2);
        };

        var ex = await actDuplicate.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Match(num => num == 2601 || num == 2627,
            "duplicate active plan for same (student, period) must trigger SQL unique constraint violation (error 2601 or 2627)");

        // 3. Active plan for same student with different period must succeed
        var actDifferentPeriod = async () =>
        {
            await CreateStudentLearningPlanAsync(student1Id, period2Id, status: "active", version: 1);
        };
        await actDifferentPeriod.Should().NotThrowAsync();

        // 4. Active plan for different student with same period must succeed
        var actDifferentStudent = async () =>
        {
            await CreateStudentLearningPlanAsync(student2Id, period1Id, status: "active", version: 1);
        };
        await actDifferentStudent.Should().NotThrowAsync();
    }

    [SkippableFact]
    public async Task StudentLearningPlans_FilteredUniqueIndex_Should_AllowMultipleNonActivePlans_ForSameStudentAndPeriod()
    {
        SkipIfDatabaseUnavailable();

        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        // Multiple non-active statuses for the same student and academic period must all succeed concurrently
        var draft1Id = await CreateStudentLearningPlanAsync(studentId, periodId, status: "draft", version: 1);
        var draft2Id = await CreateStudentLearningPlanAsync(studentId, periodId, status: "draft", version: 2);
        var revised1Id = await CreateStudentLearningPlanAsync(studentId, periodId, status: "revised", version: 1);
        var completedId = await CreateStudentLearningPlanAsync(studentId, periodId, status: "completed", version: 1);
        var archivedId = await CreateStudentLearningPlanAsync(studentId, periodId, status: "archived", version: 1);
        var activeId = await CreateStudentLearningPlanAsync(studentId, periodId, status: "active", version: 2);

        draft1Id.Should().NotBeEmpty();
        draft2Id.Should().NotBeEmpty();
        revised1Id.Should().NotBeEmpty();
        completedId.Should().NotBeEmpty();
        archivedId.Should().NotBeEmpty();
        activeId.Should().NotBeEmpty();

        const string countSql = """
            SELECT COUNT(1)
            FROM academics.student_learning_plans
            WHERE student_id = @StudentId AND academic_period_id = @PeriodId;
            """;

        var count = await ExecuteScalarAsync<int>(countSql, new { StudentId = studentId, PeriodId = periodId });
        count.Should().Be(6, "all 6 distinct plan records for the same student and period should co-exist peacefully");
    }

    [SkippableFact]
    public async Task StudentLearningPlans_Delete_Should_CascadeDeleteChildren()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum Cascade Delete Framework");
        var unitId = await CreateLearningUnitAsync(structureId, "UNIT-CASCADE-01", "Cascade Delete Unit");

        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        // 1. Create a parent student learning plan
        var planId = await CreateStudentLearningPlanAsync(studentId, periodId, status: "draft", version: 1);

        // 2. Insert child records in slp_learning_units, academic_plan_adjustments, and slp_approvals
        var unitLinkId = await CreateSlpLearningUnitAsync(planId, unitId, sequenceOrder: 1, isRequired: true);
        var adjustmentId = await CreateAcademicPlanAdjustmentAsync(planId, "accommodation", "Extended time for tests", "IEP assessment");
        var approvalId = await CreateSlpApprovalAsync(planId, planVersion: 1, approvalType: "initial_approval", approvedBy: "Academic Dean");

        // Verify child records exist
        (await ExecuteScalarAsync<int>("SELECT COUNT(1) FROM academics.slp_learning_units WHERE id = @Id;", new { Id = unitLinkId })).Should().Be(1);
        (await ExecuteScalarAsync<int>("SELECT COUNT(1) FROM academics.academic_plan_adjustments WHERE id = @Id;", new { Id = adjustmentId })).Should().Be(1);
        (await ExecuteScalarAsync<int>("SELECT COUNT(1) FROM academics.slp_approvals WHERE id = @Id;", new { Id = approvalId })).Should().Be(1);

        // 3. Delete the parent plan
        var rowsDeleted = await ExecuteAsync("DELETE FROM academics.student_learning_plans WHERE id = @Id;", new { Id = planId });
        rowsDeleted.Should().Be(1);

        // 4. Verify cascade delete occurred across all 3 child tables
        (await ExecuteScalarAsync<int>("SELECT COUNT(1) FROM academics.slp_learning_units WHERE id = @Id;", new { Id = unitLinkId })).Should().Be(0);
        (await ExecuteScalarAsync<int>("SELECT COUNT(1) FROM academics.academic_plan_adjustments WHERE id = @Id;", new { Id = adjustmentId })).Should().Be(0);
        (await ExecuteScalarAsync<int>("SELECT COUNT(1) FROM academics.slp_approvals WHERE id = @Id;", new { Id = approvalId })).Should().Be(0);
    }

    [SkippableFact]
    public async Task StudentLearningPlans_Delete_Should_SetNullOnAcademicParticipations()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum Set Null Framework");
        var unitId = await CreateLearningUnitAsync(structureId, "UNIT-SETNULL-01", "Set Null Unit");

        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        // 1. Create parent plan
        var planId = await CreateStudentLearningPlanAsync(studentId, periodId, status: "active", version: 1);

        // 2. Create academic participation linking to plan
        var participationId = Guid.NewGuid();
        const string insertPartSql = """
            INSERT INTO academics.academic_participations (
                id, student_id, learning_unit_id, academic_period_id, academic_period_label,
                participation_type, attempt_number, source, status, start_date,
                student_learning_plan_id, created_by
            ) VALUES (
                @Id, @StudentId, @UnitId, @PeriodId, '2026/2027 Ganjil',
                'standard', 1, 'learning_plan', 'in_progress', '2026-08-01',
                @PlanId, 'test-user'
            );
            """;

        await ExecuteAsync(insertPartSql, new
        {
            Id = participationId,
            StudentId = studentId,
            UnitId = unitId,
            PeriodId = periodId,
            PlanId = planId
        });

        // Verify participation has student_learning_plan_id set
        var linkedPlanId = await ExecuteScalarAsync<Guid?>("SELECT student_learning_plan_id FROM academics.academic_participations WHERE id = @Id;", new { Id = participationId });
        linkedPlanId.Should().Be(planId);

        // 3. Delete the parent plan
        var rowsDeleted = await ExecuteAsync("DELETE FROM academics.student_learning_plans WHERE id = @Id;", new { Id = planId });
        rowsDeleted.Should().Be(1);

        // 4. Verify participation still exists and student_learning_plan_id was set to NULL by ON DELETE SET NULL
        const string verifyPartSql = """
            SELECT 1 AS Item1, student_learning_plan_id AS Item2
            FROM academics.academic_participations
            WHERE id = @Id;
            """;

        var (hasRecord, planRef) = await QuerySingleOrDefaultAsync<(bool, Guid?)>(
            verifyPartSql,
            new { Id = participationId });

        hasRecord.Should().BeTrue("academic participation record should not be deleted");
        planRef.Should().BeNull("student_learning_plan_id must be set to NULL on plan deletion (ON DELETE SET NULL)");
    }

    [SkippableFact]
    public async Task StudentLearningPlans_CheckConstraints_Should_EnforceValidStatus()
    {
        SkipIfDatabaseUnavailable();

        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        const string insertSql = """
            INSERT INTO academics.student_learning_plans (
                id, student_id, academic_period_id, academic_period_label, status, version, created_by
            ) VALUES (
                @Id, @StudentId, @PeriodId, '2026/2027 Ganjil', @Status, 1, 'test-user'
            );
            """;

        // Invalid status must trigger check constraint violation (547)
        var actInvalid = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                PeriodId = periodId,
                Status = "invalid_status"
            });
        };

        var ex = await actInvalid.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "invalid SLP status must violate CK_academics_slp_status (547)");

        // Valid statuses ('draft', 'active', 'revised', 'completed', 'archived') must all succeed
        string[] validStatuses = ["draft", "active", "revised", "completed", "archived"];
        foreach (var status in validStatuses)
        {
            var act = async () =>
            {
                await ExecuteAsync(insertSql, new
                {
                    Id = Guid.NewGuid(),
                    StudentId = Guid.NewGuid(), // distinct student per iteration
                    PeriodId = periodId,
                    Status = status
                });
            };
            await act.Should().NotThrowAsync($"status '{status}' should be accepted by CK_academics_slp_status");
        }
    }

    [SkippableFact]
    public async Task StudentLearningPlans_CheckConstraints_Should_EnforceVersionGreaterThanZero()
    {
        SkipIfDatabaseUnavailable();

        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        const string insertSql = """
            INSERT INTO academics.student_learning_plans (
                id, student_id, academic_period_id, academic_period_label, status, version, created_by
            ) VALUES (
                @Id, @StudentId, @PeriodId, '2026/2027 Ganjil', 'draft', @Version, 'test-user'
            );
            """;

        // version = 0 must fail (547)
        var actZero = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                PeriodId = periodId,
                Version = 0
            });
        };
        var exZero = await actZero.Should().ThrowAsync<SqlException>();
        exZero.Which.Number.Should().Be(547, "version = 0 must violate CK_academics_slp_version (547)");

        // version = -1 must fail (547)
        var actNegative = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                PeriodId = periodId,
                Version = -1
            });
        };
        var exNeg = await actNegative.Should().ThrowAsync<SqlException>();
        exNeg.Which.Number.Should().Be(547, "version = -1 must violate CK_academics_slp_version (547)");

        // version = 1 must succeed
        var actValid = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                PeriodId = periodId,
                Version = 1
            });
        };
        await actValid.Should().NotThrowAsync();
    }

    [SkippableFact]
    public async Task SlpLearningUnits_UniqueConstraint_Should_RejectDuplicatePlanAndUnit()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum Unit Unique Test Framework");
        var unit1Id = await CreateLearningUnitAsync(structureId, "UNIT-UQ-01", "Unique Unit 1");
        var unit2Id = await CreateLearningUnitAsync(structureId, "UNIT-UQ-02", "Unique Unit 2");

        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        var plan1Id = await CreateStudentLearningPlanAsync(studentId, periodId, status: "draft", version: 1);
        var plan2Id = await CreateStudentLearningPlanAsync(Guid.NewGuid(), periodId, status: "draft", version: 1);

        // 1. Initial insert of (plan1, unit1) succeeds
        await CreateSlpLearningUnitAsync(plan1Id, unit1Id, sequenceOrder: 1, isRequired: true);

        // 2. Duplicate insert of (plan1, unit1) must violate unique constraint UQ_academics_slp_units_plan_unit
        var actDuplicate = async () =>
        {
            await CreateSlpLearningUnitAsync(plan1Id, unit1Id, sequenceOrder: 2, isRequired: false);
        };
        var ex = await actDuplicate.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Match(num => num == 2601 || num == 2627,
            "duplicate (student_learning_plan_id, learning_unit_id) must trigger SQL unique constraint violation (error 2601 or 2627)");

        // 3. Different unit for same plan succeeds
        var actDifferentUnit = async () =>
        {
            await CreateSlpLearningUnitAsync(plan1Id, unit2Id, sequenceOrder: 2, isRequired: false);
        };
        await actDifferentUnit.Should().NotThrowAsync();

        // 4. Same unit for different plan succeeds
        var actDifferentPlan = async () =>
        {
            await CreateSlpLearningUnitAsync(plan2Id, unit1Id, sequenceOrder: 1, isRequired: true);
        };
        await actDifferentPlan.Should().NotThrowAsync();
    }

    [SkippableFact]
    public async Task SlpLearningUnits_ForeignKey_Should_RestrictDeleteOfReferencedLearningUnit()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum Unit Delete Restrict Framework");
        var unitId = await CreateLearningUnitAsync(structureId, "UNIT-RESTRICT-01", "Delete Restrict Unit");

        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        var planId = await CreateStudentLearningPlanAsync(studentId, periodId, status: "draft", version: 1);
        await CreateSlpLearningUnitAsync(planId, unitId, sequenceOrder: 1, isRequired: true);

        // Deleting referenced learning_units row must fail with 547 (ON DELETE NO ACTION)
        var actDelete = async () =>
        {
            await ExecuteAsync("DELETE FROM academics.learning_units WHERE id = @Id;", new { Id = unitId });
        };

        var ex = await actDelete.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "deleting learning unit referenced by slp_learning_units must violate FK_academics_slp_units_unit (ON DELETE NO ACTION)");
    }

    [SkippableFact]
    public async Task AcademicPlanAdjustments_CheckConstraints_Should_EnforceValidAdjustmentType()
    {
        SkipIfDatabaseUnavailable();

        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var planId = await CreateStudentLearningPlanAsync(studentId, periodId, status: "draft", version: 1);

        const string insertSql = """
            INSERT INTO academics.academic_plan_adjustments (
                id, student_learning_plan_id, adjustment_type, description, rationale,
                effective_date, approved_by, approved_date, created_by
            ) VALUES (
                @Id, @PlanId, @AdjustmentType, 'Description', 'Rationale',
                '2026-08-01', 'Academic Dean', '2026-08-01', 'test-user'
            );
            """;

        // Invalid adjustment_type must fail (547)
        var actInvalid = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                PlanId = planId,
                AdjustmentType = "invalid_type"
            });
        };
        var ex = await actInvalid.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "invalid adjustment type must violate CK_academics_adjustments_type (547)");

        // Valid adjustment types ('accommodation', 'substitution', 'exemption') must all succeed
        string[] validTypes = ["accommodation", "substitution", "exemption"];
        foreach (var type in validTypes)
        {
            var act = async () =>
            {
                await ExecuteAsync(insertSql, new
                {
                    Id = Guid.NewGuid(),
                    PlanId = planId,
                    AdjustmentType = type
                });
            };
            await act.Should().NotThrowAsync($"adjustment type '{type}' should be accepted by CK_academics_adjustments_type");
        }
    }

    [SkippableFact]
    public async Task SlpApprovals_CheckConstraints_Should_EnforceValidApprovalTypeAndVersion()
    {
        SkipIfDatabaseUnavailable();

        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var planId = await CreateStudentLearningPlanAsync(studentId, periodId, status: "draft", version: 1);

        const string insertSql = """
            INSERT INTO academics.slp_approvals (
                id, student_learning_plan_id, plan_version, approved_by, approved_date, approval_type, created_by
            ) VALUES (
                @Id, @PlanId, @PlanVersion, 'Academic Dean', '2026-08-01', @ApprovalType, 'test-user'
            );
            """;

        // 1. Invalid approval_type must fail (547)
        var actInvalidType = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                PlanId = planId,
                PlanVersion = 1,
                ApprovalType = "invalid_approval_type"
            });
        };
        var exType = await actInvalidType.Should().ThrowAsync<SqlException>();
        exType.Which.Number.Should().Be(547, "invalid approval type must violate CK_academics_slp_approvals_type (547)");

        // 2. Valid approval types ('initial_approval', 'revision_approval') must succeed
        string[] validTypes = ["initial_approval", "revision_approval"];
        foreach (var type in validTypes)
        {
            var act = async () =>
            {
                await ExecuteAsync(insertSql, new
                {
                    Id = Guid.NewGuid(),
                    PlanId = planId,
                    PlanVersion = 1,
                    ApprovalType = type
                });
            };
            await act.Should().NotThrowAsync($"approval type '{type}' should be accepted by CK_academics_slp_approvals_type");
        }

        // 3. plan_version = 0 must fail (547)
        var actZeroVersion = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                PlanId = planId,
                PlanVersion = 0,
                ApprovalType = "initial_approval"
            });
        };
        var exVersion = await actZeroVersion.Should().ThrowAsync<SqlException>();
        exVersion.Which.Number.Should().Be(547, "plan_version = 0 must violate CK_academics_slp_approvals_version (547)");

        // 4. plan_version >= 1 must succeed
        var actValidVersion = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                PlanId = planId,
                PlanVersion = 2,
                ApprovalType = "revision_approval"
            });
        };
        await actValidVersion.Should().NotThrowAsync();
    }

    private async Task<Guid> CreateStudentLearningPlanAsync(
        Guid studentId,
        Guid periodId,
        string status = "draft",
        int version = 1,
        string? notes = "SLP integration test plan")
    {
        var id = Guid.NewGuid();
        const string sql = """
            INSERT INTO academics.student_learning_plans (
                id, student_id, academic_period_id, academic_period_label, status, version, notes, created_by
            ) VALUES (
                @Id, @StudentId, @PeriodId, '2026/2027 Ganjil', @Status, @Version, @Notes, 'test-user'
            );
            """;

        await ExecuteAsync(sql, new
        {
            Id = id,
            StudentId = studentId,
            PeriodId = periodId,
            Status = status,
            Version = version,
            Notes = notes
        });

        return id;
    }

    private async Task<Guid> CreateSlpLearningUnitAsync(
        Guid planId,
        Guid unitId,
        int? sequenceOrder = 1,
        bool isRequired = true,
        string? notes = null)
    {
        var id = Guid.NewGuid();
        const string sql = """
            INSERT INTO academics.slp_learning_units (
                id, student_learning_plan_id, learning_unit_id, sequence_order, is_required, notes, created_by
            ) VALUES (
                @Id, @PlanId, @UnitId, @SequenceOrder, @IsRequired, @Notes, 'test-user'
            );
            """;

        await ExecuteAsync(sql, new
        {
            Id = id,
            PlanId = planId,
            UnitId = unitId,
            SequenceOrder = sequenceOrder,
            IsRequired = isRequired,
            Notes = notes
        });

        return id;
    }

    private async Task<Guid> CreateAcademicPlanAdjustmentAsync(
        Guid planId,
        string adjustmentType,
        string description,
        string rationale,
        string approvedBy = "Academic Authority")
    {
        var id = Guid.NewGuid();
        const string sql = """
            INSERT INTO academics.academic_plan_adjustments (
                id, student_learning_plan_id, adjustment_type, description, rationale,
                effective_date, approved_by, approved_date, notes, created_by
            ) VALUES (
                @Id, @PlanId, @AdjustmentType, @Description, @Rationale,
                '2026-08-01', @ApprovedBy, '2026-08-01', 'Adjustment notes', 'test-user'
            );
            """;

        await ExecuteAsync(sql, new
        {
            Id = id,
            PlanId = planId,
            AdjustmentType = adjustmentType,
            Description = description,
            Rationale = rationale,
            ApprovedBy = approvedBy
        });

        return id;
    }

    private async Task<Guid> CreateSlpApprovalAsync(
        Guid planId,
        int planVersion,
        string approvalType,
        string approvedBy = "Academic Authority")
    {
        var id = Guid.NewGuid();
        const string sql = """
            INSERT INTO academics.slp_approvals (
                id, student_learning_plan_id, plan_version, approved_by, approved_date, approval_type, notes, created_by
            ) VALUES (
                @Id, @PlanId, @PlanVersion, @ApprovedBy, '2026-08-01', @ApprovalType, 'Approval notes', 'test-user'
            );
            """;

        await ExecuteAsync(sql, new
        {
            Id = id,
            PlanId = planId,
            PlanVersion = planVersion,
            ApprovedBy = approvedBy,
            ApprovalType = approvalType
        });

        return id;
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

    private sealed record ColumnInfo(string Name, string DataType);
    private sealed record ForeignKeyInfo(string Name, byte DeleteReferentialAction);
    private sealed record ForeignKeySchemaInfo(string FkName, string ParentSchema, string ParentTable, string ReferencedSchema, string ReferencedTable);
}
